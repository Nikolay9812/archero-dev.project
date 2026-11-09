using System;
using UnityEngine;
using UnityEngine.UI;

// The player's health. Enemies call TakeDamage; at 0 HP she dies and the Game Over screen appears.
// It builds its own HP bar above her head when the game starts, so no UI setup is needed.
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 300;
    public float invincibleTime = 0.6f; // after a hit, ignore damage for this long (and blink)
    public float reviveInvincibleTime = 2.5f; // the same after coming back with the Revive item

    // HP bar look (sizes in meters, the bar always faces the camera)
    public Vector3 barOffset = new Vector3(0f, 2.1f, 0f);
    public Vector2 barSize = new Vector2(1.2f, 0.16f);
    public Color barColor = new Color(0.3f, 0.85f, 0.3f);
    public Color barLowColor = new Color(0.9f, 0.25f, 0.2f);

    [Header("Game feel")]
    public Color hurtNumberColor = new Color(1f, 0.25f, 0.2f);
    public float hitShake = 0.3f; // camera shake in meters when she gets hit

    // The one player in the scene, so enemies can find her without tags
    public static PlayerHealth Instance { get; private set; }

    // GameOverScreen listens to this
    public static event Action Died;

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private Renderer[] renderers;
    private float invincibleTimer;
    private bool reviveUsed;
    private float dodgeChance; // 0.04 = 4% of all hits miss her
    private Transform barRoot;
    private Image barFill;
    private Text barText;

    void Awake()
    {
        Instance = this;
        maxHealth += Upgrades.BonusHealth; // permanent talent from the Collectible page
        maxHealth += Inventory.BonusInt(GearStat.MaxHealth); // worn gear

        // Found artifacts: flat HP first, then the percent on top of everything
        maxHealth += Artifacts.BonusInt(ArtifactStat.MaxHealth);
        maxHealth = Mathf.RoundToInt(maxHealth * Artifacts.Multiplier(ArtifactStat.HealthPercent));
        dodgeChance = Artifacts.Bonus(ArtifactStat.Dodge) / 100f;
        CurrentHealth = maxHealth;
        BuildBar();
        UpdateBar();
    }

    // In Start, not Awake: BowVisual builds the bow in its Awake, and every Awake runs before any Start.
    // So the bow is in this list too, and blinks with her.
    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (invincibleTimer <= 0f) return;

        invincibleTimer -= Time.deltaTime;

        // Blink by switching the model on and off ~15 times a second
        bool visible = invincibleTimer <= 0f || Mathf.FloorToInt(invincibleTimer * 15f) % 2 == 0;
        SetVisible(visible);
    }

    void LateUpdate()
    {
        // Keep the bar above her head and turned to the camera, whatever way she faces
        if (barRoot == null || Camera.main == null) return;
        barRoot.position = transform.position + barOffset;
        barRoot.rotation = Camera.main.transform.rotation;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || invincibleTimer > 0f) return;

        // Dodge (from artifacts): the hit misses. The short invincibility stops an enemy that keeps
        // touching her from rolling again every single frame.
        if (UnityEngine.Random.value < dodgeChance)
        {
            DamageNumber.Spawn(transform.position + barOffset + Vector3.up * 0.4f, "DODGE", Color.white);
            invincibleTimer = invincibleTime;
            return;
        }

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        UpdateBar();

        // Game feel: a red number above her head and a short camera shake, so every hit is noticed
        DamageNumber.Spawn(transform.position + barOffset + Vector3.up * 0.4f, amount, hurtNumberColor);
        CameraFollow.Shake(hitShake, 0.2f);
        SoundManager.Play(Sound.PlayerHurt, true);

        if (IsDead && GemShop.Owns(GemItem.Revive) && !reviveUsed) Revive();
        else if (IsDead) Die();
        else invincibleTimer = invincibleTime;
    }

    // The Revive item from the shop: back on her feet once per run. She blinks longer, to get away safely.
    void Revive()
    {
        reviveUsed = true;
        SoundManager.Play(Sound.Heal);
        CurrentHealth = maxHealth / 2;
        invincibleTimer = reviveInvincibleTime;
        UpdateBar();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        // Only make a sound when she really gets HP back (not when she is already full)
        if (amount > 0 && CurrentHealth < maxHealth) SoundManager.Play(Sound.Heal);

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth); // never above max
        UpdateBar();
    }

    // More max HP, and the new HP is filled straight away (300/300 -> 360/360, 100/300 -> 160/360)
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        Heal(amount);
    }

    void Die()
    {
        SetVisible(true);

        // Stop all control: no walking, no shooting
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;
        PlayerShooter shooter = GetComponent<PlayerShooter>();
        if (shooter != null) shooter.enabled = false;

        // Play the death animation. Without one (Animator not set up), she just falls over by code.
        PlayerAnimation playerAnimation = GetComponent<PlayerAnimation>();
        if (playerAnimation == null || !playerAnimation.PlayDeath())
        {
            transform.rotation = Quaternion.LookRotation(Vector3.up, transform.forward);
            transform.position += Vector3.up * 0.15f;
        }
        barRoot.gameObject.SetActive(false);

        Died?.Invoke();
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    void UpdateBar()
    {
        float percent = (float)CurrentHealth / maxHealth;
        barFill.fillAmount = percent;
        barFill.color = percent > 0.3f ? barColor : barLowColor;
        barText.text = CurrentHealth.ToString();
    }

    void BuildBar()
    {
        // World-space canvas: 100 UI pixels = 1 meter
        const float pixelsPerMeter = 100f;
        Vector2 size = barSize * pixelsPerMeter;

        GameObject canvasObject = new GameObject("HealthBar", typeof(Canvas));
        barRoot = canvasObject.transform;
        barRoot.localScale = Vector3.one / pixelsPerMeter;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObject.GetComponent<RectTransform>().sizeDelta = size;

        Sprite white = CreateWhiteSprite();

        Image background = CreateImage("Background", barRoot, white, new Color(0f, 0f, 0f, 0.6f), size + new Vector2(6f, 6f));

        barFill = CreateImage("Fill", barRoot, white, barColor, size);
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;

        // HP number above the bar
        GameObject textObject = new GameObject("Text", typeof(Text));
        textObject.transform.SetParent(barRoot, false);
        barText = textObject.GetComponent<Text>();
        barText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        barText.fontSize = 28;
        barText.fontStyle = FontStyle.Bold;
        barText.alignment = TextAnchor.LowerCenter;
        barText.color = Color.white;
        barText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform textRect = barText.rectTransform;
        textRect.sizeDelta = new Vector2(size.x, 40f);
        textRect.anchoredPosition = new Vector2(0f, size.y / 2f + 22f);
        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;

        background.transform.SetAsFirstSibling();
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.rectTransform.sizeDelta = size;
        return image;
    }

    // A filled Image needs a sprite, so make a tiny white one
    static Sprite CreateWhiteSprite()
    {
        Texture2D texture = Texture2D.whiteTexture;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}

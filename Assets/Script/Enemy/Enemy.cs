using System.Collections.Generic;
using UnityEngine;

// A simple enemy: it has health, flashes when hit and disappears when health reaches 0.
// Put it on any object that has a Collider (e.g. a Capsule), so arrows can hit it.
public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    public Color hitFlashColor = Color.white;
    public float hitFlashTime = 0.1f;

    // Every living enemy in the scene. The player's auto-shoot looks here for the nearest target,
    // so we don't need tags or FindObjectsByType every frame.
    public static readonly List<Enemy> All = new List<Enemy>();

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private Renderer[] renderers;
    private Color[] originalColors;
    private float flashTimer;

    void Awake()
    {
        CurrentHealth = maxHealth;

        // Remember the normal colors so we can flash and then go back
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].material.color;
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Update()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f) SetColor(null);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        flashTimer = hitFlashTime;
        SetColor(hitFlashColor);

        if (IsDead) Die();
    }

    void Die()
    {
        // Later: death animation, coins, XP...
        Destroy(gameObject);
    }

    // null = back to the original colors
    void SetColor(Color? color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].material.color = color ?? originalColors[i];
        }
    }
}

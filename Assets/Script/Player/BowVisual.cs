using UnityEngine;

// A simple bow made of shapes, held in the character's left hand.
// Its limbs have the color of the worn weapon's rarity (plain wood when no weapon is worn).
// Put it on the Player. The menu avatar adds one by code.
//
// Tuning: press Play, select the Player and change the numbers below until the bow sits right in her hand
// (they work live). Then right-click the component > Copy Component, stop Play mode,
// and right-click > Paste Component Values, because changes made in Play mode are lost.
// Also type the good numbers in here as the defaults, so the menu avatar uses them too.
public class BowVisual : MonoBehaviour
{
    public Vector3 handPosition = new Vector3(0f, 0.08f, 0.03f); // meters, from the hand bone
    public Vector3 handRotation = new Vector3(0f, 0f, 90f);      // degrees
    public float bowHeight = 0.8f;                               // meters from tip to tip
    public Color woodColor = new Color(0.55f, 0.35f, 0.2f);
    public Color gripColor = new Color(0.25f, 0.15f, 0.1f);
    public Color stringColor = new Color(0.9f, 0.9f, 0.85f);

    private Transform hand;
    private Transform bow;
    private Renderer[] limbs = new Renderer[2];

    void Awake()
    {
        // Humanoid models (like ours from Mixamo) can tell us where each bone is
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman) hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

        if (hand == null)
        {
            Debug.LogWarning("BowVisual: no humanoid left hand found, the bow floats next to " + name + ".");
            hand = transform;
        }

        BuildBow();
        RefreshColor();
    }

    void OnEnable()
    {
        Inventory.Changed += RefreshColor; // a new weapon worn in the menu changes the color right away
    }

    void OnDisable()
    {
        Inventory.Changed -= RefreshColor;
    }

    // LateUpdate runs after the animation has moved the hand, so changes in the Inspector show at once
    void LateUpdate()
    {
        PlaceInHand();
    }

    void RefreshColor()
    {
        OwnedGear weapon = Inventory.GetEquipped(GearSlot.Weapon);
        Color color = weapon != null ? GearCatalog.RarityColor(weapon.rarity) : woodColor;

        foreach (Renderer limb in limbs) limb.material.color = color;
    }

    void PlaceInHand()
    {
        bow.localPosition = handPosition;
        bow.localRotation = Quaternion.Euler(handRotation);

        // The bone may be scaled (models are often made in centimeters), so undo that scale:
        // this way bowHeight is always in real meters
        bow.localScale = Vector3.one * (bowHeight / hand.lossyScale.x);
    }

    // The bow is built 1 meter tall, standing up (Y), with the string behind it (-Z).
    // PlaceInHand then scales it to bowHeight.
    void BuildBow()
    {
        bow = new GameObject("Bow").transform;
        bow.SetParent(hand, false);

        AddPiece("Grip", new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(0.07f, 0.18f, 0.07f), gripColor);

        // The two limbs lean back towards the string. Rotating around X tilts them forwards or backwards.
        limbs[0] = AddPiece("UpperLimb", new Vector3(0f, 0.27f, -0.05f), new Vector3(-14f, 0f, 0f), new Vector3(0.045f, 0.42f, 0.05f), woodColor);
        limbs[1] = AddPiece("LowerLimb", new Vector3(0f, -0.27f, -0.05f), new Vector3(14f, 0f, 0f), new Vector3(0.045f, 0.42f, 0.05f), woodColor);

        AddPiece("String", new Vector3(0f, 0f, -0.1f), Vector3.zero, new Vector3(0.012f, 0.95f, 0.012f), stringColor);

        PlaceInHand();
    }

    Renderer AddPiece(string pieceName, Vector3 position, Vector3 rotation, Vector3 size, Color color)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = pieceName;
        piece.transform.SetParent(bow, false);
        piece.transform.localPosition = position;
        piece.transform.localRotation = Quaternion.Euler(rotation);
        piece.transform.localScale = size;

        // Only for looks: a collider in her hand would bump into her own CharacterController
        Destroy(piece.GetComponent<Collider>());

        Renderer pieceRenderer = piece.GetComponent<Renderer>();
        pieceRenderer.material.color = color;
        return pieceRenderer;
    }
}

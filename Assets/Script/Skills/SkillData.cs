using UnityEngine;

// The kinds of skill we have. An enum is a fixed list of named choices:
// the Inspector shows it as a dropdown, and PlayerSkills uses a switch to decide what each one does.
public enum SkillType
{
    AttackUp,
    AttackSpeedUp,
    MaxHealthUp,
    Heal,
    MoveSpeedUp,
    // Arrow skills (always add new types at the END: Unity saves enums as numbers, 0, 1, 2...,
    // so inserting one in the middle would change what the existing skill assets are)
    FrontArrow,
    DiagonalArrows,
    Ricochet
}

// One skill card, saved as an asset file in the Project window (like a material).
// A ScriptableObject is a script that holds data but doesn't live on a GameObject.
// Make a new one: Project window > right-click > Create > Archero > Skill.
[CreateAssetMenu(fileName = "NewSkill", menuName = "Archero/Skill")]
public class SkillData : ScriptableObject
{
    public Sprite icon; // the picture on the card (a PNG with Texture Type = Sprite (2D and UI))
    public string title = "New Skill";
    public string description = "What it does";
    public SkillType type;
    public float amount = 0.2f; // 0.2 = 20%. For arrow skills: how many (extra arrows / bounces)
    public Color cardColor = new Color(0.2f, 0.35f, 0.6f);
}

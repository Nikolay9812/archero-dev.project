using UnityEngine;

// The kinds of skill we have. An enum is a fixed list of named choices:
// the Inspector shows it as a dropdown, and PlayerSkills uses a switch to decide what each one does.
public enum SkillType
{
    AttackUp,
    AttackSpeedUp,
    MaxHealthUp,
    Heal,
    MoveSpeedUp
}

// One skill card, saved as an asset file in the Project window (like a material).
// A ScriptableObject is a script that holds data but doesn't live on a GameObject.
// Make a new one: Project window > right-click > Create > Archero > Skill.
[CreateAssetMenu(fileName = "NewSkill", menuName = "Archero/Skill")]
public class SkillData : ScriptableObject
{
    public string title = "New Skill";
    public string description = "What it does";
    public SkillType type;
    public float amount = 0.2f; // 0.2 = 20%
    public Color cardColor = new Color(0.2f, 0.35f, 0.6f);
}

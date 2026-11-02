using System;

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

// One skill card: what it's called, what the card says, what it does and how strong it is.
// [Serializable] lets Unity show a list of these in the Inspector (see SkillChoiceScreen).
// Not a MonoBehaviour: it's just data, it doesn't live on a GameObject.
[Serializable]
public class Skill
{
    public string title;
    public string description;
    public SkillType type;
    public float amount; // 0.2 = 20%

    // Unity needs an empty constructor to create skills itself (e.g. when you press + on the list in the Inspector)
    public Skill() { }

    public Skill(string title, string description, SkillType type, float amount)
    {
        this.title = title;
        this.description = description;
        this.type = type;
        this.amount = amount;
    }
}

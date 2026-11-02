using System.Collections.Generic;
using UnityEngine;

// Applies chosen skills to the player by changing her stats on the other player scripts.
// Put it on the Player, next to PlayerHealth, PlayerShooter and PlayerMovement.
public class PlayerSkills : MonoBehaviour
{
    // Every skill picked this run, in order (handy for a "your skills" list later)
    public List<Skill> Taken { get; } = new List<Skill>();

    private PlayerHealth health;
    private PlayerShooter shooter;
    private PlayerMovement movement;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        shooter = GetComponent<PlayerShooter>();
        movement = GetComponent<PlayerMovement>();
    }

    public void Apply(Skill skill)
    {
        Taken.Add(skill);

        // switch = "which one of the enum values is it?" Each case ends with break.
        switch (skill.type)
        {
            case SkillType.AttackUp:
                // RoundToInt because damage is a whole number (25 * 1.2 = 30)
                shooter.damage = Mathf.RoundToInt(shooter.damage * (1f + skill.amount));
                break;

            case SkillType.AttackSpeedUp:
                shooter.fireRate *= 1f + skill.amount;
                break;

            case SkillType.MaxHealthUp:
                health.IncreaseMaxHealth(Mathf.RoundToInt(health.maxHealth * skill.amount));
                break;

            case SkillType.Heal:
                health.Heal(Mathf.RoundToInt(health.maxHealth * skill.amount));
                break;

            case SkillType.MoveSpeedUp:
                movement.speed *= 1f + skill.amount;
                break;
        }

        Debug.Log("Skill taken: " + skill.title);
    }
}

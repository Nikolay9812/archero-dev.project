using UnityEngine;

// A reward the player collects by walking onto it, used in rest rooms.
// A rest room has two of these and the player may only take ONE:
// taking a reward removes every other reward in the same room.
// Needs a Box Collider (it is turned into a trigger automatically).
public enum RewardType
{
    Heal,
    Skill
}

[RequireComponent(typeof(BoxCollider))]
public class RoomReward : MonoBehaviour
{
    public RewardType type = RewardType.Heal;

    [Range(0f, 1f)]
    public float healPercent = 0.4f; // Heal only: part of max HP restored (0.4 = 40%)

    private bool taken;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (taken) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        taken = true;
        GiveReward(player);
        RemoveAllRewardsInRoom();
    }

    void GiveReward(PlayerHealth player)
    {
        switch (type)
        {
            case RewardType.Heal:
                player.Heal(Mathf.RoundToInt(player.maxHealth * healPercent));
                break;

            case RewardType.Skill:
                // The same 3 cards as a level-up
                SkillChoiceScreen skillScreen = FindAnyObjectByType<SkillChoiceScreen>();
                if (skillScreen != null) skillScreen.OfferSkill();
                else Debug.LogWarning("There is no SkillChoiceScreen in the scene, so this reward does nothing.");
                break;
        }
    }

    // This is what makes it a choice: once one reward is taken, the others disappear too
    void RemoveAllRewardsInRoom()
    {
        Room room = GetComponentInParent<Room>();
        if (room == null)
        {
            Destroy(gameObject);
            return;
        }

        foreach (RoomReward reward in room.GetComponentsInChildren<RoomReward>())
        {
            Destroy(reward.gameObject);
        }
    }
}

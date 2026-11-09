using System.Collections.Generic;

// A set is a group of 3 artifacts. Collecting them unlocks extra bonuses, in 3 steps ("tiers"):
//   tier 1 - find 2 of the 3
//   tier 2 - find all 3
//   tier 3 - all 3 have at least 3 stars
// Nothing about sets is saved: whether a tier is active is worked out from the artifacts every time.

// One bonus, like "ATK +3%"
public class SetBonus
{
    public readonly ArtifactStat stat;
    public readonly float value;

    public SetBonus(ArtifactStat stat, float value)
    {
        this.stat = stat;
        this.value = value;
    }
}

public class ArtifactSet
{
    public readonly string title;
    public readonly string[] artifactIds; // ids from ArtifactCatalog
    public readonly SetBonus[] tiers;     // one bonus per tier, in order

    // "params" lets the caller list the tiers one after another instead of building an array
    public ArtifactSet(string title, string[] artifactIds, params SetBonus[] tiers)
    {
        this.title = title;
        this.artifactIds = artifactIds;
        this.tiers = tiers;
    }
}

public static class ArtifactSets
{
    public static readonly ArtifactSet[] All =
    {
        new ArtifactSet("Hunter's Kit", new[] { "sun_medal", "owl_feather", "river_pebble" },
            new SetBonus(ArtifactStat.AttackPercent, 3f),
            new SetBonus(ArtifactStat.AttackSpeedPercent, 3f),
            new SetBonus(ArtifactStat.AttackPercent, 6f)),

        new ArtifactSet("Thunder Path", new[] { "storm_jar", "old_compass", "ember_stone" },
            new SetBonus(ArtifactStat.CritChance, 2f),
            new SetBonus(ArtifactStat.CritDamage, 15f),
            new SetBonus(ArtifactStat.CritChance, 4f)),

        new ArtifactSet("Giant Slayer", new[] { "giants_tooth", "iron_acorn", "frost_bell" },
            new SetBonus(ArtifactStat.HealthPercent, 4f),
            new SetBonus(ArtifactStat.BossDamage, 8f),
            new SetBonus(ArtifactStat.HealthPercent, 8f)),

        new ArtifactSet("Trickster", new[] { "moon_mirror", "lucky_clover", "fox_tail" },
            new SetBonus(ArtifactStat.Dodge, 2f),
            new SetBonus(ArtifactStat.CoinBonus, 15f),
            new SetBonus(ArtifactStat.MoveSpeedPercent, 5f))
    };

    // What each tier needs. Index = tier number.
    private static readonly int[] FoundNeeded = { 2, 3, 3 };
    private static readonly int[] StarsNeeded = { 0, 0, 3 }; // stars EVERY artifact of the set needs

    // The artifacts of a set (an id that isn't in the catalog any more is left out)
    public static ArtifactType[] Members(ArtifactSet set)
    {
        List<ArtifactType> members = new List<ArtifactType>();
        foreach (string id in set.artifactIds)
        {
            ArtifactType type = ArtifactCatalog.Find(id);
            if (type != null) members.Add(type);
        }
        return members.ToArray();
    }

    // The set an artifact belongs to, or null if it is in none
    public static ArtifactSet SetOf(ArtifactType type)
    {
        foreach (ArtifactSet set in All)
        {
            if (System.Array.IndexOf(set.artifactIds, type.id) >= 0) return set; // IndexOf = -1 if not in the array
        }
        return null;
    }

    public static int FoundCount(ArtifactSet set)
    {
        int count = 0;
        foreach (ArtifactType type in Members(set))
        {
            if (Artifacts.IsFound(type)) count++;
        }
        return count;
    }

    public static bool IsTierActive(ArtifactSet set, int tier)
    {
        if (FoundCount(set) < FoundNeeded[tier]) return false;

        foreach (ArtifactType type in Members(set))
        {
            if (Artifacts.Stars(type) < StarsNeeded[tier]) return false;
        }
        return true;
    }

    // What the player must do for a tier, e.g. "Find 2"
    public static string Requirement(int tier)
    {
        if (StarsNeeded[tier] > 0) return "All at " + StarsNeeded[tier] + " stars";
        if (FoundNeeded[tier] >= 3) return "Find all 3";
        return "Find " + FoundNeeded[tier];
    }

    // All active set bonuses for this stat, added up (Artifacts.Bonus adds this to the artifacts' own stats)
    public static float Bonus(ArtifactStat stat)
    {
        float total = 0f;
        foreach (ArtifactSet set in All)
        {
            for (int tier = 0; tier < set.tiers.Length; tier++)
            {
                if (set.tiers[tier].stat == stat && IsTierActive(set, tier)) total += set.tiers[tier].value;
            }
        }
        return total;
    }
}

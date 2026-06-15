using System;
using System.Collections.Generic;

namespace PapiroFeister.Characters.Players;

public enum SkillType
{
    Woodworking,
    Smithing,
    Cooking,
    Tailoring
}

public sealed class PlayerSkills
{
    private readonly Dictionary<SkillType, int> _xp = new();
    private readonly Dictionary<SkillType, int> _levels = new();

    public event Action<SkillType, int> OnLevelUp;

    public PlayerSkills()
    {
        // Initialize all skills to Level 1, 0 XP
        foreach (SkillType skill in Enum.GetValues<SkillType>())
        {
            _xp[skill] = 0;
            _levels[skill] = 1;
        }
    }

    public int GetLevel(SkillType skill)
    {
        return _levels.TryGetValue(skill, out int level) ? level : 1;
    }

    public int GetXP(SkillType skill)
    {
        return _xp.TryGetValue(skill, out int xp) ? xp : 0;
    }

    public int GetXPForNextLevel(int currentLevel)
    {
        // Simple scaling: Level 1 needs 100 XP, Level 2 needs 200 XP, Level 3 needs 300 XP, etc.
        return currentLevel * 100;
    }

    public void AddXP(SkillType skill, int amount, out bool leveledUp)
    {
        leveledUp = false;
        if (amount <= 0) return;

        int currentXP = GetXP(skill);
        int currentLevel = GetLevel(skill);

        currentXP += amount;
        _xp[skill] = currentXP;

        int xpNeeded = GetXPForNextLevel(currentLevel);
        while (currentXP >= xpNeeded)
        {
            currentXP -= xpNeeded;
            currentLevel++;
            _xp[skill] = currentXP;
            leveledUp = true;
            xpNeeded = GetXPForNextLevel(currentLevel);
        }

        if (leveledUp)
        {
            _levels[skill] = currentLevel;
            OnLevelUp?.Invoke(skill, currentLevel);
        }
    }
}

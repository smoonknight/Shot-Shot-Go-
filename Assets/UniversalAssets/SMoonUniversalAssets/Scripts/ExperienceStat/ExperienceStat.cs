using System;
using UnityEngine;

[System.Serializable]
public class ExperienceStat
{
    /// <summary>
    /// Level with startLevel
    /// </summary>
    public int Level => GetLevel(exp);
    /// <summary>
    /// Level without startLevel
    /// </summary>
    public int NormalizedLevel => GetLevel(exp, 0);
    public int exp;
    [NonSerialized]
    public int startLevel = 1;
    [NonSerialized]
    public int maximumLevel = 4;
    [NonSerialized]
    public float factor = 1000;
    [NonSerialized]
    public bool useExperienceTierAsReference;

    public int GetLevel(int exp) => GetLevel(exp, startLevel);
    public int GetLevel(int exp, int startLevel) => Math.Min(maximumLevel, (int)Math.Floor(startLevel + (Math.Sqrt(1 + 8 * exp / factor) / 2)));

    public bool IsNextExpRequire(out int expRequire)
    {
        int level = Level;
        int netralLevel = level - startLevel;
        expRequire = Mathf.FloorToInt((4 * (netralLevel + 1) * (netralLevel + 1) - 1) * factor / 8);
        return level < maximumLevel;
    }

    public bool IsNextExpRequireAndRemainingExp(out int expRequire, out int remainingExp)
    {
        bool isNextExpRequire = IsNextExpRequire(out expRequire);
        remainingExp = expRequire - exp;
        return isNextExpRequire;
    }

    public void AddExperience(int amount)
    {
        if (!IsNextExpRequire(out int expRequire))
        {
            exp = expRequire;
        }
        else
        {
            exp += amount;
        }
    }

    public void AddExperience(int amount, out bool isLevelUp, out (int currentLevel, int levelUpCount) result)
    {
        int previousLevel = Level;
        AddExperience(amount);
        int currentLevel = Level;

        isLevelUp = currentLevel > previousLevel;
        int levelUpCount = currentLevel - previousLevel;

        result = (currentLevel, levelUpCount);
    }
}
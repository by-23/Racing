using UnityEngine;

public static class LevelLockManager
{
    public static void UnlockLevel(int levelNumber)
    {
        PlayerPrefs.SetInt($"UnlockedLevels/Level{levelNumber}", 1);
        PlayerPrefs.Save();
    }

    public static bool IsLocked(int levelNumber)
    {
        if(levelNumber == 1)
        {
            return false;
        }

        return !PlayerPrefs.HasKey($"UnlockedLevels/Level{levelNumber}");
        
    }
}

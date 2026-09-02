
using System.Collections.Generic;
using UnityEngine;

// Menüden oyunlara aktarilacak moduler level verisi
[System.Serializable]
public class LevelConfig
{
    public string sceneName;
    public string jsonFileName;
}

public static class GameSessionData
{
    // Level modu mu-(false), Sonsuz mod mu (true)
    public static bool IsInfinityMode = true;

    // A1, A2, B1, B2, C1
    public static string SelectedLevel = "B1";

    public static Color SelectedLevelColor = Color.white;
    public static Color SelectedLevelTextColor = Color.black;

    // Baglanmasi gereken JSON dosyasinin adi (uzantisi olmadan)
    public static string TargetJsonFile = "global_typing_B1";

    // Level sistemi degiskenleri
    public static int CurrentUnitIndex = 0;
    public static int CurrentLevelIndex = 0;

    // O an oynanan unitenin tum levellerini hafizada tutar (Next Level butonu icin)
    public static List<LevelConfig> CurrentUnitLevels = new List<LevelConfig>();
}
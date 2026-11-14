using UnityEngine;

public static class HighScore
{
    private const string Key = "KesslerHighScore";

    public static int Get() => PlayerPrefs.GetInt(Key, 0);

    /// <summary>Saves the score if it beats the stored one. Returns true if it is a new record.</summary>
    public static bool TrySet(int score)
    {
        if (score <= Get()) return false;

        PlayerPrefs.SetInt(Key, score);
        PlayerPrefs.Save();
        return true;
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}

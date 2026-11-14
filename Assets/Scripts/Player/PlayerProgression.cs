using System;
using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    [Header("Leveling")]
    [SerializeField] private int startLevel = 1;
    [SerializeField] private float baseXpToLevel = 5f;
    [SerializeField] private float levelExponent = 1.35f; // higher = steeper curve
    [SerializeField] private float xpMultiplier = 1f;

    [Header("Scrap magnet")]
    [SerializeField] private float magnetRadius = 3f;

    public int Level { get; private set; }
    public float CurrentXP { get; private set; }
    public float XPToNext => Mathf.Round(baseXpToLevel * Mathf.Pow(Level, levelExponent));
    public float MagnetRadius => magnetRadius;

    public event Action<float, float> OnXPChanged; // current, needed
    public event Action<int> OnLevelUp;            // new level

    private void Awake()
    {
        Instance = this;
        Level = startLevel;
    }

    private void Start()
    {
        OnXPChanged?.Invoke(CurrentXP, XPToNext);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddXP(float amount)
    {
        CurrentXP += amount * xpMultiplier;

        // Loop in case a big pickup gives several levels at once
        while (CurrentXP >= XPToNext)
        {
            CurrentXP -= XPToNext;
            Level++;
            OnLevelUp?.Invoke(Level);
        }

        OnXPChanged?.Invoke(CurrentXP, XPToNext);
    }

    // For upgrade cards (e.g. "Magnet +25%", "XP +10%")
    public void IncreaseMagnetRadius(float amount) => magnetRadius += amount;
    public void IncreaseXpMultiplier(float amount) => xpMultiplier += amount;
}

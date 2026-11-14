using System;
using UnityEngine;

/// <summary>One card on the level-up screen. Weapons and stat boosts both produce these.</summary>
public class UpgradeOption
{
    public string Title;
    public string Description;
    public Sprite Icon;
    public Action Apply;

    public UpgradeOption(string title, string description, Sprite icon, Action apply)
    {
        Title = title;
        Description = description;
        Icon = icon;
        Apply = apply;
    }
}

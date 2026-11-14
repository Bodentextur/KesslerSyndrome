using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    [Header("Weapons the player can find")]
    [SerializeField] private WeaponBase[] availableWeapons;

    // Keyed by weapon type, so a weapon already placed on the player in the scene
    // (e.g. your starting PulseLaser) is recognised as owned.
    private readonly Dictionary<Type, WeaponBase> owned = new Dictionary<Type, WeaponBase>();

    public IReadOnlyCollection<WeaponBase> OwnedWeapons => owned.Values;

    private void Awake()
    {
        foreach (WeaponBase w in GetComponentsInChildren<WeaponBase>())
            owned[w.GetType()] = w;
    }

    /// <summary>Adds the weapon if new, otherwise levels up the one we already have.</summary>
    public void AddOrUpgrade(WeaponBase prefab)
    {
        Type type = prefab.GetType();

        if (owned.TryGetValue(type, out WeaponBase existing))
        {
            existing.LevelUp();
            return;
        }

        WeaponBase instance = Instantiate(prefab, transform);
        instance.transform.localPosition = Vector3.zero;
        owned[type] = instance;
    }

    /// <summary>Every weapon-related card that is currently valid (new weapons + non-maxed upgrades).</summary>
    public List<UpgradeOption> GetWeaponOptions()
    {
        var options = new List<UpgradeOption>();

        foreach (WeaponBase prefab in availableWeapons)
        {
            if (prefab == null) continue;
            WeaponBase captured = prefab; // avoid closure surprises

            if (owned.TryGetValue(prefab.GetType(), out WeaponBase existing))
            {
                if (existing.IsMaxLevel) continue;

                options.Add(new UpgradeOption(
                    $"{existing.WeaponName}  Lv {existing.Level} > {existing.Level + 1}",
                    existing.GetUpgradeText(),
                    existing.Icon,
                    () => AddOrUpgrade(captured)));
            }
            else
            {
                options.Add(new UpgradeOption(
                    $"New: {prefab.WeaponName}",
                    prefab.Description,
                    prefab.Icon,
                    () => AddOrUpgrade(captured)));
            }
        }

        return options;
    }
}

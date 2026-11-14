using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    [Serializable]
    public class Card
    {
        public Button button;
        public TMP_Text title;
        public TMP_Text description;
        public Image icon;
    }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Card[] cards = new Card[3];
    [SerializeField] private WeaponInventory inventory;
    [SerializeField] private PlayerProgression progression;
    [SerializeField] private PlayerController player;

    [Header("Stat upgrades")]
    [SerializeField] private float magnetBonus = 1.5f;
    [SerializeField] private float xpBonus = 0.15f;
    [SerializeField] private float repairAmount = 30f;
    [SerializeField] private Sprite magnetIcon;
    [SerializeField] private Sprite xpIcon;
    [SerializeField] private Sprite repairIcon;

    [Header("Audio")]
    [SerializeField] private AudioClip levelUpSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.4f;

    private int pendingLevelUps;
    private bool isShowing;

    private void Start()
    {
        panel.SetActive(false);
        progression.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (progression != null) progression.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        pendingLevelUps++;
        if (!isShowing) ShowNext();
    }

    private void ShowNext()
    {
        // Don't open the screen over a dead player
        if (pendingLevelUps <= 0 || player.isDead) return;

        pendingLevelUps--;
        isShowing = true;

        if (levelUpSfx != null)
            AudioManager.Play(levelUpSfx, sfxVolume);

        List<UpgradeOption> choices = PickChoices(cards.Length);

        for (int i = 0; i < cards.Length; i++)
        {
            Card card = cards[i];

            if (i >= choices.Count)
            {
                card.button.gameObject.SetActive(false);
                continue;
            }

            UpgradeOption option = choices[i];
            card.button.gameObject.SetActive(true);
            card.title.text = option.Title;
            card.description.text = option.Description;
            card.icon.enabled = option.Icon != null;
            card.icon.sprite = option.Icon;

            card.button.onClick.RemoveAllListeners();
            card.button.onClick.AddListener(() => Choose(option));
        }

        panel.SetActive(true);
        GameManager.Instance.SetState(GameState.LevelUp);
    }

    private void Choose(UpgradeOption option)
    {
        option.Apply?.Invoke();

        panel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
        isShowing = false;

        // Several levels gained at once: show the next screen right away
        if (pendingLevelUps > 0) ShowNext();
    }

    // ---------- Choosing which cards to offer ----------

    private List<UpgradeOption> PickChoices(int count)
    {
        var pool = new List<UpgradeOption>();
        pool.AddRange(inventory.GetWeaponOptions());
        pool.AddRange(GetStatOptions());

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool.GetRange(0, Mathf.Min(count, pool.Count));
    }

    private List<UpgradeOption> GetStatOptions()
    {
        return new List<UpgradeOption>
        {
            new UpgradeOption("Magnet Boost", $"Scrap pull radius +{magnetBonus:0.#}", magnetIcon,
                () => progression.IncreaseMagnetRadius(magnetBonus)),

            new UpgradeOption("Salvage Efficiency", $"+{xpBonus * 100:0}% scrap XP", xpIcon,
                () => progression.IncreaseXpMultiplier(xpBonus)),

            new UpgradeOption("Hull Repair", $"Restore {repairAmount:0} hull integrity", repairIcon,
                () => player.Heal(repairAmount)),
        };
    }
}

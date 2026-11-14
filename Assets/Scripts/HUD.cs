using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerProgression progression;

    [Header("In-game HUD")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider xpBar;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private float barSmoothing = 8f;

    [Header("Game over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalBestText;
    [SerializeField] private GameObject newRecordLabel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private float healthTarget = 1f;
    private float xpTarget;

    private void Start()
    {
        var gm = GameManager.Instance;

        player.OnHealthChanged += HandleHealth;
        progression.OnXPChanged += HandleXP;
        gm.OnScoreChanged += HandleScore;
        gm.OnGameOver += HandleGameOver;

        // Pull the starting values ourselves: other scripts may have fired their events before we subscribed
        HandleHealth(player.CurrentHealth, player.MaxHealth);
        HandleXP(progression.CurrentXP, progression.XPToNext);
        HandleScore(gm.Score);
        healthBar.value = healthTarget;
        xpBar.value = xpTarget;

        gameOverPanel.SetActive(false);
        restartButton.onClick.AddListener(() => gm.Restart());
        menuButton.onClick.AddListener(() => gm.GoToMenu());
    }

    private void OnDestroy()
    {
        if (player != null) player.OnHealthChanged -= HandleHealth;
        if (progression != null) progression.OnXPChanged -= HandleXP;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= HandleScore;
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
    }

    private void Update()
    {
        // Smooth the bars (unscaled, so they still animate during the level-up pause)
        float k = 1f - Mathf.Exp(-barSmoothing * Time.unscaledDeltaTime);
        healthBar.value = Mathf.Lerp(healthBar.value, healthTarget, k);
        xpBar.value = Mathf.Lerp(xpBar.value, xpTarget, k);
    }

    private void HandleHealth(float current, float max)
    {
        healthTarget = max > 0f ? current / max : 0f;
    }

    private void HandleXP(float current, float needed)
    {
        xpTarget = needed > 0f ? current / needed : 0f;
    }

    private void HandleScore(int score)
    {
        scoreText.text = score.ToString("N0");
    }

    private void HandleGameOver(int finalScore, bool isNewRecord)
    {
        finalScoreText.text = $"SCORE {finalScore:N0}";
        finalBestText.text = $"BEST {HighScore.Get():N0}";
        newRecordLabel.SetActive(isNewRecord);
        gameOverPanel.SetActive(true);
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, LevelUp, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("Scoring (index = debris size tier: 0 small, 1 medium, 2 large)")]
    [SerializeField] private int[] pointsByTier = { 10, 25, 50 };

    [Header("Flow")]
    [SerializeField] private float gameOverDelay = 1.5f;     // lets the death effects play before the panel shows
    [SerializeField] private string menuSceneName = "MainMenu";

    public GameState State { get; private set; } = GameState.Playing;
    public int Score { get; private set; }
    public float SurvivalTime { get; private set; }
    public int HighScoreValue => HighScore.Get();

    public event Action<int> OnScoreChanged;
    public event Action<GameState> OnStateChanged;
    public event Action<int, bool> OnGameOver; // final score, is new highscore

    private void Awake()
    {
        // Singleton: keep the first instance, destroy any duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Fresh run: make sure nothing is left paused from a previous scene
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        State = GameState.Playing;
    }

    private void OnEnable()
    {
        Debris.OnDebrisDestroyed += HandleDebrisDestroyed; // static event: must unsubscribe
    }

    private void OnDisable()
    {
        Debris.OnDebrisDestroyed -= HandleDebrisDestroyed;
    }

    private void Start()
    {
        if (player != null) player.OnDied += HandlePlayerDied;
    }

    private void OnDestroy()
    {
        if (player != null) player.OnDied -= HandlePlayerDied;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (State == GameState.Playing)
            SurvivalTime += Time.deltaTime;
    }

    // ---------- Score ----------

    private void HandleDebrisDestroyed(Debris debris)
    {
        int tier = Mathf.Clamp(debris.SizeTier, 0, pointsByTier.Length - 1);
        AddScore(pointsByTier[tier]);
    }

    public void AddScore(int amount)
    {
        if (State == GameState.GameOver) return;
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    // ---------- State ----------

    /// <summary>Single place that controls Time.timeScale, so pause menu and level-up screen can't fight.</summary>
    public void SetState(GameState newState)
    {
        if (State == newState || State == GameState.GameOver) return;

        State = newState;
        Time.timeScale = (newState == GameState.Paused || newState == GameState.LevelUp) ? 0f : 1f;
        OnStateChanged?.Invoke(newState);
    }

    /// <summary>For the pause menu. Does nothing during level-up or game over.</summary>
    public void TogglePause()
    {
        if (State == GameState.Playing) SetState(GameState.Paused);
        else if (State == GameState.Paused) SetState(GameState.Playing);
    }

    // ---------- Game over ----------

    private void HandlePlayerDied()
    {
        if (State == GameState.GameOver) return;

        // Make sure a pending pause can't linger
        Time.timeScale = 1f;
        State = GameState.GameOver;
        OnStateChanged?.Invoke(State);

        bool isNewRecord = HighScore.TrySet(Score); // saved right away, so it isn't lost if the app closes
        StartCoroutine(GameOverRoutine(isNewRecord));
    }

    private IEnumerator GameOverRoutine(bool isNewRecord)
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
        OnGameOver?.Invoke(Score, isNewRecord);
    }

    // ---------- Scene flow  ----------

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}

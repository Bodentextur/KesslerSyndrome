using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game"; 

    [Header("UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text highScoreText;        
    private void Start()
    {
        Time.timeScale = 1f;

        playButton.onClick.AddListener(Play);
        exitButton.onClick.AddListener(Exit);

        if (highScoreText != null)
            highScoreText.text = $"BEST SCORE  {HighScore.Get():N0}";
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; 
#else
        Application.Quit();
#endif
    }
}

using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;      // back to the main menu

    private void Start()
    {
        panel.SetActive(false);

        resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
        quitButton.onClick.AddListener(() => GameManager.Instance.GoToMenu());

        // The panel simply mirrors the game state, so nothing else needs to know about it
        GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        // Update still runs while Time.timeScale is 0, so Escape works for both pausing and resuming.
        // TogglePause() ignores the call during level-up and game over.
        if (EscapePressed())
            GameManager.Instance.TogglePause();
    }

    private void HandleStateChanged(GameState state)
    {
        panel.SetActive(state == GameState.Paused);
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}

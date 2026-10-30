using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private string mainMenuScene = "Main Menu";
    private bool isPaused = false;

    private void Start()
    {
        SetPaused(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetPaused(!isPaused);
        }
    }

    private void OnDestroy()
    {
        // never leave the game frozen when the scene changes
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void SetPaused(bool value)
    {
        isPaused = value;
        if (pausePanel != null) pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        // timeScale does not stop audio: pause/resume all sounds and the music
        AudioListener.pause = isPaused;
    }

    // hook these to the pause panel buttons
    public void Resume()
    {
        SetPaused(false);
    }

    public void ReloadScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuScene);
    }
}
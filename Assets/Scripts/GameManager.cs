using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public static bool IsInstanceValid => instance != null;

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TextMeshProUGUI timerText;

    private bool isPaused = false;
    private float elapsedTime = 0f;

    private string gameScene = "Game";
    private string mainMenuScene = "MainMenu";

    private void Awake()
    {
        InitializeSingleton();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateGameTimer();
    }

    public void RestartGame()
    {
        ResetTimeScale();
        LoadCurrentScene();
    }

    public void PlayGame()
    {
        ResetTimeScale();
        LoadScene(gameScene);
    }

    public void MainMenu()
    {
        ResetTimeScale();
        LoadScene(mainMenuScene);
    }

    // `forcePause` parametresi verilirse, oyunun durumunu doğrudan o değere ayarlar.
    // Eğer verilmezse, mevcut durumu tersine çevirir (duruyorsa başlatır, çalışıyorsa durdurur).
    public void Pause(bool? forcePause = null)
    {
        bool newPauseState = forcePause ?? !isPaused;
        if (newPauseState == isPaused) return;

        isPaused = newPauseState;
        UpdateTimeScale();
        UpdatePauseUI();
    }

    private void InitializeSingleton()
    {
        if (instance == null)
        {
            instance = this;

        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && IsPlayerActive())
        {
            Pause();
        }
    }

    private bool IsPlayerActive()
    {
        return PlayerController.instance != null;
    }

    // Oyunun başlangıcından itibaren geçen süreyi sayar ve ekranda gösterir.
    private void UpdateGameTimer()
    {
        if (timerText == null)
            return;

        elapsedTime += Time.deltaTime;
        DisplayFormattedTime();
    }

    // Geçen süreyi "dakika:saniye" formatında ekrana yazdırır.
    private void DisplayFormattedTime()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void LoadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    private void UpdateTimeScale()
    {
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void UpdatePauseUI()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }
    }
}
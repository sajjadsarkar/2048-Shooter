using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu2048 : MonoBehaviour
{
    private const string MusicKey = "2048_Music";
    private const string SoundKey = "2048_Sound";

    [SerializeField] private GameObject panel;
    [SerializeField] private AudioSource music;
    [SerializeField] private Image[] musicIcons;
    [SerializeField] private Image[] soundIcons;

    private GameOverManager2048 gameOver;

    private void Start()
    {
        gameOver = FindObjectOfType<GameOverManager2048>();
        panel.SetActive(false);
        ApplyAudio();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (panel.activeSelf) Resume();
        else if (Time.timeScale > 0f && !gameOver.IsGameOver) Open();
    }

    public void Open()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Restart()
    {
        MainMenuManager2048.SkipMenu = true;
        Reload();
    }

    public void Home() => Reload();

    public void ToggleMusic()
    {
        PlayerPrefs.SetInt(MusicKey, PlayerPrefs.GetInt(MusicKey, 1) ^ 1);
        ApplyAudio();
    }

    public void ToggleSound()
    {
        PlayerPrefs.SetInt(SoundKey, PlayerPrefs.GetInt(SoundKey, 1) ^ 1);
        ApplyAudio();
    }

    public void ToggleTheme() => Theme2048.Toggle();

    private void ApplyAudio()
    {
        bool musicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        bool soundOn = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        if (music != null) music.mute = !musicOn;
        if (AudioManager2048.Instance != null) AudioManager2048.Instance.ToggleSoundEffects(soundOn);
        foreach (Image i in musicIcons) i.canvasRenderer.SetAlpha(musicOn ? 1f : 0.35f);
        foreach (Image i in soundIcons) i.canvasRenderer.SetAlpha(soundOn ? 1f : 0.35f);
    }

    private void Reload()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

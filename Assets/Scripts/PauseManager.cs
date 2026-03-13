using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public bool IsPaused { get; private set; } = false;

    [Header("UI")]
    [Tooltip("The pause overlay panel to show/hide.")]
    public GameObject PausePanel;

    public static System.Action OnPaused;
    public static System.Action OnResumed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameEvents.OnGameOver += ForceResume;
        GameEvents.OnVictory  += ForceResume;
        GameEvents.OnShopOpened += ForceResume;
    }

    void OnDestroy()
    {
        GameEvents.OnGameOver   -= ForceResume;
        GameEvents.OnVictory    -= ForceResume;
        GameEvents.OnShopOpened -= ForceResume;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else          TryPause();
    }

    void TryPause()
    {
        if (GameManager.Instance == null) return;

        var state = GameManager.Instance.State;

        if (state != GameState.RoundActive && state != GameState.Wave) return;

        IsPaused = true;
        Time.timeScale = 0f;

        if (PausePanel != null) PausePanel.SetActive(true);

        OnPaused?.Invoke();
        Debug.Log("[PauseManager] Game paused.");
    }

    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;

        if (PausePanel != null) PausePanel.SetActive(false);

        OnResumed?.Invoke();
        Debug.Log("[PauseManager] Game resumed.");
    }

    void ForceResume()
    {
        if (IsPaused) Resume();
    }
}

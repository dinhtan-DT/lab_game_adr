using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int Score = 0;
    public int HighScore = 0;

    private bool gameOverTriggered = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        gameOverTriggered = false;
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHighScore(HighScore);
        }

        // Bootstrap UI managers nếu chưa có trong scene
        EnsureUIManagers();
    }

    /// <summary>Đảm bảo GameOverUI, WinUI, LevelManager tồn tại trong scene</summary>
    private void EnsureUIManagers()
    {
        if (FindFirstObjectByType<GameOverUI>() == null)
        {
            GameObject goUI = new GameObject("GameOverUI");
            goUI.AddComponent<GameOverUI>();
        }
        if (FindFirstObjectByType<WinUI>() == null)
        {
            GameObject winUI = new GameObject("WinUI");
            winUI.AddComponent<WinUI>();
        }
        if (FindFirstObjectByType<LevelManager>() == null)
        {
            GameObject lm = new GameObject("LevelManager");
            lm.AddComponent<LevelManager>();
        }
    }

    public void AddScore(int amount)
    {
        Score += amount;
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateScore(Score);
        }

        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateHighScore(HighScore);
            }
        }
    }

    /// <summary>Kích hoạt trạng thái Game Over (HP = 0)</summary>
    public void TriggerGameOver()
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;

        // Lưu HighScore lần cuối
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }

        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Show();
    }

    /// <summary>Reset flag khi bắt đầu lại game</summary>
    public void ResetGameOver()
    {
        gameOverTriggered = false;
        Score = 0;
    }

    /// <summary>Reset flag khi sang màn mới (giữ lại Score)</summary>
    public void ResetForNewLevel()
    {
        gameOverTriggered = false;
    }
}

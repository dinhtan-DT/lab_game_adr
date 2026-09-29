using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    public Image hpBar;
    public Image kiBar;
    public Text hpText;
    public Text kiText;
    public Text scoreText;
    public Text highScoreText;

    public Button soundButton;
    public Button musicButton;

    private Color soundOnColor = Color.green;
    private Color soundOffColor = Color.red;
    private Color musicOnColor = Color.green;
    private Color musicOffColor = Color.red;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Ép HUDManager giãn toàn màn hình để các Neo (Anchors) của con hoạt động đúng
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (soundButton != null)
        {
            soundButton.onClick.RemoveAllListeners();
            soundButton.onClick.AddListener(ToggleSound);
        }
        if (musicButton != null)
        {
            musicButton.onClick.RemoveAllListeners();
            musicButton.onClick.AddListener(ToggleMusic);
        }
        UpdateAudioButtons();
    }

    public void UpdateHP(float current, float max)
    {
        if (hpBar != null)
        {
            RectTransform rt = hpBar.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 maxAnchor = rt.anchorMax;
                maxAnchor.x = Mathf.Clamp01(current / max);
                rt.anchorMax = maxAnchor;
            }
        }
        if (hpText != null)
        {
            hpText.text = "HP: " + Mathf.RoundToInt(current) + "/" + Mathf.RoundToInt(max);
        }
    }

    public void UpdateKi(float current, float max)
    {
        if (kiBar != null)
        {
            RectTransform rt = kiBar.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 maxAnchor = rt.anchorMax;
                maxAnchor.x = Mathf.Clamp01(current / max);
                rt.anchorMax = maxAnchor;
            }
        }
        if (kiText != null)
        {
            kiText.text = "KI: " + Mathf.RoundToInt(current) + "/" + Mathf.RoundToInt(max);
        }
    }

    public void UpdateKI(float current, float max) => UpdateKi(current, max);

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + score;
        }
    }

    public void UpdateHighScore(int highScore)
    {
        if (highScoreText != null)
        {
            highScoreText.text = "HIGH SCORE: " + highScore;
        }
    }

    public void ToggleSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSound();
            UpdateAudioButtons();
        }
    }

    public void ToggleMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMusic();
            UpdateAudioButtons();
        }
    }

    private void UpdateAudioButtons()
    {
        if (AudioManager.Instance == null) return;

        if (soundButton != null)
        {
            Text btnText = soundButton.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = AudioManager.Instance.IsSoundMuted ? "SOUND OFF" : "SOUND ON";
            
            ColorBlock cb = soundButton.colors;
            cb.normalColor = AudioManager.Instance.IsSoundMuted ? soundOffColor : soundOnColor;
            soundButton.colors = cb;
        }

        if (musicButton != null)
        {
            Text btnText = musicButton.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = AudioManager.Instance.IsMusicMuted ? "MUSIC OFF" : "MUSIC ON";

            ColorBlock cb = musicButton.colors;
            cb.normalColor = AudioManager.Instance.IsMusicMuted ? musicOffColor : musicOnColor;
            musicButton.colors = cb;
        }
    }

    public void ShowGameOver()
    {
        GameObject panelObj = new GameObject("GameOverPanel");
        panelObj.transform.SetParent(transform, false);
        UnityEngine.UI.Image panelImg = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0, 0, 0, 0.8f);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;

        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(panelObj.transform, false);
        UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 80;
        txt.color = Color.red;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = "GAME OVER";
        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchoredPosition = new Vector2(0, 50);

        GameObject btnObj = new GameObject("ReplayButton");
        btnObj.transform.SetParent(panelObj.transform, false);
        UnityEngine.UI.Image btnImg = btnObj.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = Color.white;
        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchoredPosition = new Vector2(0, -50);
        btnRt.sizeDelta = new Vector2(200, 60);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        UnityEngine.UI.Text btnTxt = btnTextObj.AddComponent<UnityEngine.UI.Text>();
        btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnTxt.fontSize = 30;
        btnTxt.color = Color.black;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.text = "REPLAY";
        RectTransform btnTxtRt = btnTextObj.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(() => {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
    }
}

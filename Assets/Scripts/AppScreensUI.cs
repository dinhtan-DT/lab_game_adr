using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AppScreensUI : MonoBehaviour
{
    public static AppScreensUI Instance;

    private enum Screen { None, Home, Settings, Ranking }

    private readonly Color overlayColor = new Color(0.025f, 0.055f, 0.08f, 0.97f);
    private readonly Color accentColor = new Color(0.12f, 0.62f, 0.56f, 1f);
    private Canvas canvas;
    private GameObject homePanel;
    private GameObject settingsPanel;
    private GameObject rankingPanel;
    private Text soundStatus;
    private Text musicStatus;
    private Text rankingText;
    private Screen previousScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowHome()
    {
        EnsureBuilt();
        ShowOnly(Screen.Home);
    }

    public void ShowSettings()
    {
        EnsureBuilt();
        previousScreen = CurrentVisibleScreen();
        RefreshAudioStatus();
        ShowOnly(Screen.Settings);
    }

    public void ShowRanking()
    {
        EnsureBuilt();
        previousScreen = CurrentVisibleScreen();
        RefreshRanking();
        ShowOnly(Screen.Ranking);
    }

    private void EnsureBuilt()
    {
        if (canvas != null) return;

        GameObject canvasObject = new GameObject("AppScreensCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(eventSystemObject);
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        homePanel = CreatePanel("HomeScreen", "DRAGON BALL 2D", font);
        AddButton(homePanel.transform, "PLAY FROM LEVEL 1", 90f, StartNewGame, font);
        AddButton(homePanel.transform, "SETTINGS", 15f, ShowSettings, font);
        AddButton(homePanel.transform, "RANKING", -60f, ShowRanking, font);
        AddButton(homePanel.transform, "LEVEL SELECT", -145f, ShowLevelSelect, font);

        settingsPanel = CreatePanel("SettingsScreen", "SETTINGS", font);
        soundStatus = AddButton(settingsPanel.transform, "SOUND", 55f, ToggleSound, font);
        musicStatus = AddButton(settingsPanel.transform, "MUSIC", -20f, ToggleMusic, font);
        AddButton(settingsPanel.transform, "BACK", -115f, ReturnToPrevious, font);

        rankingPanel = CreatePanel("RankingScreen", "HIGH ACHIEVEMENTS", font);
        rankingText = CreateText(rankingPanel.transform, "RankingSummary", "", font, 25, new Vector2(0f, 5f), new Vector2(560f, 150f));
        AddButton(rankingPanel.transform, "BACK", -125f, ReturnToPrevious, font);

        ShowOnly(Screen.None);
    }

    private GameObject CreatePanel(string objectName, string title, Font font)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = overlayColor;

        CreateText(panel.transform, "Title", title, font, 38, new Vector2(0f, 225f), new Vector2(700f, 70f));
        return panel;
    }

    private Text CreateText(Transform parent, string objectName, string value, Font font, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Text AddButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction action, Font font)
    {
        GameObject buttonObject = new GameObject("Button_" + label.Replace(' ', '_'));
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, y);
        buttonRect.sizeDelta = new Vector2(300f, 58f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = accentColor;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        return CreateText(buttonObject.transform, "Label", label, font, 21, Vector2.zero, new Vector2(290f, 54f));
    }

    private void ShowOnly(Screen screen)
    {
        homePanel.SetActive(screen == Screen.Home);
        settingsPanel.SetActive(screen == Screen.Settings);
        rankingPanel.SetActive(screen == Screen.Ranking);
        Time.timeScale = 0f;
    }

    private void HideScreens()
    {
        ShowOnly(Screen.None);
    }

    private void ReturnToPrevious()
    {
        ShowOnly(previousScreen);
    }

    private Screen CurrentVisibleScreen()
    {
        if (homePanel != null && homePanel.activeSelf) return Screen.Home;
        if (settingsPanel != null && settingsPanel.activeSelf) return Screen.Settings;
        if (rankingPanel != null && rankingPanel.activeSelf) return Screen.Ranking;
        return Screen.None;
    }

    private void ShowLevelSelect()
    {
        ShowOnly(Screen.None);
        if (LevelManager.Instance != null)
            LevelManager.Instance.OpenLevelMenu();
        else
            Time.timeScale = 1f;
    }

    private void StartNewGame()
    {
        ShowOnly(Screen.None);
        if (GameOverUI.Instance != null) GameOverUI.Instance.Hide();
        if (WinUI.Instance != null) WinUI.Instance.Hide();
        if (LevelManager.Instance != null) LevelManager.Instance.GoToLevel1();
        else Time.timeScale = 1f;
    }

    private void ToggleSound()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.ToggleSound();
        RefreshAudioStatus();
    }

    private void ToggleMusic()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.ToggleMusic();
        RefreshAudioStatus();
    }

    private void RefreshAudioStatus()
    {
        bool soundMuted = AudioManager.Instance != null && AudioManager.Instance.IsSoundMuted;
        bool musicMuted = AudioManager.Instance != null && AudioManager.Instance.IsMusicMuted;
        if (soundStatus != null) soundStatus.text = soundMuted ? "SOUND: OFF" : "SOUND: ON";
        if (musicStatus != null) musicStatus.text = musicMuted ? "MUSIC: OFF" : "MUSIC: ON";
    }

    private void RefreshRanking()
    {
        if (rankingText == null) return;
        int highScore = GameManager.Instance != null ? GameManager.Instance.HighScore : PlayerPrefs.GetInt("HighScore", 0);
        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        int highestLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        rankingText.text = $"HIGH SCORE   {highScore}\nCURRENT SCORE   {score}\nLEVELS UNLOCKED   {highestLevel} / 3";
    }
}
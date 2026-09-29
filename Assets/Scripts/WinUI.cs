using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý màn hình thắng (Win):
/// - Hiển thị ảnh win.png (Resources) + thông điệp theo từng level
/// - 3 nút: Replay, Next Level (hoặc Home nếu Level 3), High Achievements
/// </summary>
public class WinUI : MonoBehaviour
{
    public static WinUI Instance;

    private GameObject panel;
    private bool isShowing = false;

    private static readonly Color OVERLAY_COLOR = new Color(0f, 0f, 0f, 0.70f);
    private static readonly Color PANEL_COLOR   = new Color(0.02f, 0.06f, 0.12f, 0.97f);
    private static readonly Color GOLD_COLOR    = new Color(1f, 0.85f, 0.1f, 1f);

    private Text subtitleText;
    private int displayedLevel = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        BuildUI();
    }

    public void Show(int level)
    {
        if (isShowing) return;

        // WinUI có thể được tạo runtime ngay lúc bắt đầu game, trước Start().
        if (panel == null)
            BuildUI();

        displayedLevel = level;
        isShowing = true;

        // Cập nhật subtitle theo level
        if (subtitleText != null)
        {
            if (level == 1) subtitleText.text = "🎉 CHIẾN THẮNG TRÁI ĐẤT!\n🔓 ĐÃ MỞ KHÓA CẤP 2 (ĐẠI CHIẾN 3 PICCOLO)!\nBấm NEXT để tiếp tục chơi!";
            else if (level == 2) subtitleText.text = "🎉 CHIẾN THẮNG NAMEK!\n🔓 ĐÃ MỞ KHÓA CẤP 3 (VŨ TRỤ - CELL BOSS)!\nBấm NEXT để tiếp tục chơi!";
            else subtitleText.text = "👑 GOKU CHIẾN THẮNG TOÀN DIỆN!\nBạn đã hoàn thành phá đảo toàn bộ game!";
        }

        // Refresh nút Next Level
        RefreshNextLevelButton(level);

        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayShoot(); // Âm thanh thắng (dùng tạm shoot)
    }

    public void Hide()
    {
        isShowing = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private Button nextLevelBtn;

    private void BuildUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cObj = new GameObject("UICanvas");
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cObj.AddComponent<GraphicRaycaster>();
        }

        // ---- PANEL ----
        panel = new GameObject("WinPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.SetActive(false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        panel.AddComponent<Image>().color = OVERLAY_COLOR;

        // ---- CENTER BOX ----
        GameObject box = MakeRect("Box", panel.transform);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot     = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(720f, 520f);
        boxRt.anchoredPosition = Vector2.zero;
        box.AddComponent<Image>().color = PANEL_COLOR;

        // ---- ẢNH WIN (win.png) ----
        Sprite winSprite = Resources.Load<Sprite>("win");
        if (winSprite != null)
        {
            GameObject imgObj = MakeRect("WinImage", box.transform);
            RectTransform imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.anchorMin = new Vector2(0.05f, 0.50f);
            imgRt.anchorMax = new Vector2(0.95f, 0.97f);
            imgRt.sizeDelta = Vector2.zero;
            Image img = imgObj.AddComponent<Image>();
            img.sprite = winSprite;
            img.preserveAspect = true;
        }
        else
        {
            // Fallback: chữ "YOU WIN!" màu vàng
            GameObject titleObj = MakeRect("WinTitle", box.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.68f);
            titleRt.anchorMax = new Vector2(1f, 0.97f);
            titleRt.sizeDelta = Vector2.zero;
            Text title = titleObj.AddComponent<Text>();
            title.text = "YOU WIN!";
            title.fontSize = 80;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = GOLD_COLOR;
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // ---- SUBTITLE ----
        GameObject subObj = MakeRect("Subtitle", box.transform);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.05f, 0.37f);
        subRt.anchorMax = new Vector2(0.95f, 0.55f);
        subRt.sizeDelta = Vector2.zero;
        subtitleText = subObj.AddComponent<Text>();
        subtitleText.text = "";
        subtitleText.fontSize = 22;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = Color.white;
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---- SCORE DISPLAY ----
        GameObject scoreObj = MakeRect("ScoreDisplay", box.transform);
        RectTransform scoreRt = scoreObj.GetComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0.1f, 0.27f);
        scoreRt.anchorMax = new Vector2(0.9f, 0.38f);
        scoreRt.sizeDelta = Vector2.zero;
        Text scoreText = scoreObj.AddComponent<Text>();
        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        scoreText.text = $"Score: {score}";
        scoreText.fontSize = 26;
        scoreText.fontStyle = FontStyle.Bold;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = GOLD_COLOR;
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---- 3 NÚT ----
        float btnY   = -190f;
        float btnGap = 170f;

        // Nút 1: Replay
        MakeButton("btn_win_replay", box.transform, new Vector2(-btnGap, btnY), "▶ REPLAY",
            new Color(0.15f, 0.55f, 0.15f), () => {
                Hide();
                if (GameManager.Instance != null) GameManager.Instance.ResetForNewLevel();
                PlayerA p = FindFirstObjectByType<PlayerA>();
                if (p != null) p.ResetPlayerState();
                if (LevelManager.Instance != null) LevelManager.Instance.ReplayCurrentLevel();
            });

        // Nút 2: Next Level / Home (sẽ refresh khi Show() gọi)
        nextLevelBtn = MakeButton("btn_win_next", box.transform, new Vector2(0f, btnY), "⏭ NEXT",
            new Color(0.60f, 0.20f, 0.05f), () => {
                Hide();
                if (GameManager.Instance != null) GameManager.Instance.ResetForNewLevel();
                PlayerA p = FindFirstObjectByType<PlayerA>();
                if (p != null) p.ResetPlayerState();
                if (LevelManager.Instance != null)
                {
                    if (LevelManager.Instance.currentLevel < 3)
                        LevelManager.Instance.GoToNextLevel();
                    else
                    {
                        if (AppScreensUI.Instance != null) AppScreensUI.Instance.ShowHome();
                        else LevelManager.Instance.GoToLevel1();
                    }
                }
            });

        // Nút 3: High Achievements (bảng điểm cao)
        MakeButton("btn_win_highscore", box.transform, new Vector2(btnGap, btnY), "🏆 RANKING",
            new Color(0.45f, 0.10f, 0.55f), () => {
                if (AppScreensUI.Instance != null) AppScreensUI.Instance.ShowRanking();
                else ShowHighScoreOverlay(box.transform);
            });
    }

    private void RefreshNextLevelButton(int level)
    {
        if (nextLevelBtn == null) return;
        Text lbl = nextLevelBtn.GetComponentInChildren<Text>();
        if (lbl == null) return;
        lbl.text = (level < 3) ? "⏭ NEXT LEVEL" : "🏠 HOME";
    }

    private void ShowHighScoreOverlay(Transform parent)
    {
        // Xóa overlay cũ nếu có
        Transform old = parent.Find("HSOverlay");
        if (old != null) { Destroy(old.gameObject); return; }

        GameObject ov = MakeRect("HSOverlay", parent);
        RectTransform ovRt = ov.GetComponent<RectTransform>();
        ovRt.anchorMin = new Vector2(0.1f, 0.1f);
        ovRt.anchorMax = new Vector2(0.9f, 0.9f);
        ovRt.sizeDelta = Vector2.zero;
        ov.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.15f, 0.97f);

        int hi    = GameManager.Instance != null ? GameManager.Instance.HighScore : 0;
        int score = GameManager.Instance != null ? GameManager.Instance.Score      : 0;

        GameObject txtObj = MakeRect("HSText", ov.transform);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        Text t = txtObj.AddComponent<Text>();
        t.text = $"🏆 HIGH ACHIEVEMENTS 🏆\n\nHigh Score: {hi}\nCurrent Score: {score}\n\n[Nhấn lại để đóng]";
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = GOLD_COLOR;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // ---- Helpers ----
    private GameObject MakeRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private Button MakeButton(string id, Transform parent, Vector2 pos, string label, Color color, System.Action onClick)
    {
        GameObject btnObj = MakeRect(id, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(150f, 55f);
        rt.anchoredPosition = pos;

        Image img = btnObj.AddComponent<Image>();
        img.color = color;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = Color.white;
        cb.pressedColor     = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        GameObject lblObj = MakeRect("Label", btnObj.transform);
        RectTransform lblRt = lblObj.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.sizeDelta = Vector2.zero;
        Text txt = lblObj.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.raycastTarget = false;

        return btn;
    }
}

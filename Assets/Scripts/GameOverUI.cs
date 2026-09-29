using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý màn hình Game Over:
/// - Hiển thị ảnh gameover.png (Resources) + hiệu ứng âm thanh khi HP = 0
/// - 3 nút: Replay, Home Screen, Settings
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    // Panel gốc chứa toàn bộ Game Over UI
    private GameObject panel;
    private bool isShowing = false;

    // Màu overlay
    private static readonly Color OVERLAY_COLOR = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color PANEL_COLOR   = new Color(0.08f, 0.02f, 0.02f, 0.97f);

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

    private void BuildUI()
    {
        // Tìm / tạo Canvas
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
        panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.SetActive(false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = OVERLAY_COLOR;

        // ---- CENTER BOX ----
        GameObject box = MakeRect("Box", panel.transform);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot     = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(700f, 500f);
        boxRt.anchoredPosition = Vector2.zero;
        box.AddComponent<Image>().color = PANEL_COLOR;

        // ---- ẢNH GAME OVER (gameover.png) ----
        Sprite goSprite = Resources.Load<Sprite>("gameover");
        if (goSprite != null)
        {
            GameObject imgObj = MakeRect("GameOverImage", box.transform);
            RectTransform imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.anchorMin = new Vector2(0.05f, 0.52f);
            imgRt.anchorMax = new Vector2(0.95f, 0.97f);
            imgRt.sizeDelta = Vector2.zero;
            Image img = imgObj.AddComponent<Image>();
            img.sprite = goSprite;
            img.preserveAspect = true;
        }
        else
        {
            // Fallback: chữ "GAME OVER" màu đỏ
            GameObject titleObj = MakeRect("GameOverTitle", box.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.62f);
            titleRt.anchorMax = new Vector2(1f, 0.96f);
            titleRt.sizeDelta = Vector2.zero;
            Text title = titleObj.AddComponent<Text>();
            title.text = "GAME OVER";
            title.fontSize = 72;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.15f, 0.15f, 1f);
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // ---- 3 NÚT ----
        float btnY   = -130f;
        float btnGap = 160f;

        MakeButton("btn_go_replay",    box.transform, new Vector2(-btnGap, btnY), "▶ REPLAY",
            new Color(0.15f, 0.55f, 0.15f), () => {
                Hide();
                if (GameManager.Instance != null) GameManager.Instance.ResetGameOver();
                PlayerA p = FindFirstObjectByType<PlayerA>();
                if (p != null) p.ResetPlayerState();
                if (LevelManager.Instance != null) LevelManager.Instance.ReplayCurrentLevel();
            });

        MakeButton("btn_go_home",      box.transform, new Vector2(0f, btnY), "🏠 HOME",
            new Color(0.15f, 0.35f, 0.65f), () => {
                Hide();
                if (GameManager.Instance != null) GameManager.Instance.ResetGameOver();
                PlayerA p = FindFirstObjectByType<PlayerA>();
                if (p != null) p.ResetPlayerState();
                if (LevelManager.Instance != null) LevelManager.Instance.GoToLevel1();
            });

        MakeButton("btn_go_settings",  box.transform, new Vector2(btnGap, btnY), "⚙ SETTINGS",
            new Color(0.50f, 0.35f, 0.10f), () => {
                ToggleSettingsPanel(box.transform);
            });
    }

    // ---- Settings mini-panel (âm thanh) ----
    private GameObject settingsPanel;
    private void ToggleSettingsPanel(Transform parent)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            return;
        }
        settingsPanel = MakeRect("SettingsPanel", parent);
        RectTransform rt = settingsPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, 160f);
        rt.anchoredPosition = new Vector2(0f, 60f);
        settingsPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Sound toggle
        MakeButton("btn_sound", settingsPanel.transform, new Vector2(-70f, 0f), "🔊 Sound",
            new Color(0.2f, 0.5f, 0.2f), () => {
                if (AudioManager.Instance != null) AudioManager.Instance.ToggleSound();
            });
        // Music toggle
        MakeButton("btn_music", settingsPanel.transform, new Vector2(70f, 0f), "🎵 Music",
            new Color(0.2f, 0.2f, 0.5f), () => {
                if (AudioManager.Instance != null) AudioManager.Instance.ToggleMusic();
            });
    }

    public void Show()
    {
        if (isShowing) return;
        isShowing = true;
        if (panel != null) panel.SetActive(true);

        // Dừng game
        Time.timeScale = 0f;

        // Âm thanh Game Over
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayExplosion();
    }

    public void Hide()
    {
        isShowing = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    // ---- Helpers ----
    private GameObject MakeRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void MakeButton(string id, Transform parent, Vector2 pos, string label, Color color, System.Action onClick)
    {
        GameObject btnObj = MakeRect(id, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(140f, 55f);
        rt.anchoredPosition = pos;

        Image img = btnObj.AddComponent<Image>();
        img.color = color;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = Color.white;
        cb.pressedColor     = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        // Label
        GameObject lblObj = MakeRect("Label", btnObj.transform);
        RectTransform lblRt = lblObj.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.sizeDelta = Vector2.zero;
        Text txt = lblObj.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 22;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.raycastTarget = false;
    }
}

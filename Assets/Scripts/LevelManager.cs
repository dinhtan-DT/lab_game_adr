using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton quản lý 3 cấp độ chơi (Level 1, 2, 3), nhiệm vụ, thế giới game,
/// lưu trữ trạng thái người chơi, và hệ thống thanh công cụ toàn màn hình khi bắt đầu,
/// thu gọn ở góc trên bên phải khi chơi, tạm dừng khi mở, và mở khóa tuần tự (Lv1 -> Lv2 -> Lv3).
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Cấp Độ Hiện Tại")]
    public int currentLevel = 1;

    [Header("Mục Tiêu Thắng Mỗi Level")]
    public int killsToWinLevel1 = 3;
    public int killsToWinLevel2 = 1; // 1 Siêu Boss Piccolo
    public int killsToWinLevel3 = 3;

    [Header("Cấu Hình Tốc Độ Theo Level")]
    public float[] enemySpeedPerLevel = { 5f, 7.5f, 9.5f };
    public float[] bgScrollSpeedPerLevel = { 2f, 3.5f, 5f };
    public float[] itemSpawnIntervalPerLevel = { 5f, 4f, 3f };
    public float[] playerSpeedPerLevel = { 8f, 10f, 13f };

    // Kill count trong session hiện tại
    private int sessionKillCount = 0;
    private int piccoloKillCount = 0;
    private bool levelCompleted = false;

    // Lưu trạng thái xuyên level
    private const string PREF_LEVEL    = "SavedLevel";
    private const string PREF_UNLOCKED = "UnlockedLevel";
    private const string PREF_HP       = "SavedHP";
    private const string PREF_KI       = "SavedKI";
    private const string PREF_SCORE    = "SavedScore";

    // UI Elements
    private GameObject hudContainer;
    private GameObject compactQuestBanner;
    private Text questBannerText;
    private GameObject btnCollapsedMenu;
    private GameObject fullscreenToolbarModal;
    private Text saveStatusText;

    // Sound buttons inside toolbar
    private Text soundBtnText;
    private Text musicBtnText;
    private Button soundButton;
    private Button musicButton;

    // Level buttons in modal
    private GameObject[] levelCardObjects = new GameObject[3];

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
            return;
        }

        currentLevel = PlayerPrefs.GetInt(PREF_LEVEL, 1);
    }

    private void Start()
    {
        BuildLevelHUD();
        ApplyLevelSettings();

        // KHI BẮT ĐẦU GAME: Hiển thị thanh công cụ toàn màn hình, tạm dừng game chờ người chơi chọn cấp độ!
        OpenLevelMenu();
    }

    // ────────────────────────────────────────────
    // GIAO DIỆN CẤP ĐỘ, NHIỆM VỤ & THANH CÔNG CỤ
    // ────────────────────────────────────────────
    public void BuildLevelHUD()
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

        if (hudContainer != null) Destroy(hudContainer);

        hudContainer = new GameObject("LevelHUD_Container");
        hudContainer.transform.SetParent(canvas.transform, false);

        // ĐẶC BIỆT QUAN TRỌNG: Ép LevelHUD_Container giãn toàn màn hình (Stretch Full Screen)
        // để các neo (Anchors) của banner nhiệm vụ và nút menu ăn theo đúng cạnh màn hình, không bị kẹt ở giữa!
        RectTransform hudRt = hudContainer.GetComponent<RectTransform>();
        if (hudRt == null) hudRt = hudContainer.AddComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero;
        hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero;
        hudRt.offsetMax = Vector2.zero;
        hudRt.sizeDelta = Vector2.zero;
        hudRt.anchoredPosition = Vector2.zero;
        hudRt.pivot = new Vector2(0.5f, 0.5f);

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. THANH NHIỆM VỤ GỌN GÀNG (Ở mép TRÊN CÙNG GIỮA màn hình, không chắn tầm nhìn)
        compactQuestBanner = new GameObject("CompactQuestBanner");
        compactQuestBanner.transform.SetParent(hudContainer.transform, false);
        RectTransform bannerRt = compactQuestBanner.AddComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.5f, 1f);
        bannerRt.anchorMax = new Vector2(0.5f, 1f);
        bannerRt.pivot     = new Vector2(0.5f, 1f);
        bannerRt.anchoredPosition = new Vector2(0f, -10f);
        bannerRt.sizeDelta = new Vector2(440f, 32f);

        Image bannerBg = compactQuestBanner.AddComponent<Image>();
        bannerBg.color = new Color(0.06f, 0.08f, 0.16f, 0.88f);

        GameObject textObj = new GameObject("QuestText");
        textObj.transform.SetParent(compactQuestBanner.transform, false);
        RectTransform txtRt = textObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        questBannerText = textObj.AddComponent<Text>();
        questBannerText.font = defaultFont;
        questBannerText.fontSize = 15;
        questBannerText.fontStyle = FontStyle.Bold;
        questBannerText.alignment = TextAnchor.MiddleCenter;
        questBannerText.color = Color.yellow;

        // 2. NÚT THU GỌN Ở GÓC TRÊN BÊN PHẢI MÀN HÌNH ([⚙ CẤP ĐỘ / MENU])
        // Đặt ở góc phải màn hình cùng hàng với các nút Sound/Music
        btnCollapsedMenu = new GameObject("Btn_CollapsedMenu");
        btnCollapsedMenu.transform.SetParent(hudContainer.transform, false);
        RectTransform menuBtnRt = btnCollapsedMenu.AddComponent<RectTransform>();
        menuBtnRt.anchorMin = new Vector2(1f, 1f);
        menuBtnRt.anchorMax = new Vector2(1f, 1f);
        menuBtnRt.pivot     = new Vector2(1f, 1f);
        menuBtnRt.anchoredPosition = new Vector2(-340f, -20f);
        menuBtnRt.sizeDelta = new Vector2(150f, 38f);

        Image menuBtnImg = btnCollapsedMenu.AddComponent<Image>();
        menuBtnImg.color = new Color(0.12f, 0.22f, 0.40f, 0.92f);

        Button menuBtn = btnCollapsedMenu.AddComponent<Button>();
        menuBtn.onClick.AddListener(() => OpenLevelMenu());

        GameObject menuBtnTxtObj = new GameObject("Text");
        menuBtnTxtObj.transform.SetParent(btnCollapsedMenu.transform, false);
        RectTransform mbTxtRt = menuBtnTxtObj.AddComponent<RectTransform>();
        mbTxtRt.anchorMin = Vector2.zero;
        mbTxtRt.anchorMax = Vector2.one;
        mbTxtRt.sizeDelta = Vector2.zero;
        Text mbTxt = menuBtnTxtObj.AddComponent<Text>();
        mbTxt.font = defaultFont;
        mbTxt.text = "⚙ MENU / CẤP ĐỘ";
        mbTxt.fontSize = 14;
        mbTxt.fontStyle = FontStyle.Bold;
        mbTxt.alignment = TextAnchor.MiddleCenter;
        mbTxt.color = Color.white;
        mbTxt.raycastTarget = false;

        // 3. THANH CÔNG CỤ TOÀN MÀN HÌNH (Menu Chọn Cấp Độ & Cài Đặt Âm Thanh)
        BuildFullscreenToolbar(canvas.transform, defaultFont);

        // 4. THÔNG BÁO LƯU TRẠNG THÁI (Ngay dưới thanh nhiệm vụ mép trên)
        GameObject saveObj = new GameObject("SaveStatusText");
        saveObj.transform.SetParent(hudContainer.transform, false);
        RectTransform saveRt = saveObj.AddComponent<RectTransform>();
        saveRt.anchorMin = new Vector2(0.5f, 1f);
        saveRt.anchorMax = new Vector2(0.5f, 1f);
        saveRt.pivot     = new Vector2(0.5f, 1f);
        saveRt.anchoredPosition = new Vector2(0f, -46f);
        saveRt.sizeDelta = new Vector2(600f, 26f);
        saveStatusText = saveObj.AddComponent<Text>();
        saveStatusText.font = defaultFont;
        saveStatusText.fontSize = 14;
        saveStatusText.fontStyle = FontStyle.BoldAndItalic;
        saveStatusText.alignment = TextAnchor.MiddleCenter;
        saveStatusText.color = new Color(0.3f, 1f, 0.5f, 1f);
        saveStatusText.text = "";

        UpdateHUDTexts();
    }

    private void BuildFullscreenToolbar(Transform parent, Font font)
    {
        fullscreenToolbarModal = new GameObject("FullscreenToolbarModal");
        fullscreenToolbarModal.transform.SetParent(parent, false);

        RectTransform modalRt = fullscreenToolbarModal.AddComponent<RectTransform>();
        modalRt.anchorMin = Vector2.zero;
        modalRt.anchorMax = Vector2.one;
        modalRt.sizeDelta = Vector2.zero;

        Image modalOverlay = fullscreenToolbarModal.AddComponent<Image>();
        modalOverlay.color = new Color(0.03f, 0.04f, 0.10f, 0.96f);

        // Khung nội dung chính
        GameObject boxObj = new GameObject("ModalBox");
        boxObj.transform.SetParent(fullscreenToolbarModal.transform, false);
        RectTransform boxRt = boxObj.AddComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot     = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(860f, 540f);
        boxRt.anchoredPosition = Vector2.zero;

        Image boxBg = boxObj.AddComponent<Image>();
        boxBg.color = new Color(0.07f, 0.09f, 0.20f, 0.98f);

        // Header Tiêu đề
        GameObject titleObj = new GameObject("HeaderTitle");
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 210f);
        titleRt.sizeDelta = new Vector2(800f, 50f);
        Text titleTxt = titleObj.AddComponent<Text>();
        titleTxt.font = font;
        titleTxt.text = "🐉 DRAGON BALL 2D: CHỌN CẤP ĐỘ & CÀI ĐẶT ⚙";
        titleTxt.fontSize = 25;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(1f, 0.85f, 0.15f);

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 175f);
        subRt.sizeDelta = new Vector2(800f, 30f);
        Text subTxt = subObj.AddComponent<Text>();
        subTxt.font = font;
        subTxt.text = "Chọn cấp độ để bắt đầu chạy game hoặc tùy chỉnh âm thanh bên dưới";
        subTxt.fontSize = 15;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = new Color(0.7f, 0.9f, 1f);

        // TẠO 3 CARD CẤP ĐỘ (Ngang nhau ở giữa)
        float cardWidth = 250f;
        float cardHeight = 220f;
        float[] posXs = { -270f, 0f, 270f };

        for (int i = 0; i < 3; i++)
        {
            int lvl = i + 1;
            GameObject cardObj = new GameObject("LevelCard_" + lvl);
            cardObj.transform.SetParent(boxObj.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(posXs[i], 20f);
            cRt.sizeDelta = new Vector2(cardWidth, cardHeight);

            levelCardObjects[i] = cardObj;
        }

        RefreshLevelCards(font);

        // KHU VỰC NÚT CÀI ĐẶT ÂM THANH & TIẾP TỤC Ở DƯỚI
        float bottomY = -180f;

        // 1. Nút Âm thanh (SFX)
        GameObject soundBtnObj = CreateButtonObj(boxObj.transform, new Vector2(-220f, bottomY), new Vector2(200f, 48f), new Color(0.15f, 0.35f, 0.65f), () => {
            ToggleSound();
        });
        soundButton = soundBtnObj.GetComponent<Button>();
        soundBtnText = soundBtnObj.GetComponentInChildren<Text>();

        // 2. Nút Nhạc nền (Music)
        GameObject musicBtnObj = CreateButtonObj(boxObj.transform, new Vector2(0f, bottomY), new Vector2(200f, 48f), new Color(0.20f, 0.45f, 0.55f), () => {
            ToggleMusic();
        });
        musicButton = musicBtnObj.GetComponent<Button>();
        musicBtnText = musicBtnObj.GetComponentInChildren<Text>();

        // 3. Nút Tiếp Tục Chơi (Đóng Menu)
        CreateButtonObj(boxObj.transform, new Vector2(220f, bottomY), new Vector2(200f, 48f), new Color(0.15f, 0.60f, 0.25f), () => {
            CloseLevelMenu();
        }, "▶ TIẾP TỤC CHƠI");

        UpdateAudioButtonLabels();
    }

    private void RefreshLevelCards(Font font)
    {
        int unlockedLevel = PlayerPrefs.GetInt(PREF_UNLOCKED, 1);

        for (int i = 0; i < 3; i++)
        {
            int lvl = i + 1;
            GameObject cardObj = levelCardObjects[i];
            if (cardObj == null) continue;

            // Xóa components cũ nếu có
            foreach (Transform child in cardObj.transform) Destroy(child.gameObject);

            Image cardBg = cardObj.GetComponent<Image>();
            if (cardBg == null) cardBg = cardObj.AddComponent<Image>();

            Button cardBtn = cardObj.GetComponent<Button>();
            if (cardBtn == null) cardBtn = cardObj.AddComponent<Button>();
            cardBtn.onClick.RemoveAllListeners();

            bool isUnlocked = lvl <= unlockedLevel;
            bool isCurrent = lvl == currentLevel;

            Color bgColor;
            string title;
            string statusStr;
            string descStr;

            if (lvl == 1)
            {
                bgColor = isCurrent ? new Color(0.18f, 0.65f, 0.25f) : new Color(0.12f, 0.45f, 0.18f);
                title = "⭐ CẤP 1\nTRÁI ĐẤT";
                statusStr = isCurrent ? "[ĐANG CHỌN]" : "[MỞ KHÓA]";
                descStr = "🎯 Mục tiêu: Tiêu diệt 3 Vegeta\n⚔ Độ khó: Dễ\n⚡ Tốc độ: Cơ bản";
            }
            else if (lvl == 2)
            {
                if (isUnlocked)
                {
                    bgColor = isCurrent ? new Color(0.15f, 0.60f, 0.85f) : new Color(0.10f, 0.42f, 0.65f);
                    title = "⭐⭐ CẤP 2\nNAMEK";
                    statusStr = isCurrent ? "[ĐANG CHỌN]" : "[MỞ KHÓA]";
                    descStr = "🎯 Mục tiêu: Đánh bại Siêu Boss Piccolo\n💥 AI nhân bản kỹ năng Player A\n⚡ Kích thước khủng ~235x235";
                }
                else
                {
                    bgColor = new Color(0.22f, 0.24f, 0.28f, 0.70f);
                    title = "🔒 CẤP 2\nNAMEK";
                    statusStr = "[🔒 KHÓA]";
                    descStr = "Cần hoàn thành Cấp 1 để mở khóa!";
                }
            }
            else
            {
                if (isUnlocked)
                {
                    bgColor = isCurrent ? new Color(0.70f, 0.20f, 0.75f) : new Color(0.50f, 0.12f, 0.58f);
                    title = "⭐⭐⭐ CẤP 3\nVŨ TRỤ";
                    statusStr = isCurrent ? "[ĐANG CHỌN]" : "[MỞ KHÓA]";
                    descStr = "🎯 Mục tiêu: Tiêu diệt Siêu Boss Cell\n⚔ Độ khó: Cực Khó\n⚡ Tốc độ: Tối Đa";
                }
                else
                {
                    bgColor = new Color(0.22f, 0.24f, 0.28f, 0.70f);
                    title = "🔒 CẤP 3\nVŨ TRỤ";
                    statusStr = "[🔒 KHÓA]";
                    descStr = "Cần hoàn thành Cấp 2 để mở khóa!";
                }
            }

            cardBg.color = bgColor;
            cardBtn.interactable = isUnlocked;

            if (isUnlocked)
            {
                cardBtn.onClick.AddListener(() => {
                    GoToLevel(lvl);
                });
            }

            // Title
            GameObject tObj = new GameObject("Title");
            tObj.transform.SetParent(cardObj.transform, false);
            RectTransform tRt = tObj.AddComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 65f);
            tRt.sizeDelta = new Vector2(230f, 60f);
            Text tTxt = tObj.AddComponent<Text>();
            tTxt.font = font;
            tTxt.text = title;
            tTxt.fontSize = 18;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = isUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            tTxt.raycastTarget = false;

            // Status Badge
            GameObject sObj = new GameObject("Status");
            sObj.transform.SetParent(cardObj.transform, false);
            RectTransform sRt = sObj.AddComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, 15f);
            sRt.sizeDelta = new Vector2(230f, 25f);
            Text sTxt = sObj.AddComponent<Text>();
            sTxt.font = font;
            sTxt.text = statusStr;
            sTxt.fontSize = 14;
            sTxt.fontStyle = FontStyle.Bold;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = isUnlocked ? (isCurrent ? Color.yellow : Color.cyan) : new Color(1f, 0.4f, 0.4f);
            sTxt.raycastTarget = false;

            // Description
            GameObject dObj = new GameObject("Desc");
            dObj.transform.SetParent(cardObj.transform, false);
            RectTransform dRt = dObj.AddComponent<RectTransform>();
            dRt.anchoredPosition = new Vector2(0f, -45f);
            dRt.sizeDelta = new Vector2(220f, 80f);
            Text dTxt = dObj.AddComponent<Text>();
            dTxt.font = font;
            dTxt.text = descStr;
            dTxt.fontSize = 13;
            dTxt.alignment = TextAnchor.MiddleCenter;
            dTxt.color = isUnlocked ? new Color(0.9f, 0.95f, 1f) : new Color(0.6f, 0.6f, 0.6f);
            dTxt.raycastTarget = false;
        }
    }

    private GameObject CreateButtonObj(Transform parent, Vector2 pos, Vector2 size, Color color, System.Action onClick, string defaultLabel = "")
    {
        GameObject btnObj = new GameObject("Btn_" + defaultLabel);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = color;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = defaultLabel;
        txt.fontSize = 15;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;

        return btnObj;
    }

    public void ToggleSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSound();
        }
        UpdateAudioButtonLabels();
    }

    public void ToggleMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMusic();
        }
        UpdateAudioButtonLabels();
    }

    private void UpdateAudioButtonLabels()
    {
        bool soundMuted = AudioManager.Instance != null && AudioManager.Instance.IsSoundMuted;
        bool musicMuted = AudioManager.Instance != null && AudioManager.Instance.IsMusicMuted;

        if (soundBtnText != null)
        {
            soundBtnText.text = soundMuted ? "🔇 ÂM THANH: TẮT" : "🔊 ÂM THANH: BẬT";
        }
        if (soundButton != null)
        {
            soundButton.GetComponent<Image>().color = soundMuted ? new Color(0.35f, 0.35f, 0.4f) : new Color(0.15f, 0.45f, 0.85f);
        }

        if (musicBtnText != null)
        {
            musicBtnText.text = musicMuted ? "🔇 NHẠC NỀN: TẮT" : "🎵 NHẠC NỀN: BẬT";
        }
        if (musicButton != null)
        {
            musicButton.GetComponent<Image>().color = musicMuted ? new Color(0.35f, 0.35f, 0.4f) : new Color(0.20f, 0.55f, 0.65f);
        }
    }

    /// <summary>
    /// Mở thanh công cụ toàn màn hình và TẠM DỪNG GAME
    /// </summary>
    public void OpenLevelMenu()
    {
        if (fullscreenToolbarModal != null)
            fullscreenToolbarModal.SetActive(true);

        if (btnCollapsedMenu != null)
            btnCollapsedMenu.SetActive(false);

        // TẠM DỪNG GAME
        Time.timeScale = 0f;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RefreshLevelCards(defaultFont);
        UpdateAudioButtonLabels();
    }

    /// <summary>
    /// Đóng thanh công cụ, thu gọn vào góc trên bên phải màn hình và TIẾP TỤC GAME
    /// </summary>
    public void CloseLevelMenu()
    {
        if (fullscreenToolbarModal != null)
            fullscreenToolbarModal.SetActive(false);

        if (btnCollapsedMenu != null)
            btnCollapsedMenu.SetActive(true);

        // TIẾP TỤC GAME
        Time.timeScale = 1f;
    }

    public void UpdateHUDTexts()
    {
        if (questBannerText == null) return;

        if (currentLevel == 1)
        {
            questBannerText.text = $"[CẤP 1: TRÁI ĐẤT] 🎯 Diệt Vegeta: ({sessionKillCount}/3)";
            questBannerText.color = new Color(1f, 0.85f, 0.1f);
        }
        else if (currentLevel == 2)
        {
            Boss_PiccoloA pBoss = FindFirstObjectByType<Boss_PiccoloA>();
            if (pBoss != null)
            {
                questBannerText.text = $"[CẤP 2: NAMEK] 🎯 Siêu Boss Piccolo HP: ({pBoss.currentHp}/{pBoss.maxHp})";
            }
            else
            {
                questBannerText.text = $"[CẤP 2: NAMEK] 🎯 Diệt Siêu Boss Piccolo (AI Clone A)";
            }
            questBannerText.color = new Color(0.2f, 0.95f, 0.85f);
        }
        else
        {
            NPC_Cell cell = FindFirstObjectByType<NPC_Cell>();
            questBannerText.text = cell != null
                ? $"[CẤP 3: VŨ TRỤ] 🎯 Đánh bại Cell: ({cell.currentHp}/{cell.maxHp} HP)"
                : "[CẤP 3: VŨ TRỤ] 🎯 Đánh bại Cell";
            questBannerText.color = new Color(1f, 0.45f, 0.9f);
        }
    }

    public void UpdateCellHp(int hp)
    {
        if (currentLevel == 3 && questBannerText != null)
        {
            questBannerText.text = $"[CẤP 3: VŨ TRỤ] 🎯 Cell Boss HP: ({Mathf.Max(0, hp)}/20)";
        }
    }

    /// <summary>Áp dụng cài đặt tốc độ/spawn theo level hiện tại</summary>
    public void ApplyLevelSettings()
    {
        int idx = Mathf.Clamp(currentLevel - 1, 0, 2);

        // Tốc độ nền
        if (BackgroundManager.Instance != null)
            BackgroundManager.Instance.scrollSpeed = bgScrollSpeedPerLevel[idx];

        // Item spawn interval
        if (ItemSpawner.Instance != null)
            ItemSpawner.Instance.spawnInterval = itemSpawnIntervalPerLevel[idx];

        // Tốc độ Goku
        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (player != null)
            player.moveSpeed = playerSpeedPerLevel[idx];

        // Tốc độ Enemy B
        float bSpeed = enemySpeedPerLevel[idx];
        EnemyB[] bEnemies = FindObjectsByType<EnemyB>(FindObjectsSortMode.None);
        foreach (var b in bEnemies)
            if (b != null) b.moveSpeed = bSpeed;

        // Đổi màu background & camera theo level
        if (BackgroundManager.Instance != null)
            BackgroundManager.Instance.SetLevelTint(currentLevel);

        // Spawn NPC đặc biệt theo level
        SpawnLevelNPCs();

        // Restore player state nếu đang chuyển level
        if (currentLevel > 1)
            RestorePlayerState();

        sessionKillCount = 0;
        piccoloKillCount = 0;
        levelCompleted = false;

        UpdateHUDTexts();
    }

    /// <summary>Spawn NPC đặc biệt dựa trên level</summary>
    public void SpawnLevelNPCs()
    {
        // 1. Dọn sạch NPC cũ trước khi sinh mới
        NPC_Frieza[] friezas = FindObjectsByType<NPC_Frieza>(FindObjectsSortMode.None);
        foreach (var f in friezas) Destroy(f.gameObject);

        NPC_Cell[] cells = FindObjectsByType<NPC_Cell>(FindObjectsSortMode.None);
        foreach (var c in cells) Destroy(c.gameObject);

        NPC_Piccolo[] piccolos = FindObjectsByType<NPC_Piccolo>(FindObjectsSortMode.None);
        foreach (var p in piccolos) Destroy(p.gameObject);

        Boss_PiccoloA[] pBosses = FindObjectsByType<Boss_PiccoloA>(FindObjectsSortMode.None);
        foreach (var pb in pBosses) Destroy(pb.gameObject);

        GameObject lingeringCellBar = GameObject.Find("CellHPBar");
        if (lingeringCellBar != null) Destroy(lingeringCellBar);

        // 2. Dọn sạch Vegeta (EnemyB) ở Cấp 2 và Cấp 3 để không gây nhiễu và dồn ép người chơi
        if (currentLevel != 1)
        {
            EnemyB[] bEnemies = FindObjectsByType<EnemyB>(FindObjectsSortMode.None);
            foreach (var b in bEnemies)
            {
                if (b != null) Destroy(b.gameObject);
            }
        }

        // 3. Spawn theo level
        if (currentLevel == 1)
        {
            // Cấp 1: Vegeta (EnemyB)
            EnemyB eb = FindFirstObjectByType<EnemyB>();
            if (eb == null)
            {
                PlayerA pA = FindFirstObjectByType<PlayerA>();
                if (pA != null) pA.EnsureEnemyBCount();
            }
        }
        else if (currentLevel == 2)
        {
            // Cấp 2: Sinh Siêu Boss Piccolo (AI nhân bản Player A)
            piccoloKillCount = 0;
            SpawnPiccoloBoss();
        }
        else if (currentLevel == 3)
        {
            // Cấp 3: Cell là boss cuối, sau Vegeta ở cấp 1 và Piccolo ở cấp 2.
            piccoloKillCount = 0;
            SpawnCell();
        }
    }

    public void SpawnPiccoloBoss(int id = 1, int difficultyLevel = 2)
    {
        if (Camera.main == null) return;
        GameObject obj = new GameObject("Boss_PiccoloA");
        Boss_PiccoloA boss = obj.AddComponent<Boss_PiccoloA>();
        boss.ConfigureDifficulty(difficultyLevel);
        float groundY = BackgroundManager.GroundSurfaceY + 1.02f;
        float[] xPositions = { 0.72f, 0.86f, 1.00f };
        float x = Camera.main.ViewportToWorldPoint(new Vector3(xPositions[Mathf.Clamp(id - 1, 0, 2)], 0f, 10f)).x;
        obj.transform.position = new Vector3(x, groundY, 0f);
    }

    /// <summary>Được gọi khi một Siêu Boss Piccolo bị tiêu diệt ở Cấp 2 hoặc Cấp 3</summary>
    public void RegisterPiccoloBossKill()
    {
        if (levelCompleted) return;

        piccoloKillCount++;
        UpdateHUDTexts();

        int requiredKills = currentLevel == 2 ? 1 : killsToWinLevel3;
        if ((currentLevel == 2 || currentLevel == 3) && piccoloKillCount >= requiredKills)
        {
            levelCompleted = true;
            SavePlayerState();

            if (currentLevel == 2)
            {
                // Mở khóa Cấp 3!
                PlayerPrefs.SetInt(PREF_UNLOCKED, 3);
                PlayerPrefs.Save();
            }

            if (WinUI.Instance != null)
                WinUI.Instance.Show(currentLevel);
        }
    }

    public void SpawnPiccolo(int id)
    {
        if (Camera.main == null) return;
        GameObject obj = new GameObject("NPC_Piccolo_" + id);
        NPC_Piccolo piccolo = obj.AddComponent<NPC_Piccolo>();

        // Sinh ở phía bên phải màn hình (cách xa Goku) để Goku có đủ tầm nhìn và thời gian ra đòn Kamehameha
        float[] xPositions = { 0.85f, 0.95f, 1.05f };
        float[] yPositions = { 0.40f, 0.70f, 0.55f };
        int idx = Mathf.Clamp(id - 1, 0, 2);

        Vector3 pos = Camera.main.ViewportToWorldPoint(new Vector3(xPositions[idx], yPositions[idx], 10f));
        pos.z = 0f;
        obj.transform.position = pos;
    }

    public void SpawnFrieza(int id)
    {
        if (Camera.main == null) return;
        GameObject obj = new GameObject("NPC_Frieza_" + id);
        obj.AddComponent<NPC_Frieza>();
        float rx = Random.Range(0.60f, 0.90f);
        float ry = Random.Range(0.35f, 0.75f);
        Vector3 pos = Camera.main.ViewportToWorldPoint(new Vector3(rx, ry, 10f));
        pos.z = 0f;
        obj.transform.position = pos;
    }

    public void SpawnCell()
    {
        if (Camera.main == null) return;
        GameObject obj = new GameObject("NPC_Cell_Boss");
        obj.AddComponent<NPC_Cell>();
        Vector3 pos = Camera.main.ViewportToWorldPoint(new Vector3(0.82f, 0.6f, 10f));
        pos.z = 0f;
        obj.transform.position = pos;
    }

    /// <summary>Được gọi mỗi khi 1 kẻ địch B hoặc quái bị tiêu diệt</summary>
    public void RegisterKill(bool isCellBoss = false)
    {
        if (levelCompleted) return;

        if (!isCellBoss)
            sessionKillCount++;

        UpdateHUDTexts();
        CheckWinCondition(isCellBoss);
    }

    /// <summary>Được gọi khi 1 Piccolo bị tiêu diệt ở Cấp 2</summary>
    public void RegisterPiccoloKill()
    {
        if (levelCompleted) return;

        piccoloKillCount++;
        UpdateHUDTexts();

        if (currentLevel == 2 && piccoloKillCount >= 3)
        {
            levelCompleted = true;
            SavePlayerState();

            // Mở khóa Cấp 3!
            PlayerPrefs.SetInt(PREF_UNLOCKED, 3);
            PlayerPrefs.Save();

            if (WinUI.Instance != null)
                WinUI.Instance.Show(2);
        }
    }

    private void CheckWinCondition(bool cellDefeated = false)
    {
        if (levelCompleted) return;

        bool win = false;

        if (currentLevel == 1 && sessionKillCount >= killsToWinLevel1)
        {
            win = true;
            // Mở khóa Cấp 2!
            int curUnlocked = PlayerPrefs.GetInt(PREF_UNLOCKED, 1);
            if (curUnlocked < 2)
            {
                PlayerPrefs.SetInt(PREF_UNLOCKED, 2);
                PlayerPrefs.Save();
            }
        }
        else if (currentLevel == 2 && piccoloKillCount >= killsToWinLevel2)
        {
            win = true;
            // Mở khóa Cấp 3!
            PlayerPrefs.SetInt(PREF_UNLOCKED, 3);
            PlayerPrefs.Save();
        }
        else if (currentLevel == 3 && cellDefeated)
        {
            win = true;
        }

        if (win)
        {
            levelCompleted = true;
            SavePlayerState();
            if (WinUI.Instance != null)
                WinUI.Instance.Show(currentLevel);
        }
    }

    /// <summary>Chuyển trực tiếp sang Cấp Độ chỉ định (Tự động đóng menu & chạy game)</summary>
    public void GoToLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, 3);
        PlayerPrefs.SetInt(PREF_LEVEL, currentLevel);
        PlayerPrefs.Save();

        // Tự động đóng menu toàn màn hình và thu gọn vào góc phải khi đã chọn xong
        CloseLevelMenu();

        // Reset trạng thái sống/chết & UI
        levelCompleted = false;
        sessionKillCount = 0;
        piccoloKillCount = 0;
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ResetForNewLevel();

        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Hide();

        if (WinUI.Instance != null)
            WinUI.Instance.Hide();

        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (player != null)
        {
            // Luôn khởi đầu cấp độ mới với 100% Máu và Ki để tránh vòng lặp chết liên tục
            player.ResetPlayerState(player.maxHp, player.maxKi);
        }

        ApplyLevelSettings();
        ShowNotification($"🚀 ĐÃ VÀO CẤP ĐỘ {currentLevel}!");
    }

    /// <summary>Chuyển lên level tiếp theo khi bấm Next Level ở màn hình Win</summary>
    public void GoToNextLevel()
    {
        if (currentLevel < 3)
        {
            currentLevel++;
            PlayerPrefs.SetInt(PREF_LEVEL, currentLevel);
            PlayerPrefs.Save();
        }

        CloseLevelMenu();

        levelCompleted = false;
        sessionKillCount = 0;
        piccoloKillCount = 0;
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ResetForNewLevel();

        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Hide();

        if (WinUI.Instance != null)
            WinUI.Instance.Hide();

        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (player != null)
        {
            // Sang màn mới: Hồi đầy 100% Máu và Ki
            player.ResetPlayerState(player.maxHp, player.maxKi);
        }

        ApplyLevelSettings();
    }

    /// <summary>Chơi lại cấp hiện tại khi bấm Replay ở màn hình GameOver hoặc Win</summary>
    public void ReplayCurrentLevel()
    {
        CloseLevelMenu();

        PlayerPrefs.SetInt(PREF_LEVEL, currentLevel);
        PlayerPrefs.Save();

        levelCompleted = false;
        sessionKillCount = 0;
        piccoloKillCount = 0;
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ResetForNewLevel();

        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Hide();

        if (WinUI.Instance != null)
            WinUI.Instance.Hide();

        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (player != null)
        {
            // Chơi lại: Hồi đầy 100% Máu và Ki
            player.ResetPlayerState(player.maxHp, player.maxKi);
        }

        ApplyLevelSettings();
    }

    /// <summary>Về Level 1 (Home Screen)</summary>
    public void GoToLevel1()
    {
        CloseLevelMenu();

        currentLevel = 1;
        PlayerPrefs.SetInt(PREF_LEVEL, 1);
        PlayerPrefs.DeleteKey(PREF_HP);
        PlayerPrefs.DeleteKey(PREF_KI);
        PlayerPrefs.DeleteKey(PREF_SCORE);
        PlayerPrefs.Save();

        levelCompleted = false;
        sessionKillCount = 0;
        piccoloKillCount = 0;
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ResetGameOver();

        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Hide();

        if (WinUI.Instance != null)
            WinUI.Instance.Hide();

        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (player != null)
            player.ResetPlayerState(player.maxHp, player.maxKi);

        ApplyLevelSettings();
    }

    public void SavePlayerState()
    {
        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (player != null)
        {
            // Không bao giờ lưu trạng thái chết (HP <= 0) để tránh lỗi kẹt vòng lặp chết
            float hpToSave = (player.isDead || player.currentHp <= 0) ? player.maxHp : player.currentHp;
            PlayerPrefs.SetFloat(PREF_HP, hpToSave);
            PlayerPrefs.SetFloat(PREF_KI, Mathf.Max(50f, player.currentKi));
        }
        if (GameManager.Instance != null)
        {
            PlayerPrefs.SetInt(PREF_SCORE, GameManager.Instance.Score);
        }
        PlayerPrefs.SetInt(PREF_LEVEL, currentLevel);
        PlayerPrefs.Save();

        ShowNotification($"💾 ĐÃ LƯU TRẠNG THÁI CẤP {currentLevel}!");
    }

    public void RestorePlayerState()
    {
        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (player != null)
        {
            if (PlayerPrefs.HasKey(PREF_HP))
            {
                float savedHp = PlayerPrefs.GetFloat(PREF_HP);
                player.currentHp = Mathf.Clamp(savedHp, 80f, player.maxHp);
            }
            else
            {
                player.currentHp = player.maxHp;
            }

            if (PlayerPrefs.HasKey(PREF_KI))
            {
                float savedKi = PlayerPrefs.GetFloat(PREF_KI);
                player.currentKi = Mathf.Clamp(savedKi, 50f, player.maxKi);
            }
            else
            {
                player.currentKi = player.maxKi;
            }

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateHP(player.currentHp, player.maxHp);
                HUDManager.Instance.UpdateKi(player.currentKi, player.maxKi);
            }
        }

        if (GameManager.Instance != null && PlayerPrefs.HasKey(PREF_SCORE))
        {
            GameManager.Instance.Score = PlayerPrefs.GetInt(PREF_SCORE);
            if (HUDManager.Instance != null)
                HUDManager.Instance.UpdateScore(GameManager.Instance.Score);
        }
    }

    public void ShowNotification(string msg)
    {
        if (saveStatusText == null) return;
        saveStatusText.text = msg;
        CancelInvoke(nameof(ClearNotification));
        Invoke(nameof(ClearNotification), 2.5f);
    }

    private void ClearNotification()
    {
        if (saveStatusText != null)
            saveStatusText.text = "";
    }
}

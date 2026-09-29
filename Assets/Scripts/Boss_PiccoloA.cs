using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SIÊU BOSS PICCOLO (AI CLONE PLAYER A) - CẤP 2: NAMEK
/// Sở hữu đầy đủ bộ kỹ năng và cơ chế di chuyển chiến đấu như Player A:
/// 1. Di chuyển chạy bộ, nhảy cao né đòn (Dodge Jump)
/// 2. Gồng Ki (Ki Charge) với Aura xanh lá Namek (Small150..158)
/// 3. Ma Quán Quang Sát Pháo (Makankosappo / Special Beam Cannon) uy lực
/// 4. Bắn liên hoàn đạn Ki (Ki Blast Barrage - 3 viên)
/// 5. Lướt cận chiến thần tốc (Melee Dash Strike)
/// 6. Bật khiên năng lượng (Namek Energy Barrier) chắn đòn Kamehameha
/// Kích thước: ~235x235 px (characterScale = 0.90f)
/// Máu: 15 HP, Ki: 100
/// </summary>
[RequireComponent(typeof(CharacterParts))]
public class Boss_PiccoloA : MonoBehaviour
{
    public static Boss_PiccoloA Instance { get; private set; }

    [Header("Chỉ Số Siêu Boss Piccolo")]
    public int maxHp = 15;
    public int currentHp = 15;
    public float maxKi = 100f;
    public float currentKi = 60f;
    public float moveSpeed = 3.2f;
    public float jumpForce = 9.5f;

    [Header("Độ Khó Theo Cấp")]
    public int difficultyLevel = 2;
    public float damageMultiplier = 1f;
    private float cooldownMultiplier = 1f;

    [Header("Trạng Thái")]
    public bool isDead = false;
    public bool isShieldActive = false;
    public bool isChargingKi = false;
    public bool isPerformingSkill = false;
    public bool isJumping = false;

    // Sprite Animation & Renderers
    private CharacterParts parts;
    private SpriteRenderer headRenderer;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer legRenderer;

    private Sprite headIdle, headAction;
    private Sprite bodyIdle;
    private Sprite[] bodyRunFrames;
    private Sprite[] bodyChargeFrames;
    private Sprite[] bodyBeamFrames;
    private Sprite legIdle;
    private Sprite[] legRunFrames;
    private Sprite legJump, legFall;

    // Effects & Projections
    private Sprite[] auraFrames;
    private Sprite[] beamSpiralFrames;
    private Sprite[] attackEffectFrames;
    private Sprite[] explosionFrames;
    private SpriteRenderer auraRenderer;
    private GameObject shieldObj;

    // Overhead UI
    private GameObject overheadCanvas;
    private Slider hpSlider;
    private Slider kiSlider;
    private Text hpText;
    private Text kiText;
    private Text nameTag;

    // AI Variables
    private PlayerA player;
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private float aiDecisionTimer = 0.5f;
    private float skillCooldownTimer = 1.5f;
    private float groundY;
    private float verticalVelocity = 0f;
    private float gravity = 22f;
    private bool facingRight = false;
    private float runAnimTimer = 0f;
    private int runAnimIndex = 0;

    public void ConfigureDifficulty(int level)
    {
        difficultyLevel = level;

        if (level >= 3)
        {
            maxHp = 22;
            moveSpeed = 4.0f;
            jumpForce = 10.5f;
            damageMultiplier = 1.25f;
            cooldownMultiplier = 0.82f;
        }
        else
        {
            maxHp = 18;
            moveSpeed = 3.6f;
            jumpForce = 10.0f;
            damageMultiplier = 1.1f;
            cooldownMultiplier = 0.95f;
        }
    }

    private void Awake()
    {
        Instance = this;
        parts = GetComponent<CharacterParts>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.15f; // Khớp với kích thước ~235x235 px

        SetupCharacterRenderers();
        LoadAllPiccoloSprites();
        SetupAuraRenderer();
    }

    /// <summary>
    /// Tạo 3 GameObject con: Head, Body, Leg và gán vào CharacterParts
    /// Đảm bảo toàn bộ ngoại hình Piccolo hiển thị sắc nét, kích thước chuẩn 235x235
    /// </summary>
    private void SetupCharacterRenderers()
    {
        Transform hTrans = transform.Find("Head");
        if (hTrans == null)
        {
            GameObject h = new GameObject("Head");
            h.transform.SetParent(transform, false);
            hTrans = h.transform;
        }
        headRenderer = hTrans.GetComponent<SpriteRenderer>();
        if (headRenderer == null) headRenderer = hTrans.gameObject.AddComponent<SpriteRenderer>();
        headRenderer.sortingOrder = 25;

        Transform bTrans = transform.Find("Body");
        if (bTrans == null)
        {
            GameObject b = new GameObject("Body");
            b.transform.SetParent(transform, false);
            bTrans = b.transform;
        }
        bodyRenderer = bTrans.GetComponent<SpriteRenderer>();
        if (bodyRenderer == null) bodyRenderer = bTrans.gameObject.AddComponent<SpriteRenderer>();
        bodyRenderer.sortingOrder = 24;

        Transform lTrans = transform.Find("Leg");
        if (lTrans == null)
        {
            GameObject l = new GameObject("Leg");
            l.transform.SetParent(transform, false);
            lTrans = l.transform;
        }
        legRenderer = lTrans.GetComponent<SpriteRenderer>();
        if (legRenderer == null) legRenderer = lTrans.gameObject.AddComponent<SpriteRenderer>();
        legRenderer.sortingOrder = 23;

        if (parts != null)
        {
            parts.headRenderer = headRenderer;
            parts.bodyRenderer = bodyRenderer;
            parts.legRenderer = legRenderer;
            parts.characterScale = 2.05f; // Boss lớn hơn rõ rệt so với nhân vật thường
            parts.headHeight = 0.38f;     // Giữ đầu liền cổ nhưng không đè lên thân
            parts.legHeight = -0.32f;     // Giữ chân sát thân nhưng không chồng lên áo
            parts.headSize = 0.9f;        // Đầu cân đối với thân boss
            parts.SyncAndApplyAll();
        }
    }

    private void Start()
    {
        currentHp = maxHp;
        currentKi = 60f;
        isDead = false;

        groundY = BackgroundManager.GroundSurfaceY + 1.02f;
        Vector3 p = transform.position;
        p.y = groundY;
        transform.position = p;

        if (parts != null)
        {
            parts.characterScale = 2.05f;
            parts.headHeight = 0.38f;
            parts.legHeight = -0.32f;
            parts.headSize = 0.9f;
            parts.SetPose(headIdle, bodyIdle, legIdle);
            parts.Flip(false);
            facingRight = false;
        }

        player = FindFirstObjectByType<PlayerA>();
        BuildOverheadUI();
    }

    private Sprite LoadSpriteSafe(string name)
    {
        Sprite s = Resources.Load<Sprite>("Piccolo/" + name);
        if (s == null) s = Resources.Load<Sprite>(name);
#if UNITY_EDITOR
        if (s == null)
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Piccolo/{name}.png");
        if (s == null)
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Item/item1/ResAwkenV2/{name}.png");
        if (s == null)
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{name}.png");
#endif
        return s;
    }

    private void LoadAllPiccoloSprites()
    {
        // 1. Đầu
        headIdle   = LoadSpriteSafe("Small121");
        headAction = LoadSpriteSafe("Small122");

        // 2. Thân
        bodyIdle = LoadSpriteSafe("Small123");
        bodyRunFrames = new Sprite[]
        {
            LoadSpriteSafe("Small124"),
            LoadSpriteSafe("Small125"),
            LoadSpriteSafe("Small126"),
            LoadSpriteSafe("Small127"),
            LoadSpriteSafe("Small128")
        };
        bodyChargeFrames = new Sprite[]
        {
            LoadSpriteSafe("Small129"),
            LoadSpriteSafe("Small130"),
            LoadSpriteSafe("Small131")
        };
        bodyBeamFrames = new Sprite[]
        {
            LoadSpriteSafe("Small132"),
            LoadSpriteSafe("Small133"),
            LoadSpriteSafe("Small134"),
            LoadSpriteSafe("Small135")
        };

        // 3. Chân
        legIdle = LoadSpriteSafe("Small138");
        legRunFrames = new Sprite[]
        {
            LoadSpriteSafe("Small139"),
            LoadSpriteSafe("Small140"),
            LoadSpriteSafe("Small141"),
            LoadSpriteSafe("Small142"),
            LoadSpriteSafe("Small143"),
            LoadSpriteSafe("Small144"),
            LoadSpriteSafe("Small145"),
            LoadSpriteSafe("Small146")
        };
        legJump = LoadSpriteSafe("Small147");
        legFall = LoadSpriteSafe("Small149");

        // 4. Aura & Effects (Small150..180)
        List<Sprite> auras = new List<Sprite>();
        for (int i = 150; i <= 158; i++)
        {
            Sprite s = LoadSpriteSafe($"Small{i}");
            if (s != null) auras.Add(s);
        }
        auraFrames = auras.ToArray();

        List<Sprite> beams = new List<Sprite>();
        for (int i = 159; i <= 168; i++)
        {
            Sprite s = LoadSpriteSafe($"Small{i}");
            if (s != null) beams.Add(s);
        }
        beamSpiralFrames = beams.ToArray();

        List<Sprite> attackEffects = new List<Sprite>();
        for (int i = 363; i <= 365; i++)
        {
            Sprite s = LoadSpriteSafe($"Small{i}");
            if (s != null) attackEffects.Add(s);
        }
        attackEffectFrames = attackEffects.ToArray();

        List<Sprite> exps = new List<Sprite>();
        for (int i = 169; i <= 180; i++)
        {
            Sprite s = LoadSpriteSafe($"Small{i}");
            if (s != null) exps.Add(s);
        }
        explosionFrames = exps.ToArray();

        // Fallbacks nếu sprite nào thiếu
        if (headIdle == null) headIdle = Resources.Load<Sprite>("Small91");
        if (headAction == null) headAction = Resources.Load<Sprite>("Small92");
        if (bodyIdle == null) bodyIdle = Resources.Load<Sprite>("1");
        if (legIdle == null) legIdle = Resources.Load<Sprite>("22");
        if (legJump == null) legJump = Resources.Load<Sprite>("28");
        if (legFall == null) legFall = Resources.Load<Sprite>("29");

        if (parts != null)
        {
            parts.SetPose(headIdle, bodyIdle, legIdle);
        }
    }

    private void SetupAuraRenderer()
    {
        GameObject auraObj = new GameObject("PiccoloAura");
        auraObj.transform.SetParent(transform, false);
        auraObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        auraObj.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        auraRenderer = auraObj.AddComponent<SpriteRenderer>();
        auraRenderer.sortingOrder = 22; // Ngay sau lưng thân boss
        auraRenderer.color = new Color(0.3f, 1f, 0.4f, 0.85f); // Màu xanh Namek
        auraObj.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    // OVERHEAD UI (HP + KI BAR)
    // ─────────────────────────────────────────────────────────────
    private void BuildOverheadUI()
    {
        if (overheadCanvas != null) Destroy(overheadCanvas);

        overheadCanvas = new GameObject("OverheadUI");
        overheadCanvas.transform.SetParent(transform, false);
        overheadCanvas.transform.localPosition = new Vector3(0f, 1.10f, 0f);

        Canvas canvas = overheadCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rt = overheadCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 70f);
        rt.localScale = new Vector3(0.018f, 0.018f, 1f);

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Tên Boss
        GameObject nameObj = new GameObject("NameTag");
        nameObj.transform.SetParent(overheadCanvas.transform, false);
        RectTransform nRt = nameObj.AddComponent<RectTransform>();
        nRt.anchoredPosition = new Vector2(0f, 20f);
        nRt.sizeDelta = new Vector2(240f, 24f);
        nameTag = nameObj.AddComponent<Text>();
        nameTag.font = defaultFont;
        nameTag.text = "★ SIÊU BOSS: PICCOLO (AI NAMEK) ★";
        nameTag.fontSize = 13;
        nameTag.fontStyle = FontStyle.Bold;
        nameTag.alignment = TextAnchor.MiddleCenter;
        nameTag.color = new Color(0.25f, 1f, 0.5f);

        // 2. Thanh Máu (HP Bar)
        GameObject hpBg = new GameObject("HpBg");
        hpBg.transform.SetParent(overheadCanvas.transform, false);
        RectTransform hpBgRt = hpBg.AddComponent<RectTransform>();
        hpBgRt.anchoredPosition = new Vector2(0f, 2f);
        hpBgRt.sizeDelta = new Vector2(170f, 14f);
        Image hpBgImg = hpBg.AddComponent<Image>();
        hpBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        GameObject hpFill = new GameObject("HpFill");
        hpFill.transform.SetParent(hpBg.transform, false);
        RectTransform hpFillRt = hpFill.AddComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.sizeDelta = Vector2.zero;
        Image hpFillImg = hpFill.AddComponent<Image>();
        hpFillImg.color = new Color(0.95f, 0.2f, 0.2f, 1f);

        hpSlider = hpBg.AddComponent<Slider>();
        hpSlider.targetGraphic = hpFillImg;
        hpSlider.fillRect = hpFillRt;
        hpSlider.minValue = 0;
        hpSlider.maxValue = maxHp;
        hpSlider.value = currentHp;

        GameObject hpTextObj = new GameObject("HpText");
        hpTextObj.transform.SetParent(hpBg.transform, false);
        RectTransform hpTxtRt = hpTextObj.AddComponent<RectTransform>();
        hpTxtRt.anchorMin = Vector2.zero;
        hpTxtRt.anchorMax = Vector2.one;
        hpTxtRt.sizeDelta = Vector2.zero;
        hpText = hpTextObj.AddComponent<Text>();
        hpText.font = defaultFont;
        hpText.text = $"{currentHp} / {maxHp} HP";
        hpText.fontSize = 11;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;

        // 3. Thanh Ki (Ki Bar)
        GameObject kiBg = new GameObject("KiBg");
        kiBg.transform.SetParent(overheadCanvas.transform, false);
        RectTransform kiBgRt = kiBg.AddComponent<RectTransform>();
        kiBgRt.anchoredPosition = new Vector2(0f, -14f);
        kiBgRt.sizeDelta = new Vector2(170f, 10f);
        Image kiBgImg = kiBg.AddComponent<Image>();
        kiBgImg.color = new Color(0.05f, 0.1f, 0.2f, 0.9f);

        GameObject kiFill = new GameObject("KiFill");
        kiFill.transform.SetParent(kiBg.transform, false);
        RectTransform kiFillRt = kiFill.AddComponent<RectTransform>();
        kiFillRt.anchorMin = Vector2.zero;
        kiFillRt.anchorMax = Vector2.one;
        kiFillRt.sizeDelta = Vector2.zero;
        Image kiFillImg = kiFill.AddComponent<Image>();
        kiFillImg.color = new Color(0.2f, 0.6f, 1f, 1f);

        kiSlider = kiBg.AddComponent<Slider>();
        kiSlider.targetGraphic = kiFillImg;
        kiSlider.fillRect = kiFillRt;
        kiSlider.minValue = 0;
        kiSlider.maxValue = maxKi;
        kiSlider.value = currentKi;

        GameObject kiTextObj = new GameObject("KiText");
        kiTextObj.transform.SetParent(kiBg.transform, false);
        RectTransform kiTxtRt = kiTextObj.AddComponent<RectTransform>();
        kiTxtRt.anchorMin = Vector2.zero;
        kiTxtRt.anchorMax = Vector2.one;
        kiTxtRt.sizeDelta = Vector2.zero;
        kiText = kiTextObj.AddComponent<Text>();
        kiText.font = defaultFont;
        kiText.text = $"KI: {(int)currentKi}%";
        kiText.fontSize = 9;
        kiText.fontStyle = FontStyle.Bold;
        kiText.alignment = TextAnchor.MiddleCenter;
        kiText.color = Color.white;
    }

    private void UpdateUI()
    {
        if (hpSlider != null) hpSlider.value = currentHp;
        if (hpText != null) hpText.text = $"{currentHp} / {maxHp} HP";
        if (kiSlider != null) kiSlider.value = currentKi;
        if (kiText != null) kiText.text = $"KI: {(int)currentKi}%";
    }

    private void LateUpdate()
    {
        // Triệt tiêu việc lật chữ: khi Piccolo quay trái hoặc phải, chữ overhead vẫn luôn đọc xuôi từ trái sang phải!
        if (overheadCanvas != null)
        {
            float sign = transform.localScale.x < 0 ? -1f : 1f;
            overheadCanvas.transform.localScale = new Vector3(sign * 0.010f, 0.010f, 1f);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // UPDATE & AI CONTROLLER
    // ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (isDead) return;

        if (player == null)
            player = FindFirstObjectByType<PlayerA>();

        HandlePhysics();
        DetectIncomingAttacks();

        if (isPerformingSkill || isChargingKi)
            return;

        aiDecisionTimer -= Time.deltaTime;
        skillCooldownTimer -= Time.deltaTime;

        if (aiDecisionTimer <= 0f)
        {
            aiDecisionTimer = 0.25f;
            DecideNextAction();
        }

        UpdateLocomotion();
        UpdateUI();
    }

    private void HandlePhysics()
    {
        Vector3 pos = transform.position;

        if (pos.y > groundY)
        {
            verticalVelocity -= gravity * Time.deltaTime;
            pos.y += verticalVelocity * Time.deltaTime;

            if (pos.y <= groundY)
            {
                pos.y = groundY;
                verticalVelocity = 0f;
                isJumping = false;
            }
        }
        else
        {
            pos.y = groundY;
            if (!isJumping)
                verticalVelocity = 0f;
        }

        transform.position = pos;
    }

    private void UpdateLocomotion()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        float dirX = player.transform.position.x - transform.position.x;

        // Xoay mặt về phía người chơi
        if (dirX > 0.3f && !facingRight)
        {
            facingRight = true;
            parts.Flip(true);
        }
        else if (dirX < -0.3f && facingRight)
        {
            facingRight = false;
            parts.Flip(false);
        }

        // Nếu ở xa (> 4m), di chuyển lại gần
        if (dist > 4.5f)
        {
            float step = (facingRight ? 1f : -1f) * moveSpeed * Time.deltaTime;
            transform.position += new Vector3(step, 0f, 0f);

            // Animate run
            runAnimTimer += Time.deltaTime;
            if (runAnimTimer >= 0.12f)
            {
                runAnimTimer = 0f;
                runAnimIndex = (runAnimIndex + 1) % bodyRunFrames.Length;
                Sprite b = bodyRunFrames[runAnimIndex] != null ? bodyRunFrames[runAnimIndex] : bodyIdle;
                Sprite l = (legRunFrames != null && legRunFrames.Length > 0) ? legRunFrames[runAnimIndex % legRunFrames.Length] : legIdle;
                parts.SetPose(headIdle, b, l);
            }
        }
        else if (dist < 2.5f)
        {
            // Quá gần thì lùi lại nhẹ để giữ khoảng cách ra đòn
            float step = (facingRight ? -1f : 1f) * (moveSpeed * 0.7f) * Time.deltaTime;
            transform.position += new Vector3(step, 0f, 0f);
            parts.SetPose(headIdle, bodyIdle, legIdle);
        }
        else
        {
            if (!isJumping)
                parts.SetPose(headIdle, bodyIdle, legIdle);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // AI TỔ HỢP CHIẾN THUẬT (HƠI KHÓ 1 CHÚT)
    // ─────────────────────────────────────────────────────────────
    private void DecideNextAction()
    {
        if (player == null || skillCooldownTimer > 0f) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);

        // 1. Nếu Ki thấp (< 35), ưu tiên lùi lại gồng Ki tích năng lượng
        if (currentKi < 35f)
        {
            StartCoroutine(ChargeKiRoutine(1.8f));
            return;
        }

        // 2. Nếu khoảng cách rất gần (< 3.2m), tung đòn Melee Dash đấm bay Goku
        if (dist <= 3.2f && currentKi >= 15f && Random.value < 0.65f)
        {
            StartCoroutine(MeleeStrikeRoutine());
            return;
        }

        // 3. Nếu khoảng cách trung bình/xa (3.5m - 9m):
        // 45% bắn Ma Quán Quang Sát Pháo (Makankosappo), 55% bắn loạt đạn Ki liên hoàn
        if (dist > 3.0f)
        {
            if (currentKi >= 40f && Random.value < 0.45f)
            {
                StartCoroutine(MakankosappoBeamRoutine());
            }
            else if (currentKi >= 20f)
            {
                StartCoroutine(KiBlastBarrageRoutine());
            }
        }
    }

    /// <summary>Phát hiện đòn Kamehameha hoặc đạn đang bay tới để né/khiên</summary>
    private void DetectIncomingAttacks()
    {
        if (isShieldActive || isDead) return;

        KamehamehaC kame = FindFirstObjectByType<KamehamehaC>();
        if (kame != null)
        {
            float d = Vector3.Distance(transform.position, kame.transform.position);
            if (d < 3.8f && skillCooldownTimer <= 0.5f)
            {
                // 50% cơ hội bật khiên Namek, 50% nhảy cao né đòn
                if (Random.value < 0.5f && currentKi >= 25f)
                {
                    StartCoroutine(ShieldRoutine());
                }
                else if (!isJumping)
                {
                    PerformDodgeJump();
                }
            }
        }
    }

    private void PerformDodgeJump()
    {
        isJumping = true;
        verticalVelocity = jumpForce;
        parts.SetPose(headAction, bodyIdle, legJump);
    }

    // ─────────────────────────────────────────────────────────────
    // KỸ NĂNG 1: GỒNG KI (CHARGE KI) + AURA NAMEK
    // ─────────────────────────────────────────────────────────────
    private IEnumerator ChargeKiRoutine(float duration)
    {
        isChargingKi = true;
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(true);

        parts.SetPose(headAction, bodyChargeFrames != null && bodyChargeFrames.Length > 0 ? bodyChargeFrames[0] : bodyIdle, legIdle);

        float elapsed = 0f;
        int frameIdx = 0;
        float frameTimer = 0f;

        while (elapsed < duration && !isDead)
        {
            elapsed += Time.deltaTime;
            currentKi = Mathf.Min(maxKi, currentKi + 35f * Time.deltaTime);

            frameTimer += Time.deltaTime;
            if (frameTimer >= 0.08f)
            {
                frameTimer = 0f;
                frameIdx++;
                if (auraFrames != null && auraFrames.Length > 0 && auraRenderer != null)
                {
                    auraRenderer.sprite = auraFrames[frameIdx % auraFrames.Length];
                }
            }
            yield return null;
        }

        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
        isChargingKi = false;
        skillCooldownTimer = 1.0f * cooldownMultiplier;
    }

    // ─────────────────────────────────────────────────────────────
    // KỸ NĂNG 2: MA QUÁN QUANG SÁT PHÁO (MAKANKOSAPPO BEAM)
    // ─────────────────────────────────────────────────────────────
    private IEnumerator MakankosappoBeamRoutine()
    {
        isPerformingSkill = true;
        currentKi -= 35f;

        // 1. Niệm chiêu: đưa 2 ngón tay lên trán tụ khí (0.6s)
        Sprite prepBody = bodyBeamFrames != null && bodyBeamFrames.Length > 0 ? bodyBeamFrames[0] : bodyIdle;
        parts.SetPose(headAction, prepBody, legIdle);

        // Tạo quả cầu năng lượng xoắn ốc nhỏ trên đầu/ngón tay
        GameObject spark = new GameObject("BeamSpark");
        spark.transform.SetParent(transform, false);
        spark.transform.localPosition = new Vector3(facingRight ? 0.4f : -0.4f, 1.4f, 0f);
        SpriteRenderer sparkSr = spark.AddComponent<SpriteRenderer>();
        sparkSr.color = new Color(0.85f, 0.2f, 1f, 1f); // Tím/Vàng Ma Giới
        sparkSr.sortingOrder = 26;

        float chargeTime = 0.6f;
        float t = 0f;
        int sparkIdx = 0;
        while (t < chargeTime && !isDead)
        {
            t += Time.deltaTime;
            spark.transform.Rotate(0f, 0f, 720f * Time.deltaTime);
            if (attackEffectFrames != null && attackEffectFrames.Length > 0)
            {
                sparkSr.sprite = attackEffectFrames[sparkIdx % attackEffectFrames.Length];
                sparkIdx++;
            }
            yield return null;
        }
        Destroy(spark);

        if (isDead) yield break;

        // 2. Phóng chùm tia xoắn ốc Makankosappo xuyên thấu
        Sprite fireBody = bodyBeamFrames != null && bodyBeamFrames.Length > 2 ? bodyBeamFrames[2] : bodyIdle;
        parts.SetPose(headAction, fireBody, legIdle);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();

        SpawnMakankosappoProjectile();

        yield return new WaitForSeconds(0.4f);
        isPerformingSkill = false;
        skillCooldownTimer = 1.8f * cooldownMultiplier;
    }

    private void SpawnMakankosappoProjectile()
    {
        GameObject beamObj = new GameObject("Piccolo_Makankosappo");
        Vector3 spawnPos = transform.position + new Vector3(facingRight ? 1.0f : -1.0f, 0.4f, 0f);
        beamObj.transform.position = spawnPos;
        beamObj.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

        SpriteRenderer sr = beamObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 27;
        sr.color = new Color(0.9f, 0.25f, 1f, 1f); // Màu tím đặc trưng Makankosappo

        if (attackEffectFrames != null && attackEffectFrames.Length > 0)
            sr.sprite = attackEffectFrames[0];
        else if (beamSpiralFrames != null && beamSpiralFrames.Length > 0)
            sr.sprite = beamSpiralFrames[0];

        StartCoroutine(MakankosappoFlight(beamObj, facingRight ? 1f : -1f));
    }

    private IEnumerator MakankosappoFlight(GameObject beamObj, float dir)
    {
        float speed = 14f;
        float life = 2.5f;
        float elapsed = 0f;
        int frame = 0;
        SpriteRenderer sr = beamObj.GetComponent<SpriteRenderer>();

        while (elapsed < life && beamObj != null)
        {
            beamObj.transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
            elapsed += Time.deltaTime;

            if (sr != null && attackEffectFrames != null && attackEffectFrames.Length > 0)
            {
                frame++;
                sr.sprite = attackEffectFrames[frame % attackEffectFrames.Length];
            }

            // Kiểm tra trúng Player A
            if (player != null && !player.isDead)
            {
                float dist = Vector3.Distance(beamObj.transform.position, player.transform.position);
                if (dist < 1.4f)
                {
                    player.TakeDamage(25f * damageMultiplier);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    Destroy(beamObj);
                    yield break;
                }
            }
            yield return null;
        }

        if (beamObj != null) Destroy(beamObj);
    }

    // ─────────────────────────────────────────────────────────────
    // KỸ NĂNG 3: LIÊN HOÀN ĐẠN KI (KI BLAST BARRAGE - 3 SHOTS)
    // ─────────────────────────────────────────────────────────────
    private IEnumerator KiBlastBarrageRoutine()
    {
        isPerformingSkill = true;
        currentKi -= 20f;

        for (int i = 0; i < 3; i++)
        {
            if (isDead) yield break;

            parts.SetPose(headAction, bodyBeamFrames != null && bodyBeamFrames.Length > 1 ? bodyBeamFrames[1] : bodyIdle, legIdle);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();

            SpawnSingleKiBlast();
            yield return new WaitForSeconds(0.22f);
        }

        isPerformingSkill = false;
        skillCooldownTimer = 1.4f * cooldownMultiplier;
    }

    private void SpawnSingleKiBlast()
    {
        GameObject ki = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ki.name = "Piccolo_KiBlast";
        ki.transform.position = transform.position + new Vector3(facingRight ? 0.9f : -0.9f, 0.4f, 0f);
        ki.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        Collider c = ki.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Renderer r = ki.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.shader = Shader.Find("Sprites/Default");
            r.material.color = new Color(0.2f, 1f, 0.6f); // Xanh lục ngọc Namek
        }

        Vector3 targetPos = player != null ? player.transform.position : transform.position + new Vector3(facingRight ? 10f : -10f, 0f, 0f);
        StartCoroutine(KiBlastHomingFlight(ki, targetPos));
    }

    private IEnumerator KiBlastHomingFlight(GameObject ki, Vector3 targetPos)
    {
        float speed = 11f;
        float elapsed = 0f;

        while (elapsed < 2.0f && ki != null)
        {
            elapsed += Time.deltaTime;
            ki.transform.position = Vector3.MoveTowards(ki.transform.position, targetPos, speed * Time.deltaTime);

            if (player != null && !player.isDead)
            {
                if (Vector3.Distance(ki.transform.position, player.transform.position) < 1.1f)
                {
                    player.TakeDamage(10f * damageMultiplier);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    Destroy(ki);
                    yield break;
                }
            }

            if (Vector3.Distance(ki.transform.position, targetPos) < 0.2f)
            {
                Destroy(ki);
                yield break;
            }
            yield return null;
        }

        if (ki != null) Destroy(ki);
    }

    // ─────────────────────────────────────────────────────────────
    // KỸ NĂNG 4: LƯỚT CẬN CHIẾN THẦN TỐC (MELEE STRIKE)
    // ─────────────────────────────────────────────────────────────
    private IEnumerator MeleeStrikeRoutine()
    {
        isPerformingSkill = true;
        currentKi -= 15f;

        Vector3 startPos = transform.position;
        float dashDist = facingRight ? 3.8f : -3.8f;
        Vector3 dashTarget = startPos + new Vector3(dashDist, 0f, 0f);

        parts.SetPose(headAction, bodyBeamFrames != null && bodyBeamFrames.Length > 2 ? bodyBeamFrames[2] : bodyIdle, legJump);
        if (bodyRenderer != null) bodyRenderer.color = new Color(1f, 0.3f, 0.3f);

        float dashTime = 0.16f;
        float t = 0f;
        bool dealtDamage = false;

        while (t < dashTime && !isDead)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, dashTarget, t / dashTime);

            if (!dealtDamage && player != null && Vector3.Distance(transform.position, player.transform.position) < 2.0f)
            {
                dealtDamage = true;
                player.TakeDamage(16f * damageMultiplier);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
            }
            yield return null;
        }

        if (bodyRenderer != null) bodyRenderer.color = Color.white;
        isPerformingSkill = false;
        skillCooldownTimer = 1.6f * cooldownMultiplier;
    }

    // ─────────────────────────────────────────────────────────────
    // KỸ NĂNG 5: KHIÊN NĂNG LƯỢNG NAMEK (BARRIER SHIELD)
    // ─────────────────────────────────────────────────────────────
    private IEnumerator ShieldRoutine()
    {
        isShieldActive = true;
        currentKi -= 25f;

        if (shieldObj == null)
        {
            shieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldObj.name = "Piccolo_Shield";
            shieldObj.transform.SetParent(transform, false);
            shieldObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            shieldObj.transform.localScale = new Vector3(2.4f, 2.4f, 1f);

            Collider c = shieldObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer r = shieldObj.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.shader = Shader.Find("Sprites/Default");
                r.material.color = new Color(0.2f, 0.9f, 0.4f, 0.55f);
            }
        }

        shieldObj.SetActive(true);
        yield return new WaitForSeconds(2.0f);

        if (shieldObj != null) shieldObj.SetActive(false);
        isShieldActive = false;
        skillCooldownTimer = 2.2f;
    }

    // ─────────────────────────────────────────────────────────────
    // NHẬN SÁT THƯƠNG & CHẾT
    // ─────────────────────────────────────────────────────────────
    public void TakeHit(int damage = 1)
    {
        if (isDead) return;

        // Nếu khiên năng lượng đang bật, hấp thụ hoàn toàn sát thương!
        if (isShieldActive)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
            return;
        }

        currentHp -= damage;
        UpdateUI();

        // Flash màu đỏ khi trúng đạn
        StartCoroutine(HitFlashRoutine());

        if (currentHp <= 0)
        {
            currentHp = 0;
            StartCoroutine(DieRoutine());
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            bodyRenderer.color = Color.white;
        }
    }

    public IEnumerator StunRoutine(float duration)
    {
        if (isDead) yield break;
        bool wasPerforming = isPerformingSkill;
        isPerformingSkill = true;
        skillCooldownTimer = duration;
        if (bodyRenderer != null) bodyRenderer.color = Color.yellow;
        yield return new WaitForSeconds(duration);
        if (bodyRenderer != null) bodyRenderer.color = Color.white;
        isPerformingSkill = false;
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        if (overheadCanvas != null) Destroy(overheadCanvas);
        if (shieldObj != null) Destroy(shieldObj);
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();

        // Báo về LevelManager: Đánh bại Siêu Boss Piccolo -> Win Cấp 2!
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RegisterPiccoloBossKill();
        }

        // Hiệu ứng nổ tung với các sprite năng lượng Namek Small169..180
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 1.0f)
        {
            t += Time.deltaTime * 1.6f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.4f;
            transform.localScale = startScale * s;
            if (bodyRenderer != null) bodyRenderer.color = Color.Lerp(Color.white, Color.clear, t);
            if (headRenderer != null) headRenderer.color = Color.Lerp(Color.white, Color.clear, t);
            if (legRenderer != null)  legRenderer.color  = Color.Lerp(Color.white, Color.clear, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || isDead) return;
        if (other.GetComponent<KamehamehaC>() != null || other.gameObject.name.Contains("Projectile") || other.gameObject.name.Contains("Kamehameha"))
        {
            TakeHit(3);
        }
    }
}

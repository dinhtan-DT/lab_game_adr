using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC Thông Minh #3: Cell Boss (Level 3)
/// AI State Machine: Idle → Jump → Chase → PowerUp
/// - Idle:    Đứng trên bệ nổi
/// - Jump:    Nhảy sang bệ gần A nhất khi A cách xa > 3 unit
/// - Chase:   Lao thẳng vào A khi A cách < 3 unit, gây 25 damage
/// - PowerUp: Khi HP < 15 → tốc độ +50%, màu đỏ, damage +50%
/// HP = 30 (cần 30 chưởng C để hạ hoàn toàn)
/// Khi HP = 0 → Win!
/// </summary>
public class NPC_Cell : MonoBehaviour
{
    // ---- Stats Cân Bằng (Cấp 3 Hấp Dẫn & Vừa Sức) ----
    public int   maxHp        = 20; // 20 HP (vừa đủ kịch tính, không quá dài dòng gây ức chế)
    public int   currentHp;
    public float moveSpeed    = 2.8f; // Giảm tốc độ để Cell di chuyển uy lực, không phi vèo vèo
    public float attackDamage = 14f;  // Sát thương 14 (thay vì 25 quá thốn)
    public float jumpSpeed    = 5.0f; // Nhảy mượt mà, không quá nhanh
    public float chaseRange   = 3.5f;

    // ---- State ----
    private enum State { Idle, Jump, Chase, PowerUp, Dead }
    private State state = State.Idle;

    // ---- Renderer ----
    private SpriteRenderer sr;
    private static readonly Color CELL_NORMAL   = new Color(0.20f, 0.80f, 0.25f, 1f); // xanh lá
    private static readonly Color CELL_POWERUP  = new Color(0.90f, 0.15f, 0.10f, 1f); // đỏ

    // ---- Jump target ----
    private Vector3 jumpTarget;

    // ---- HP Bar UI ----
    private Slider hpSlider;
    private Text   hpLabel;
    private GameObject hpBarObj;

    // ---- Cooldown ----
    private float attackCooldown = 3.5f; // Chờ 3.5s an toàn đầu màn
    private const float ATTACK_CD = 2.5f; // Thời gian hồi đòn 2.5s

    // ---- Bounds ----
    private float leftBound, rightBound, topBound, bottomBound;

    private bool isPoweredUp = false;
    private bool isDead      = false;

    private void Awake()
    {
        currentHp = maxHp;

        // Renderer
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        // Nạp sprite cell.png (Resources)
        Sprite cellSprite = Resources.Load<Sprite>("cell");
        if (cellSprite != null)
        {
            sr.sprite = cellSprite;
            sr.color  = Color.white;
        }
        else
        {
            sr.sprite = Resources.Load<Sprite>("Small92");
            sr.color  = CELL_NORMAL;
        }
#if UNITY_EDITOR
        if (sr.sprite == null)
        {
            sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Small92.png");
            sr.color  = CELL_NORMAL;
        }
#endif

        sr.sortingOrder = 15; // Luôn hiển thị nổi bật ở tiền cảnh, không bao giờ bị che khuất
        transform.localScale = new Vector3(2.5f, 2.5f, 1f); // Kích thước ~235x235 px (to lớn, cực kỳ bắt mắt)

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f; // Bán kính 0.5f ở scale 2.5 = 1.25m vừa khít thân hình Cell
    }

    private void Start()
    {
        CalculateBounds();
        BuildHPBar();
        state = State.Idle;
        CreateOverheadNameTag();
    }

    private void CreateOverheadNameTag()
    {
        Transform existing = transform.Find("OverheadNameTag");
        if (existing != null) return;

        GameObject tagObj = new GameObject("OverheadNameTag");
        tagObj.transform.SetParent(transform, false);
        tagObj.transform.localPosition = new Vector3(0, 0.7f, 0);

        TextMesh tm = tagObj.AddComponent<TextMesh>();
        tm.text = "☠ FINAL BOSS: CELL (HOÀN HẢO) ☠";
        tm.fontSize = 26;
        tm.characterSize = 0.025f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.3f, 1f, 0.4f, 1f); // Xanh lá độc

        MeshRenderer mr = tagObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 25;
    }

    // ────────────────────────────────────────────
    // HP BAR
    // ────────────────────────────────────────────
    private void BuildHPBar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Container
        hpBarObj = new GameObject("CellHPBar");
        hpBarObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = hpBarObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.25f, 0.92f);
        rt.anchorMax = new Vector2(0.75f, 0.99f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        // Background
        Image bg = hpBarObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.05f, 0.05f, 0.85f);

        // Label "CELL"
        GameObject lblObj = new GameObject("CellLabel");
        lblObj.transform.SetParent(hpBarObj.transform, false);
        RectTransform lblRt = lblObj.AddComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0f);
        lblRt.anchorMax = new Vector2(0.25f, 1f);
        lblRt.sizeDelta = Vector2.zero;
        Text lbl = lblObj.AddComponent<Text>();
        lbl.text = "☠ CELL";
        lbl.fontSize = 20;
        lbl.fontStyle = FontStyle.Bold;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = CELL_NORMAL;
        lbl.font  = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Slider (HP bar)
        GameObject sliderObj = new GameObject("HPSlider");
        sliderObj.transform.SetParent(hpBarObj.transform, false);
        RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.26f, 0.1f);
        sliderRt.anchorMax = new Vector2(0.99f, 0.9f);
        sliderRt.sizeDelta = Vector2.zero;

        hpSlider = sliderObj.AddComponent<Slider>();
        hpSlider.minValue = 0;
        hpSlider.maxValue = maxHp;
        hpSlider.value    = maxHp;
        hpSlider.interactable = false;

        // Background của slider
        GameObject sbg = new GameObject("Background");
        sbg.transform.SetParent(sliderObj.transform, false);
        RectTransform sbgRt = sbg.AddComponent<RectTransform>();
        sbgRt.anchorMin = Vector2.zero;
        sbgRt.anchorMax = Vector2.one;
        sbgRt.sizeDelta = Vector2.zero;
        sbg.AddComponent<Image>().color = new Color(0.3f, 0.1f, 0.1f, 0.9f);

        // Fill Area
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.sizeDelta = new Vector2(-5f, -5f);
        faRt.anchoredPosition = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = CELL_NORMAL;

        hpSlider.fillRect = fillRt;

        // HP text
        GameObject hpTxtObj = new GameObject("HPText");
        hpTxtObj.transform.SetParent(sliderObj.transform, false);
        RectTransform hpTxtRt = hpTxtObj.AddComponent<RectTransform>();
        hpTxtRt.anchorMin = Vector2.zero;
        hpTxtRt.anchorMax = Vector2.one;
        hpTxtRt.sizeDelta = Vector2.zero;
        hpLabel = hpTxtObj.AddComponent<Text>();
        hpLabel.text = $"{maxHp}/{maxHp}";
        hpLabel.fontSize = 16;
        hpLabel.fontStyle = FontStyle.Bold;
        hpLabel.alignment = TextAnchor.MiddleCenter;
        hpLabel.color = Color.white;
        hpLabel.font  = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpLabel.raycastTarget = false;
    }

    private void UpdateHPBar()
    {
        if (hpSlider != null) hpSlider.value = currentHp;
        if (hpLabel  != null) hpLabel.text   = $"{currentHp}/{maxHp}";
    }

    // ────────────────────────────────────────────
    // UPDATE
    // ────────────────────────────────────────────
    private void Update()
    {
        if (isDead) return;

        CalculateBounds();
        attackCooldown -= Time.deltaTime;

        PlayerA player = FindFirstObjectByType<PlayerA>();

        switch (state)
        {
            case State.Idle:    HandleIdle(player);    break;
            case State.Jump:    HandleJump(player);    break;
            case State.Chase:   HandleChase(player);   break;
            case State.PowerUp: HandleChase(player);   break; // PowerUp vẫn chase nhưng nhanh hơn
        }
    }

    private const float BOSS_SCALE = 2.5f; // Đảm bảo kích thước chuẩn ~235x235 px

    private void HandleIdle(PlayerA player)
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.transform.position);

        // Cell luôn luôn chủ động tìm kiếm và áp sát Goku, không bao giờ đứng im mất tích
        Vector3 dir = (player.transform.position - transform.position).normalized;
        transform.position += dir * (moveSpeed * 0.75f) * Time.deltaTime;

        if (dir.x < 0) transform.localScale = new Vector3(-BOSS_SCALE, BOSS_SCALE, 1f);
        else           transform.localScale = new Vector3( BOSS_SCALE, BOSS_SCALE, 1f);

        if (dist <= chaseRange * 1.5f)
        {
            state = State.Chase;
        }
    }

    private void HandleJump(PlayerA player)
    {
        // Nhảy mượt đến jumpTarget
        transform.position = Vector3.MoveTowards(transform.position, jumpTarget, jumpSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, jumpTarget) < 0.15f)
        {
            state = State.Idle;
        }

        // Nếu A vào gần thì chase ngay
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= chaseRange) state = State.Chase;
        }
    }

    private void HandleChase(PlayerA player)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            state = State.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.transform.position);

        // Bay đến A với tốc độ hợp lý
        float speed = isPoweredUp ? moveSpeed * 1.25f : moveSpeed;
        Vector3 dir = (player.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // Flip chuẩn theo BOSS_SCALE = 2.5f (kích thước ~235x235 px)
        if (dir.x < 0) transform.localScale = new Vector3(-BOSS_SCALE, BOSS_SCALE, 1f);
        else           transform.localScale = new Vector3( BOSS_SCALE, BOSS_SCALE, 1f);

        // Tấn công nếu đủ gần + hết cooldown
        if (dist <= 2.2f && attackCooldown <= 0f)
        {
            float dmg = isPoweredUp ? attackDamage * 1.25f : attackDamage;
            player.TakeDamage(dmg);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
            attackCooldown = ATTACK_CD;
        }
    }

    // ────────────────────────────────────────────
    // NHẬN SÁT THƯƠNG TỪ KAMEHAMEHA C
    // ────────────────────────────────────────────
    public void TakeHit(int damage = 1)
    {
        if (isDead) return;

        currentHp = Mathf.Max(0, currentHp - damage);
        UpdateHPBar();

        if (LevelManager.Instance != null)
            LevelManager.Instance.UpdateCellHp(currentHp);

        // Nhấp nháy trắng
        StartCoroutine(HitFlash());

        // PowerUp khi HP < 50%
        if (!isPoweredUp && currentHp < maxHp / 2)
        {
            TriggerPowerUp();
        }

        if (currentHp <= 0)
            StartCoroutine(DieRoutine());
    }

    private IEnumerator HitFlash()
    {
        if (sr != null) sr.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        if (!isDead)
            sr.color = isPoweredUp ? CELL_POWERUP : CELL_NORMAL;
    }

    private void TriggerPowerUp()
    {
        isPoweredUp = true;
        state = State.PowerUp;
        if (sr != null) sr.color = CELL_POWERUP;
        // Cập nhật màu HP bar
        if (hpSlider != null)
        {
            Image fill = hpSlider.fillRect?.GetComponent<Image>();
            if (fill != null) fill.color = CELL_POWERUP;
        }

        TextMesh tm = GetComponentInChildren<TextMesh>();
        if (tm != null)
        {
            tm.text = "🔥 NPC 3: CELL [HÓA ĐIÊN!] 🔥";
            tm.color = Color.red;
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayWarningBeeps();
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();

        // Ẩn HP bar
        if (hpBarObj != null) hpBarObj.SetActive(false);

        // Báo win Level 3
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegisterKill(isCellBoss: true);

        // Hiệu ứng chết: phóng to rồi tan biến
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
            transform.localScale = startScale * s;
            if (sr != null) sr.color = Color.Lerp(CELL_POWERUP, Color.clear, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    // ────────────────────────────────────────────
    // HELPERS
    // ────────────────────────────────────────────
    private FloatingObstacle FindNearestPlatformToPlayer(PlayerA player)
    {
        if (FloatingObstacle.ActiveObstacles == null || FloatingObstacle.ActiveObstacles.Count == 0)
            return null;

        FloatingObstacle nearest = null;
        float minDist = float.MaxValue;
        foreach (var obs in FloatingObstacle.ActiveObstacles)
        {
            if (obs == null || !obs.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(obs.transform.position, player.transform.position);
            if (d < minDist) { minDist = d; nearest = obs; }
        }
        return nearest;
    }

    private void CalculateBounds()
    {
        if (Camera.main == null) return;
        float h = Camera.main.orthographicSize;
        float w = h * Camera.main.aspect;
        Vector3 c = Camera.main.transform.position;
        leftBound   = c.x - w + 1.2f;
        rightBound  = c.x + w - 1.2f;
        topBound    = c.y + h - 0.8f;
        bottomBound = BackgroundManager.GroundSurfaceY + 1.1f;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, leftBound, rightBound);
        p.y = Mathf.Clamp(p.y, bottomBound, topBound);
        transform.position = p;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && (other.GetComponent<KamehamehaC>() != null || other.gameObject.name.Contains("Projectile") || other.gameObject.name.Contains("Kamehameha")))
            TakeHit();
    }

    private void OnDestroy()
    {
        if (hpBarObj != null) Destroy(hpBarObj);
    }
}

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
    private SpriteRenderer auraRenderer;
    private Sprite[] auraFrames;
    private Sprite[] blastFrames;
    private Sprite[] beamFrames;
    private Sprite[] impactFrames;
    private float auraFrameTimer;
    private int auraFrameIndex;
    private float rangedAttackCooldown = 2.4f;
    private bool isRangedAttacking;
    private int nextRangedPattern;
    private static Material projectileTrailMaterial;
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

        // Dùng chân dung Perfect Cell lấy từ kho Item; giữ ảnh cũ làm fallback.
        Sprite cellSprite = Resources.Load<Sprite>("cell_item");
        if (cellSprite == null)
            cellSprite = Resources.Load<Sprite>("cell");
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
        transform.localScale = new Vector3(1.875f, 1.2f, 1f);
        LoadCombatEffects();
        CreateAuraRenderer();

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
        rangedAttackCooldown = 1.5f;
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
        rangedAttackCooldown -= Time.deltaTime;

        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (isRangedAttacking)
        {
            AnimateAura();
            return;
        }

        switch (state)
        {
            case State.Idle:    HandleIdle(player);    break;
            case State.Jump:    HandleJump(player);    break;
            case State.Chase:   HandleChase(player);   break;
            case State.PowerUp: HandleChase(player);   break; // PowerUp vẫn chase nhưng nhanh hơn
        }

        if (player != null && !player.isDead && rangedAttackCooldown <= 0f)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > 3.5f && distance < 15f)
                StartCoroutine(RangedAttackRoutine(player, nextRangedPattern++ % 3));
        }
    }

    private void LoadCombatEffects()
    {
        PlayerA player = FindFirstObjectByType<PlayerA>();
        if (player != null)
        {
            auraFrames = player.auraFrames;
            blastFrames = player.chargingBallFrames;
        }

        KamehamehaC effects = player != null && player.projectileCPrefab != null
            ? player.projectileCPrefab.GetComponent<KamehamehaC>()
            : null;
        if (effects != null)
        {
            beamFrames = CompactSprites(effects.spriteStraight, effects.spriteAngle40, effects.spriteAngle80, effects.spriteNearTarget);
            impactFrames = CompactSprites(effects.exp63, effects.exp64, effects.exp65, effects.exp77, effects.exp78,
                effects.exp53, effects.exp54, effects.exp55);
        }

        if (auraFrames == null || auraFrames.Length == 0)
            auraFrames = FindLoadedSprites("Small982", "Small983", "Small984", "Small985");
        if (blastFrames == null || blastFrames.Length == 0)
            blastFrames = FindLoadedSprites("Small48", "Small49", "Small50", "Small51");
        if (beamFrames == null || beamFrames.Length == 0)
            beamFrames = FindLoadedSprites("Small59", "Small60", "Small61", "Small62");
        if (impactFrames == null || impactFrames.Length == 0)
            impactFrames = FindLoadedSprites("Small63", "Small64", "Small65", "Small77", "Small78", "Small53", "Small54", "Small55");
    }

    private Sprite[] CompactSprites(params Sprite[] sprites)
    {
        System.Collections.Generic.List<Sprite> frames = new System.Collections.Generic.List<Sprite>();
        foreach (Sprite sprite in sprites)
            if (sprite != null) frames.Add(sprite);
        return frames.ToArray();
    }

    private Sprite[] FindLoadedSprites(params string[] spriteNames)
    {
        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        System.Collections.Generic.List<Sprite> frames = new System.Collections.Generic.List<Sprite>();
        foreach (string spriteName in spriteNames)
        {
            foreach (Sprite sprite in loadedSprites)
            {
                if (sprite != null && sprite.name == spriteName)
                {
                    frames.Add(sprite);
                    break;
                }
            }
        }
        return frames.ToArray();
    }

    private void CreateAuraRenderer()
    {
        GameObject auraObject = new GameObject("CellPowerAura");
        auraObject.transform.SetParent(transform, false);
        auraObject.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        auraObject.transform.localScale = Vector3.one * 1.25f;
        auraRenderer = auraObject.AddComponent<SpriteRenderer>();
        auraRenderer.sortingOrder = 14;
        auraRenderer.color = new Color(0.4f, 1f, 0.45f, 0.8f);
        auraObject.SetActive(false);
    }

    private void AnimateAura()
    {
        if (auraRenderer == null || auraFrames == null || auraFrames.Length == 0) return;
        auraFrameTimer += Time.deltaTime;
        if (auraFrameTimer >= 0.07f)
        {
            auraFrameTimer = 0f;
            auraRenderer.sprite = auraFrames[auraFrameIndex++ % auraFrames.Length];
        }
        float pulse = 1.1f + Mathf.Sin(Time.time * 16f) * 0.12f;
        auraRenderer.transform.localScale = Vector3.one * pulse;
    }

    private IEnumerator RangedAttackRoutine(PlayerA target, int pattern)
    {
        isRangedAttacking = true;
        rangedAttackCooldown = isPoweredUp ? 2.1f : 3.2f;
        if (auraRenderer != null) auraRenderer.gameObject.SetActive(true);

        float chargeTime = pattern == 1 ? 0.72f : 0.38f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWarningBeeps();
        float elapsed = 0f;
        while (elapsed < chargeTime && !isDead)
        {
            elapsed += Time.deltaTime;
            AnimateAura();
            if (sr != null)
            {
                float chargePulse = 0.65f + Mathf.PingPong(elapsed * 2.8f, 0.35f);
                sr.color = Color.Lerp(isPoweredUp ? CELL_POWERUP : CELL_NORMAL, Color.white, chargePulse);
            }
            yield return null;
        }

        if (sr != null) sr.color = isPoweredUp ? CELL_POWERUP : CELL_NORMAL;

        if (!isDead && target != null && !target.isDead)
        {
            if (pattern == 0)
            {
                for (int shot = 0; shot < 3; shot++)
                {
                    Vector3 direction = (target.transform.position - transform.position).normalized;
                    FireCellProjectile(direction, true, 8.5f, isPoweredUp ? 6f : 4.5f, 0.48f, blastFrames, false);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
                    yield return new WaitForSeconds(0.18f);
                }
            }
            else if (pattern == 1)
            {
                Vector3 direction = (target.transform.position - transform.position).normalized;
                FireCellProjectile(direction, true, 16f, isPoweredUp ? 14f : 10f, 0.78f, beamFrames, true);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
            }
            else
            {
                Vector3 direction = (target.transform.position - transform.position).normalized;
                for (int shot = -2; shot <= 2; shot++)
                {
                    Vector3 spreadDirection = Quaternion.Euler(0f, 0f, shot * 12f) * direction;
                    FireCellProjectile(spreadDirection, false, 9f, isPoweredUp ? 5f : 3.5f, 0.4f, blastFrames, false);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
            }
        }

        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
        isRangedAttacking = false;
    }

    private void FireCellProjectile(Vector3 direction, bool homing, float speed, float damage, float size, Sprite[] frames, bool beam)
    {
        GameObject projectile = new GameObject(beam ? "CellEnergyBeam" : "CellKiBlast");
        projectile.transform.position = transform.position + direction * 1.1f;
        projectile.transform.localScale = beam ? new Vector3(1.25f, 0.65f, 1f) : Vector3.one * size;

        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 30;
        renderer.color = beam
            ? new Color(0.65f, 1f, 0.72f, 0.96f)
            : (isPoweredUp ? new Color(1f, 0.28f, 0.12f) : new Color(0.45f, 1f, 0.35f));
        if (frames != null && frames.Length > 0) renderer.sprite = frames[0];

        if (beam)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = beam ? 0.3f : 0.2f;
        trail.startWidth = beam ? 0.55f : size * 0.65f;
        trail.endWidth = 0f;
        if (projectileTrailMaterial == null)
            projectileTrailMaterial = new Material(Shader.Find("Sprites/Default"));
        trail.material = projectileTrailMaterial;
        trail.startColor = renderer.color;
        trail.endColor = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 0f);

        StartCoroutine(CellProjectileFlight(projectile, direction, homing, speed, damage, frames, beam));
    }

    private IEnumerator CellProjectileFlight(GameObject projectile, Vector3 direction, bool homing, float speed, float damage, Sprite[] frames, bool beam)
    {
        float elapsed = 0f;
        float frameTimer = 0f;
        int frameIndex = 0;
        PlayerA target = FindFirstObjectByType<PlayerA>();
        while (projectile != null && elapsed < 3f)
        {
            elapsed += Time.deltaTime;
            frameTimer += Time.deltaTime;
            if (frames != null && frames.Length > 0 && frameTimer >= 0.055f)
            {
                frameTimer = 0f;
                SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.sprite = frames[frameIndex++ % frames.Length];
            }

            if (homing && target != null && !target.isDead)
            {
                Vector3 toTarget = (target.transform.position - projectile.transform.position).normalized;
                direction = Vector3.RotateTowards(direction, toTarget, 2.2f * Time.deltaTime, 0f).normalized;
            }

            projectile.transform.position += direction * speed * Time.deltaTime;
            if (beam)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (target != null && !target.isDead && Vector3.Distance(projectile.transform.position, target.transform.position) < (beam ? 1.0f : 0.75f))
            {
                target.TakeDamage(damage);
                SpawnCellImpact(projectile.transform.position);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                Destroy(projectile);
                yield break;
            }
            yield return null;
        }

        if (projectile != null) Destroy(projectile);
    }

    private void SpawnCellImpact(Vector3 position)
    {
        if (impactFrames == null || impactFrames.Length == 0) return;
        GameObject effect = new GameObject("CellImpactEffect");
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.8f;
        SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 31;
        StartCoroutine(PlayImpact(effect, renderer));
    }

    private IEnumerator PlayImpact(GameObject effect, SpriteRenderer renderer)
    {
        float frameDuration = 0.035f;
        for (int i = 0; i < impactFrames.Length; i++)
        {
            if (effect == null) yield break;
            renderer.sprite = impactFrames[i];
            effect.transform.localScale = Vector3.one * (0.75f + i * 0.07f);
            yield return new WaitForSeconds(frameDuration);
        }
        if (effect != null) Destroy(effect);
    }

    private const float BOSS_SCALE_X = 1.875f;
    private const float BOSS_SCALE_Y = 1.2f;

    private void HandleIdle(PlayerA player)
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.transform.position);

        // Cell luôn luôn chủ động tìm kiếm và áp sát Goku, không bao giờ đứng im mất tích
        Vector3 dir = (player.transform.position - transform.position).normalized;
        transform.position += dir * (moveSpeed * 0.75f) * Time.deltaTime;

        if (dir.x < 0) transform.localScale = new Vector3(-BOSS_SCALE_X, BOSS_SCALE_Y, 1f);
        else           transform.localScale = new Vector3( BOSS_SCALE_X, BOSS_SCALE_Y, 1f);

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

        if (dir.x < 0) transform.localScale = new Vector3(-BOSS_SCALE_X, BOSS_SCALE_Y, 1f);
        else           transform.localScale = new Vector3( BOSS_SCALE_X, BOSS_SCALE_Y, 1f);

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

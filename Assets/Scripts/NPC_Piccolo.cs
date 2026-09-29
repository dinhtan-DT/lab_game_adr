using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kẻ địch Piccolo tại Cấp 2 (Hành tinh Namek):
/// - Chịu đúng 3 viên đạn chưởng mới bị tiêu diệt (maxHp = 3).
/// - Có thanh máu 3 vạch và tag tên [Piccolo] trên đầu.
/// - Được lắp ghép từ sprite Piccolo trích xuất từ Item (Đầu Small121/122, Thân Small123/125, Chân Small138/139).
/// - Khi HP <= 0: Phát nổ và báo về LevelManager.RegisterPiccoloKill().
/// </summary>
[RequireComponent(typeof(CharacterParts))]
public class NPC_Piccolo : MonoBehaviour
{
    [Header("Chỉ Số Máu & Sát Thương (5 Hit để diệt)")]
    public int maxHp = 5;
    public int currentHp = 5;
    public float attackDamage = 10f;
    public float moveSpeed = 2.2f;
    public bool isDead = false;
    public bool isStunned = false;

    private CharacterParts parts;
    private Sprite headIdle;
    private Sprite headMove;
    private Sprite bodyIdle;
    private Sprite bodyMove;
    private Sprite legIdle;
    private Sprite legMove;

    // Overhead UI
    private GameObject overheadCanvas;
    private Image[] hpSegments;
    private Text nameTagText;

    private PlayerA player;
    private float attackCooldown = 3.0f; // Thời gian chờ 3 giây khi mới sinh để người chơi không bị tấn công tức thì
    private Vector3 patrolTarget;
    private float patrolTimer = 0f;

    private void Awake()
    {
        parts = GetComponent<CharacterParts>();
        LoadPiccoloSprites();

        // Cấu hình Collider & Rigidbody để nhận va chạm đạn chưởng
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.1f; // Bán kính va chạm khớp với kích thước boss to ~235x235

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Start()
    {
        currentHp = maxHp;
        isDead = false;
        attackCooldown = 3.0f; // 3 giây an toàn khi bắt đầu màn

        if (parts != null)
        {
            parts.characterScale = 0.85f; // Boss siêu to khổng lồ (~235x235 px) đúng theo mong muốn của người chơi
            parts.headHeight = 1.6f;
            parts.legHeight = -0.6f;
            parts.SetPose(headIdle, bodyIdle, legIdle);
            parts.Flip(false);
        }

        player = FindFirstObjectByType<PlayerA>();
        BuildOverheadUI();
        PickNewPatrolTarget();
    }

    private void LoadPiccoloSprites()
    {
        headIdle = Resources.Load<Sprite>("piccolo_head_idle");
        headMove = Resources.Load<Sprite>("piccolo_head_move");
        bodyIdle = Resources.Load<Sprite>("piccolo_body_idle");
        bodyMove = Resources.Load<Sprite>("piccolo_body_move");
        legIdle  = Resources.Load<Sprite>("piccolo_leg_idle");
        legMove  = Resources.Load<Sprite>("piccolo_leg_move");

        // Fallback từ Sprites nếu chưa load được
        if (headIdle == null) headIdle = Resources.Load<Sprite>("Small91");
        if (headMove == null) headMove = Resources.Load<Sprite>("Small92");
        if (bodyIdle == null) bodyIdle = Resources.Load<Sprite>("1");
        if (bodyMove == null) bodyMove = Resources.Load<Sprite>("2");
        if (legIdle == null)  legIdle  = Resources.Load<Sprite>("22");
        if (legMove == null)  legMove  = Resources.Load<Sprite>("23");
    }

    private void BuildOverheadUI()
    {
        if (overheadCanvas != null) Destroy(overheadCanvas);

        overheadCanvas = new GameObject("OverheadUI");
        overheadCanvas.transform.SetParent(transform, false);
        overheadCanvas.transform.localPosition = new Vector3(0f, 2.5f, 0f); // Nâng cao trên đỉnh đầu boss to

        Canvas canvas = overheadCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rt = overheadCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150f, 45f);
        rt.localScale = new Vector3(0.018f, 0.018f, 1f);

        // 1. Nhãn tên [Piccolo] màu xanh ngọc Namek
        GameObject nameObj = new GameObject("NameTag");
        nameObj.transform.SetParent(overheadCanvas.transform, false);
        RectTransform nRt = nameObj.AddComponent<RectTransform>();
        nRt.anchoredPosition = new Vector2(0f, 12f);
        nRt.sizeDelta = new Vector2(150f, 22f);
        nameTagText = nameObj.AddComponent<Text>();
        nameTagText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameTagText.text = "★ Piccolo (Namek)";
        nameTagText.fontSize = 15;
        nameTagText.fontStyle = FontStyle.Bold;
        nameTagText.alignment = TextAnchor.MiddleCenter;
        nameTagText.color = new Color(0.2f, 1f, 0.4f, 1f); // Màu xanh Namek

        // 2. Thanh máu 5 vạch (5 Hit)
        GameObject hpBarObj = new GameObject("HPBar");
        hpBarObj.transform.SetParent(overheadCanvas.transform, false);
        RectTransform hpRt = hpBarObj.AddComponent<RectTransform>();
        hpRt.anchoredPosition = new Vector2(0f, -8f);
        hpRt.sizeDelta = new Vector2(115f, 12f);

        Image bg = hpBarObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        hpSegments = new Image[maxHp];
        float segWidth = 18f;
        float spacing = 21f;
        float startX = -((maxHp - 1) * spacing) / 2f;
        for (int i = 0; i < maxHp; i++)
        {
            GameObject segObj = new GameObject("Seg_" + i);
            segObj.transform.SetParent(hpBarObj.transform, false);
            RectTransform sRt = segObj.AddComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(startX + i * spacing, 0f);
            sRt.sizeDelta = new Vector2(segWidth, 8f);
            Image sImg = segObj.AddComponent<Image>();
            sImg.color = new Color(0.2f, 0.9f, 0.2f);
            hpSegments[i] = sImg;
        }

        UpdateHPUI();
    }

    private void UpdateHPUI()
    {
        if (hpSegments == null) return;
        for (int i = 0; i < hpSegments.Length; i++)
        {
            if (hpSegments[i] != null)
            {
                bool active = i < currentHp;
                Color segColor = Color.green;
                if (currentHp <= 2) segColor = Color.red;
                else if (currentHp <= 3) segColor = Color.yellow;
                hpSegments[i].color = active ? segColor : new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }
    }

    private void Update()
    {
        if (isDead || isStunned) return;

        if (attackCooldown > 0f) attackCooldown -= Time.deltaTime;

        if (player == null || !player.gameObject.activeInHierarchy)
            player = FindFirstObjectByType<PlayerA>();

        // AI di chuyển và tiếp cận người chơi
        HandleAI();
    }

    private void HandleAI()
    {
        Vector3 targetPos = patrolTarget;

        if (player != null && !player.isDead)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);

            // Nếu người chơi ở cự ly săn tìm (dưới 8m) -> lao tới tấn công
            if (dist < 8.0f)
            {
                targetPos = player.transform.position;

                // Nếu rất gần -> gây sát thương
                if (dist < 1.2f && attackCooldown <= 0f)
                {
                    player.TakeDamage(attackDamage);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    attackCooldown = 2.2f;
                    PickNewPatrolTarget(); // Tạm lùi về vị trí tuần tra để người chơi có khoảng trống phản đòn
                }
            }
        }

        // Di chuyển tới targetPos
        Vector3 dir = (targetPos - transform.position);
        if (dir.magnitude > 0.1f)
        {
            Vector3 step = dir.normalized * moveSpeed * Time.deltaTime;
            transform.position += step;

            if (parts != null)
            {
                parts.Flip(dir.x > 0);
                parts.SetPose(headMove, bodyMove, legMove);
            }
        }
        else
        {
            if (parts != null) parts.SetPose(headIdle, bodyIdle, legIdle);
        }

        patrolTimer += Time.deltaTime;
        if (patrolTimer > 3.0f)
        {
            patrolTimer = 0f;
            PickNewPatrolTarget();
        }
    }

    private void PickNewPatrolTarget()
    {
        if (Camera.main == null) return;
        float rx = Random.Range(0.4f, 0.9f);
        float ry = Random.Range(0.25f, 0.8f);
        Vector3 pos = Camera.main.ViewportToWorldPoint(new Vector3(rx, ry, 10f));
        pos.z = 0f;
        patrolTarget = pos;
    }

    /// <summary>
    /// Nhận 1 hit sát thương từ đạn chưởng hoặc đòn cận chiến
    /// </summary>
    public void TakeHit(int damage = 1)
    {
        if (isDead) return;

        currentHp = Mathf.Max(0, currentHp - damage);
        UpdateHPUI();
        StartCoroutine(FlashRoutine());

        if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (LevelManager.Instance != null)
            LevelManager.Instance.RegisterPiccoloKill();

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(150);

        Destroy(gameObject);
    }

    private IEnumerator FlashRoutine()
    {
        if (parts != null && parts.bodyRenderer != null)
        {
            Color oldColor = parts.bodyRenderer.color;
            parts.bodyRenderer.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            if (parts != null && parts.bodyRenderer != null)
                parts.bodyRenderer.color = oldColor;
        }
    }

    public IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        // Bị đạn chưởng C (Kamehameha) va chạm
        KamehamehaC bullet = collision.GetComponent<KamehamehaC>();
        if (bullet != null)
        {
            TakeHit(1);
        }
    }
}

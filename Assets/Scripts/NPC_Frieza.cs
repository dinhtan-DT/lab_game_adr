using System.Collections;
using UnityEngine;

/// <summary>
/// NPC Thông Minh #2: Frieza
/// AI State Machine: Patrol → Aggro → Attack → Recover
/// - Patrol:  Bay theo sóng sin ngang màn hình
/// - Aggro:   Khi A vào vòng 4 unit → bay thẳng về phía A
/// - Attack:  Khi A vào vòng 2 unit → body slam gây 30 damage
/// - Recover: Đứng yên 0.5s sau khi tấn công
/// Gây sát thương khi chưởng C chạm → tự hồi sinh sau 1.5s
/// </summary>
public class NPC_Frieza : MonoBehaviour
{
    // ---- Cấu hình Cân Bằng (Không quá khó / ức chế) ----
    public float patrolSpeed  = 2.5f; // Tốc độ bay tuần tra chậm rãi
    public float chaseSpeed   = 4.0f; // Tốc độ đuổi vừa phải để người chơi né được
    public float attackDamage = 10f;  // Sát thương 10 (thay vì 30 quá nặng)
    public float aggroRange   = 4.0f;
    public float attackRange  = 2.0f;
    public float recoverTime  = 1.2f; // Đứng nghỉ 1.2s sau khi lao đánh để người chơi có cơ hội bắn trả

    // ---- State ----
    private enum State { Patrol, Aggro, Attack, Recover, Dead }
    private State state = State.Patrol;

    // ---- Renderer ----
    private SpriteRenderer sr;
    private static readonly Color FRIEZA_TINT = new Color(0.85f, 0.25f, 0.90f, 1f); // tím nhạt

    // ---- Patrol wave ----
    private float patrolTimer = 0f;
    private float patrolDirX  = -1f; // Đi sang trái trước
    private float initialY;

    // ---- Bounds ----
    private float leftBound, rightBound, topBound, bottomBound;

    // ---- Kill/Respawn ----
    private bool isDead = false;
    private int  killHits = 1; // Số chưởng cần để hạ (1 = chết ngay)

    private void Awake()
    {
        // Renderer
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        // Nạp sprite frieza.png (Resources)
        Sprite friezaSprite = Resources.Load<Sprite>("frieza");
        if (friezaSprite != null)
        {
            sr.sprite = friezaSprite;
            sr.color  = Color.white;
        }
        else
        {
            sr.sprite = Resources.Load<Sprite>("Small91");
            sr.color  = FRIEZA_TINT;
        }
#if UNITY_EDITOR
        if (sr.sprite == null)
        {
            sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Small91.png");
            sr.color  = FRIEZA_TINT;
        }
#endif

        sr.sortingOrder = 14; // Nổi bật ở tiền cảnh
        transform.localScale = new Vector3(2.4f, 2.4f, 1f); // Kích cỡ ~235x235 px (to rõ, dễ nhìn)

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f; // Bán kính 0.5f ở scale 2.4 = 1.2m
    }

    private void Start()
    {
        initialY = transform.position.y;
        CalculateBounds();
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
        tm.text = "⚡ NPC 2: FRIEZA (Hộ Vệ) ⚡";
        tm.fontSize = 24;
        tm.characterSize = 0.025f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.95f, 0.5f, 1f, 1f); // Tím Frieza

        MeshRenderer mr = tagObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 25;
    }

    private void CalculateBounds()
    {
        if (Camera.main == null) return;
        float h = Camera.main.orthographicSize;
        float w = h * Camera.main.aspect;
        Vector3 c = Camera.main.transform.position;
        leftBound   = c.x - w + 0.5f;
        rightBound  = c.x + w - 0.5f;
        topBound    = c.y + h - 0.5f;
        bottomBound = BackgroundManager.GroundSurfaceY + 1.0f;
    }

    private void Update()
    {
        if (isDead) return;

        CalculateBounds();
        PlayerA player = FindFirstObjectByType<PlayerA>();

        switch (state)
        {
            case State.Patrol:   HandlePatrol(player);   break;
            case State.Aggro:    HandleAggro(player);    break;
            case State.Attack:   HandleAttack(player);   break;
            case State.Recover:  /* Xử lý trong Coroutine */ break;
        }

        ClampToBounds();
    }

    // ──────────────────────────────────────────
    // PATROL: Bay sóng sin, đổi hướng khi chạm biên
    // ──────────────────────────────────────────
    private void HandlePatrol(PlayerA player)
    {
        patrolTimer += Time.deltaTime;

        float sinY = Mathf.Sin(patrolTimer * 1.8f) * 1.5f;
        Vector3 move = new Vector3(patrolDirX * patrolSpeed * Time.deltaTime,
                                   sinY * Time.deltaTime, 0f);
        transform.position += move;

        // Đảo hướng khi chạm biên trái/phải
        if (transform.position.x <= leftBound)  patrolDirX =  1f;
        if (transform.position.x >= rightBound) patrolDirX = -1f;

        // Chuyển sang Aggro nếu A vào phạm vi
        if (player != null && player.gameObject.activeInHierarchy)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= aggroRange) TransitionTo(State.Aggro);
        }
    }

    // ──────────────────────────────────────────
    // AGGRO: Bay thẳng về phía A
    // ──────────────────────────────────────────
    private void HandleAggro(PlayerA player)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            TransitionTo(State.Patrol);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.transform.position);

        // Về Patrol nếu A đi quá xa
        if (dist > aggroRange * 1.5f)
        {
            TransitionTo(State.Patrol);
            return;
        }

        // Chuyển sang Attack nếu rất gần
        if (dist <= attackRange)
        {
            TransitionTo(State.Attack);
            return;
        }

        // Bay đến A
        Vector3 dir = (player.transform.position - transform.position).normalized;
        transform.position += dir * chaseSpeed * Time.deltaTime;

        // Flip sprite theo hướng di chuyển
        if (dir.x < 0) transform.localScale = new Vector3(-0.2f, 0.2f, 1f);
        else            transform.localScale = new Vector3( 0.2f, 0.2f, 1f);
    }

    // ──────────────────────────────────────────
    // ATTACK: Lao vào body slam
    // ──────────────────────────────────────────
    private void HandleAttack(PlayerA player)
    {
        if (player == null) { TransitionTo(State.Patrol); return; }

        // Gây damage và chuyển sang Recover
        player.TakeDamage(attackDamage);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
        TransitionTo(State.Recover);
        StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        // Nhấp nháy màu đỏ khi tấn công
        if (sr != null) sr.color = Color.red;
        yield return new WaitForSeconds(recoverTime);
        if (sr != null) sr.color = FRIEZA_TINT;
        TransitionTo(State.Patrol);
    }

    // ──────────────────────────────────────────
    // Bị chưởng C chạm
    // ──────────────────────────────────────────
    public void TakeHit()
    {
        if (isDead) return;
        killHits--;
        if (killHits <= 0)
            StartCoroutine(DieRoutine());
        else
        {
            // Nhấp nháy trắng
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (sr != null) sr.color = Color.white;
        yield return new WaitForSeconds(0.15f);
        if (sr != null) sr.color = FRIEZA_TINT;
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();

        // Báo kill cho LevelManager
        if (LevelManager.Instance != null) LevelManager.Instance.RegisterKill();

        // Hiệu ứng: xoay và thu nhỏ
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0f, t / 0.6f);
            transform.localScale = startScale * s;
            transform.Rotate(0, 0, 360f * Time.deltaTime);
            if (sr != null) sr.color = Color.Lerp(FRIEZA_TINT, Color.clear, t / 0.6f);
            yield return null;
        }

        // Hồi sinh sau 4.5s để người chơi có khoảng thở tiêu diệt Cell
        yield return new WaitForSeconds(4.5f);
        isDead = false;
        killHits = 1;
        transform.localScale = new Vector3(0.32f, 0.32f, 1f);
        if (sr != null) sr.color = FRIEZA_TINT;

        // Spawn lại ở biên phải
        if (Camera.main != null)
        {
            float rx = Random.Range(0.65f, 0.92f);
            float ry = Random.Range(0.25f, 0.75f);
            Vector3 pos = Camera.main.ViewportToWorldPoint(new Vector3(rx, ry, 10f));
            pos.z = 0f;
            transform.position = pos;
        }
        TransitionTo(State.Patrol);
    }

    private void ClampToBounds()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, leftBound, rightBound);
        p.y = Mathf.Clamp(p.y, bottomBound, topBound);
        transform.position = p;
    }

    private void TransitionTo(State next)
    {
        state = next;
    }

    // Để KamehamehaC gọi khi va chạm
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && (other.GetComponent<KamehamehaC>() != null || other.gameObject.name.Contains("Projectile") || other.gameObject.name.Contains("Kamehameha")))
            TakeHit();
    }
}

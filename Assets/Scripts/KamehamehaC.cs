using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều khiển Đối tượng C (Chưởng lực Kamehameha):
/// - Tự động đuổi theo B (Homing), đổi hướng ngay cả khi B respawn.
/// - Tốc độ bay nhanh (speed = 16).
/// - Đổi sprite theo góc: Small59 (thẳng), Small60 (40 độ), Small61 (80 độ).
/// - Lật ảnh khi B ở dưới A.
/// - Khi gần tới B: đổi thành Small62.
/// - Khi chạm B: nổ xoay tròn cực nhanh (0.45s) theo chuỗi:
///   Small63 -> Small64 -> (Small65 + Small77 + Small78 cùng lúc) -> Small53 -> Small54 -> Small55 rồi kết thúc!
/// </summary>
public class KamehamehaC : MonoBehaviour
{
    [Header("Cấu hình bay (Bay nhanh & kết thúc nhanh)")]
    public float speed = 16f;
    [Range(0.05f, 1.5f)]
    [Tooltip("Kích thước đạn C (Mặc định 0.2 đã tăng x2)")]
    public float projectileScale = 0.2f;
    public float hitDistance = 1.2f;
    public float nearDistance = 1.8f;
    public float explosionDuration = 0.45f;

    [Header("Sprite Bay (Góc & Gần Đích)")]
    public Sprite spriteStraight;   // Small59.png (Thẳng ngang)
    public Sprite spriteAngle40;    // Small60.png (Góc 40 độ)
    public Sprite spriteAngle80;    // Small61.png (Góc 80 độ)
    public Sprite spriteNearTarget; // Small62.png (Gần mục tiêu)

    [Header("Sprite Phát Nổ (Small63 -> Small55)")]
    public Sprite exp63; // Small63.png
    public Sprite exp64; // Small64.png
    public Sprite exp65; // Small65.png
    public Sprite exp77; // Small77.png (cùng lúc với 65)
    public Sprite exp78; // Small78.png (cùng lúc với 65)
    public Sprite exp53; // Small53.png
    public Sprite exp54; // Small54.png
    public Sprite exp55; // Small55.png

    private Transform targetEnemyB;
    private SpriteRenderer mainRenderer;
    private SpriteRenderer extraRenderer1;
    private SpriteRenderer extraRenderer2;
    private bool isExploding = false;

    private void Awake()
    {
        if (projectileScale <= 0.01f || Mathf.Approximately(projectileScale, 0.1f)) projectileScale = 0.2f;
        transform.localScale = new Vector3(projectileScale, projectileScale, 1f);
        mainRenderer = GetComponent<SpriteRenderer>();
        if (mainRenderer == null) mainRenderer = gameObject.AddComponent<SpriteRenderer>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.0f;

        extraRenderer1 = CreateChildRenderer("ExtraExp1");
        extraRenderer2 = CreateChildRenderer("ExtraExp2");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isExploding) return;

        EnemyB eb = other.GetComponent<EnemyB>();
        if (eb != null && eb.gameObject.activeInHierarchy)
        {
            eb.OnHitBySkill(explosionDuration);
            StartCoroutine(ExplosionRoutine());
            return;
        }

        NPC_Frieza frieza = other.GetComponent<NPC_Frieza>();
        if (frieza != null && frieza.gameObject.activeInHierarchy)
        {
            frieza.TakeHit();
            StartCoroutine(ExplosionRoutine());
            return;
        }

        NPC_Cell cell = other.GetComponent<NPC_Cell>();
        if (cell != null && cell.gameObject.activeInHierarchy)
        {
            cell.TakeHit(3);
            StartCoroutine(ExplosionRoutine());
            return;
        }

        NPC_Piccolo piccolo = other.GetComponent<NPC_Piccolo>();
        if (piccolo != null && piccolo.gameObject.activeInHierarchy && !piccolo.isDead)
        {
            piccolo.TakeHit(1);
            StartCoroutine(ExplosionRoutine());
            return;
        }

        Boss_PiccoloA bossPiccolo = other.GetComponent<Boss_PiccoloA>();
        if (bossPiccolo != null && bossPiccolo.gameObject.activeInHierarchy && !bossPiccolo.isDead)
        {
            bossPiccolo.TakeHit(3);
            StartCoroutine(ExplosionRoutine());
            return;
        }
    }

    private SpriteRenderer CreateChildRenderer(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localScale = Vector3.one;
        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sortingOrder = mainRenderer.sortingOrder + 1;
        child.SetActive(false);
        return sr;
    }

    public void Init(Transform target)
    {
        targetEnemyB = target;
    }

    private void Update()
    {
        if (isExploding) return;

        if (targetEnemyB == null)
        {
            float closest = float.MaxValue;
            EnemyB[] enemies = FindObjectsByType<EnemyB>(FindObjectsSortMode.None);
            if (enemies != null)
            {
                foreach (var e in enemies)
                {
                    if (e == null || !e.gameObject.activeInHierarchy) continue;
                    float d = Vector3.Distance(transform.position, e.transform.position);
                    if (d < closest)
                    {
                        closest = d;
                        targetEnemyB = e.transform;
                    }
                }
            }
            NPC_Cell[] cells = FindObjectsByType<NPC_Cell>(FindObjectsSortMode.None);
            if (cells != null)
            {
                foreach (var c in cells)
                {
                    if (c == null || !c.gameObject.activeInHierarchy) continue;
                    float d = Vector3.Distance(transform.position, c.transform.position);
                    if (d < closest)
                    {
                        closest = d;
                        targetEnemyB = c.transform;
                    }
                }
            }
            NPC_Frieza[] friezas = FindObjectsByType<NPC_Frieza>(FindObjectsSortMode.None);
            if (friezas != null)
            {
                foreach (var f in friezas)
                {
                    if (f == null || !f.gameObject.activeInHierarchy) continue;
                    float d = Vector3.Distance(transform.position, f.transform.position);
                    if (d < closest)
                    {
                        closest = d;
                        targetEnemyB = f.transform;
                    }
                }
            }
            NPC_Piccolo[] piccolos = FindObjectsByType<NPC_Piccolo>(FindObjectsSortMode.None);
            if (piccolos != null)
            {
                foreach (var p in piccolos)
                {
                    if (p == null || !p.gameObject.activeInHierarchy || p.isDead) continue;
                    float d = Vector3.Distance(transform.position, p.transform.position);
                    if (d < closest)
                    {
                        closest = d;
                        targetEnemyB = p.transform;
                    }
                }
            }
            Boss_PiccoloA[] pBosses = FindObjectsByType<Boss_PiccoloA>(FindObjectsSortMode.None);
            if (pBosses != null)
            {
                foreach (var pb in pBosses)
                {
                    if (pb == null || !pb.gameObject.activeInHierarchy || pb.isDead) continue;
                    float d = Vector3.Distance(transform.position, pb.transform.position);
                    if (d < closest)
                    {
                        closest = d;
                        targetEnemyB = pb.transform;
                    }
                }
            }
            if (targetEnemyB == null)
            {
                transform.position += Vector3.right * speed * Time.deltaTime;
                return;
            }
        }

        Vector3 diff = targetEnemyB.position - transform.position;
        float dist = diff.magnitude;

        if (dist <= hitDistance)
        {
            if (targetEnemyB != null)
            {
                EnemyB enemyB = targetEnemyB.GetComponent<EnemyB>();
                if (enemyB != null)
                {
                    enemyB.OnHitBySkill(explosionDuration);
                }
                NPC_Frieza frieza = targetEnemyB.GetComponent<NPC_Frieza>();
                if (frieza != null)
                {
                    frieza.TakeHit();
                }
                NPC_Cell cell = targetEnemyB.GetComponent<NPC_Cell>();
                if (cell != null)
                {
                    cell.TakeHit(3);
                }
                NPC_Piccolo piccolo = targetEnemyB.GetComponent<NPC_Piccolo>();
                if (piccolo != null)
                {
                    piccolo.TakeHit(1);
                }
                Boss_PiccoloA bossPiccolo = targetEnemyB.GetComponent<Boss_PiccoloA>();
                if (bossPiccolo != null)
                {
                    bossPiccolo.TakeHit(3);
                }
            }
            StartCoroutine(ExplosionRoutine());
            return;
        }

        UpdateFlightVisuals(diff, dist);

        // Bỏ cơ chế nảy tường/đất, đạn C bay thẳng đuổi theo B hoặc bay theo đường thẳng mượt mà
        transform.position = Vector3.MoveTowards(transform.position, targetEnemyB.position, speed * Time.deltaTime);
    }

    private void UpdateFlightVisuals(Vector3 diff, float dist)
    {
        if (dist <= nearDistance && spriteNearTarget != null)
        {
            mainRenderer.sprite = spriteNearTarget;
            return;
        }

        float angleDeg = Mathf.Abs(Mathf.Atan2(diff.y, Mathf.Abs(diff.x)) * Mathf.Rad2Deg);

        if (angleDeg < 25f && spriteStraight != null)
        {
            mainRenderer.sprite = spriteStraight;
        }
        else if (angleDeg < 65f && spriteAngle40 != null)
        {
            mainRenderer.sprite = spriteAngle40;
        }
        else if (spriteAngle80 != null)
        {
            mainRenderer.sprite = spriteAngle80;
        }

        mainRenderer.flipY = (diff.y < -0.1f);
        mainRenderer.flipX = (diff.x < 0f);
    }

    /// <summary>
    /// Chuỗi phát nổ nhanh gọn dứt khoát (chỉ 0.45s) xoay tròn quanh B
    /// </summary>
    private IEnumerator ExplosionRoutine()
    {
        isExploding = true;
        mainRenderer.flipX = false;
        mainRenderer.flipY = false;

        if (targetEnemyB != null)
        {
            transform.position = targetEnemyB.position;
        }

        float frameTime = explosionDuration / 6.0f; // ~0.075s mỗi frame

        StartCoroutine(Rotate360Routine(explosionDuration));

        mainRenderer.sprite = exp63;
        yield return new WaitForSeconds(frameTime);

        mainRenderer.sprite = exp64;
        yield return new WaitForSeconds(frameTime);

        mainRenderer.sprite = exp65;
        if (extraRenderer1 != null && exp77 != null)
        {
            extraRenderer1.gameObject.SetActive(true);
            extraRenderer1.sprite = exp77;
        }
        if (extraRenderer2 != null && exp78 != null)
        {
            extraRenderer2.gameObject.SetActive(true);
            extraRenderer2.sprite = exp78;
        }
        yield return new WaitForSeconds(frameTime);

        if (extraRenderer1 != null) extraRenderer1.gameObject.SetActive(false);
        if (extraRenderer2 != null) extraRenderer2.gameObject.SetActive(false);

        mainRenderer.sprite = exp53;
        yield return new WaitForSeconds(frameTime);

        mainRenderer.sprite = exp54;
        yield return new WaitForSeconds(frameTime);

        mainRenderer.sprite = exp55;
        yield return new WaitForSeconds(frameTime);

        Destroy(gameObject);
    }

    private IEnumerator Rotate360Routine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.Rotate(0f, 0f, (720f / duration) * Time.deltaTime);
            yield return null;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Tự Động Gán Toàn Bộ Sprite (1-Click)")]
    public void AutoAssignSprites()
    {
        string p = "Assets/Sprites/";
        spriteStraight   = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small59.png");
        spriteAngle40    = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small60.png");
        spriteAngle80    = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small61.png");
        spriteNearTarget = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small62.png");

        exp63 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small63.png");
        exp64 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small64.png");
        exp65 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small65.png");
        exp77 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small77.png");
        exp78 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small78.png");
        exp53 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small53.png");
        exp54 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small54.png");
        exp55 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small55.png");

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

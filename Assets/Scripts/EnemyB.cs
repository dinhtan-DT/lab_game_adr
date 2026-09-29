using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều khiển Đối tượng B (Kẻ địch):
/// - Tự động xuất hiện ở giữa biên phải màn hình (đối diện A).
/// - Kích thước bằng A.
/// - Tốc độ di chuyển nhanh x2 (moveSpeed = 5).
/// - Đầu thay bằng Small91 (đứng yên) và Small92 (di chuyển), thân và chân giống A.
/// - Di chuyển linh hoạt sang trái.
/// - Tự động nạp sprite nếu bị trống.
/// </summary>
[RequireComponent(typeof(CharacterParts))]
public class EnemyB : MonoBehaviour
{
    [Header("Cấu hình di chuyển")]
    public float moveSpeed = 5f;

    [Header("Chế Độ Di Chuyển Ngẫu Nhiên (Random Roam)")]
    [Tooltip("Bật để B đổi hướng bay lượn ngẫu nhiên liên tục trong màn hình")]
    public bool randomMovement = true;
    [Tooltip("Thời gian tối thiểu để đổi hướng mới (giây)")]
    public float changeDirIntervalMin = 1.0f;
    [Tooltip("Thời gian tối đa để đổi hướng mới (giây)")]
    public float changeDirIntervalMax = 2.5f;
    [Tooltip("Bật: B bay lượn tự do khắp 3/4 màn hình. Tắt: Chỉ bay lượn ở nửa phải.")]
    public bool fullScreenRoam = true;

    [Header("Xác Suất Chạm Biên Trái & Biên Trên (Quy Tắc Đề Bài)")]
    [Range(0.2f, 1.0f)]
    [Tooltip("Tỉ lệ B chủ động bay hướng ra biên trái/biên trên để kích hoạt vòng lặp màn hình (Mặc định: 0.65)")]
    public float borderTriggerChance = 0.65f;

    [Header("Cấu hình khi chạy thẳng (Chế độ cũ)")]
    public bool verticalWobble = true;
    public float wobbleSpeed = 4f;
    public float wobbleMagnitude = 0.5f;

    [Header("Kích Thước Toàn Bộ (Đã nâng lên: 0.25)")]
    [Range(0.05f, 1.5f)]
    [Tooltip("Kéo thanh trượt để thu nhỏ / phóng to B (Mặc định 0.25)")]
    public float characterScale = 0.25f;

    [Header("Độ Cao Đầu của B (Vegeta - Đã hạ thấp 2px: 1.1375)")]
    [Range(0.5f, 3.0f)]
    [Tooltip("Kéo thanh trượt này để đầu Vegeta cao lên hoặc thấp xuống theo ý thích (Mặc định: 1.1375)")]
    public float headHeight = 1.1375f;

    [Header("Sprite Đầu của B (Thay thế cho A)")]
    public Sprite headIdle;       // Small91.png
    public Sprite headMove;       // Small92.png

    [Header("Sprite Thân & Chân (Dùng chung bộ của A)")]
    public Sprite bodyIdle;         // 1.png
    public Sprite legIdle;          // 22.png
    public Sprite[] bodyMoveFrames; // 2, 3, 4, 5, 6.png
    public Sprite[] legMoveFrames;  // 23, 24, 25, 26, 27.png
    public float moveFrameRate = 0.06f;

    private CharacterParts parts;
    private float moveAnimTimer = 0f;
    private int currentMoveFrame = 0;
    private float initialY = 0f;

    private Vector3 currentWaypoint;
    private float randomTimer = 0f;
    private float nextChangeTime = 1.5f;

    private float leftBorderX;
    private float rightBorderX;
    private float minSpawnY;
    private float maxSpawnY;

    private void Awake()
    {
        parts = GetComponent<CharacterParts>();
        if (moveSpeed < 4f) moveSpeed = 5f;
        if (moveFrameRate > 0.08f) moveFrameRate = 0.06f;

        // Tự động nâng kích cỡ lên 0.25 để bot to rõ ràng, dễ nhìn
        if (characterScale <= 0.01f || characterScale < 0.24f) characterScale = 0.25f;
        if (parts != null)
        {
            parts.characterScale = characterScale;
            if (headHeight > 1.35f || headHeight <= 0.05f || Mathf.Approximately(headHeight, 1.2f)) headHeight = 1.1375f;
            parts.headHeight = headHeight;
            parts.SyncAndApplyAll();
        }

        if (headIdle == null || bodyIdle == null || legIdle == null)
        {
            PlayerA pA = FindFirstObjectByType<PlayerA>();
            if (pA != null)
            {
                if (headIdle == null) headIdle = pA.enemyBHeadIdle;
                if (headMove == null) headMove = pA.enemyBHeadMove;
                if (bodyIdle == null) bodyIdle = pA.bodyIdle;
                if (legIdle == null) legIdle = pA.legIdle;
                if (bodyMoveFrames == null || bodyMoveFrames.Length == 0) bodyMoveFrames = pA.bodyMoveFrames;
                if (legMoveFrames == null || legMoveFrames.Length == 0) legMoveFrames = pA.legMoveFrames;
            }
        }

#if UNITY_EDITOR
        if (headIdle == null || bodyIdle == null || legIdle == null)
        {
            AutoAssignSprites();
        }
#endif
    }

    private void OnValidate()
    {
        if (parts == null) parts = GetComponent<CharacterParts>();
        if (parts != null)
        {
            parts.characterScale = characterScale;
            parts.headHeight = headHeight;
            parts.SyncAndApplyAll();
        }
    }

    private void Start()
    {
        if (characterScale <= 0.01f || Mathf.Approximately(characterScale, 0.1f)) characterScale = 0.2f;
        if (parts != null)
        {
            parts.characterScale = characterScale;
            parts.headHeight = headHeight;
            parts.SyncAndApplyAll();
            parts.SetPose(headIdle, bodyIdle, legIdle);
        }

        if (headIdle == null || bodyIdle == null || legIdle == null)
        {
            PlayerA pA = FindFirstObjectByType<PlayerA>();
            if (pA != null)
            {
                if (headIdle == null) headIdle = pA.enemyBHeadIdle;
                if (headMove == null) headMove = pA.enemyBHeadMove;
                if (bodyIdle == null) bodyIdle = pA.bodyIdle;
                if (legIdle == null) legIdle = pA.legIdle;
                if (bodyMoveFrames == null || bodyMoveFrames.Length == 0) bodyMoveFrames = pA.bodyMoveFrames;
                if (legMoveFrames == null || legMoveFrames.Length == 0) legMoveFrames = pA.legMoveFrames;
            }
            if (parts != null) parts.SetPose(headIdle, bodyIdle, legIdle);
        }

#if UNITY_EDITOR
        if (headIdle == null || bodyIdle == null || legIdle == null)
        {
            AutoAssignSprites();
        }
#endif
        CalculateScreenBounds();

        if (Camera.main != null)
        {
            Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(0.92f, 0.5f, 10f));
            spawnPos.z = 0f;
            transform.position = spawnPos;
            initialY = spawnPos.y;
        }

        parts.Flip(false);
        PickNewWaypoint();
        CreateOverheadNameTag();
    }

    private void CreateOverheadNameTag()
    {
        Transform existing = transform.Find("OverheadNameTag");
        if (existing != null) return;

        GameObject tagObj = new GameObject("OverheadNameTag");
        tagObj.transform.SetParent(transform, false);
        tagObj.transform.localPosition = new Vector3(0, 1.4f, 0);

        TextMesh tm = tagObj.AddComponent<TextMesh>();
        tm.text = "★ NPC 1: VEGETA (AI) ★";
        tm.fontSize = 28;
        tm.characterSize = 0.045f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.85f, 0.1f, 1f); // Vàng kim

        MeshRenderer mr = tagObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 25;
    }

    private bool isHit = false;

    private void Update()
    {
        CalculateScreenBounds();

        if (isHit || isStunned) return;

        if (randomMovement)
        {
            HandleRandomRoam();
        }
        else
        {
            HandleStraightMovement();
        }

        CheckScreenBordersAndWrap();
        UpdateMoveAnimation();

        CheckForbiddenZone();
        CheckCollisionWithPlayer();
    }

    private void CheckForbiddenZone()
    {
        // Vùng cấm: Sát mặt đất (Y < GroundY + 1.5f)
        if (transform.position.y < BackgroundManager.GroundSurfaceY + 1.5f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWarningBeeps();
            }
        }
    }

    private void CheckCollisionWithPlayer()
    {
        PlayerA pA = FindFirstObjectByType<PlayerA>();
        if (pA != null && pA.gameObject.activeInHierarchy)
        {
            if (Vector3.Distance(transform.position, pA.transform.position) < 1.2f)
            {
                if (pA.isMeleeAttacking)
                {
                    // A đang dùng đòn cận chiến, B chết, A không mất máu
                    StartCoroutine(DieAndRespawnRoutine());
                }
                else
                {
                    // B đâm vào A bình thường
                    pA.TakeDamage(20f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    StartCoroutine(DieAndRespawnRoutine());
                }
            }
        }
    }

    private IEnumerator DieAndRespawnRoutine()
    {
        if (isHit) yield break; // Tránh gọi 2 lần
        isHit = true;

        // Báo kill cho LevelManager và cộng điểm
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegisterKill();
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(100);

        // Ẩn tạm thời thay vì tắt hoàn toàn GameObject
        if (parts.headRenderer != null) parts.headRenderer.enabled = false;
        if (parts.bodyRenderer != null) parts.bodyRenderer.enabled = false;
        if (parts.legRenderer != null) parts.legRenderer.enabled = false;

        yield return new WaitForSeconds(1f);

        // Hiện lại và hồi sinh
        if (parts.headRenderer != null) parts.headRenderer.enabled = true;
        if (parts.bodyRenderer != null) parts.bodyRenderer.enabled = true;
        if (parts.legRenderer != null) parts.legRenderer.enabled = true;

        RespawnAtRightEdge();
        isHit = false;
    }

    private bool isStunned = false;
    public IEnumerator StunRoutine(float duration)
    {
        if (isStunned) yield break;
        isStunned = true;
        
        Color oldHead = parts.headRenderer != null ? parts.headRenderer.color : Color.white;
        Color oldBody = parts.bodyRenderer != null ? parts.bodyRenderer.color : Color.white;
        Color oldLeg = parts.legRenderer != null ? parts.legRenderer.color : Color.white;

        Color stunColor = new Color(0.8f, 0.8f, 0f, 1f); // Màu vàng tối

        if (parts.headRenderer != null) parts.headRenderer.color = stunColor;
        if (parts.bodyRenderer != null) parts.bodyRenderer.color = stunColor;
        if (parts.legRenderer != null) parts.legRenderer.color = stunColor;

        float elapsed = 0f;
        Vector3 originPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Rung lắc nhẹ
            transform.position = originPos + new Vector3(Mathf.Sin(elapsed * 50f) * 0.05f, 0, 0);
            yield return null;
        }

        transform.position = originPos;
        if (parts.headRenderer != null) parts.headRenderer.color = oldHead;
        if (parts.bodyRenderer != null) parts.bodyRenderer.color = oldBody;
        if (parts.legRenderer != null) parts.legRenderer.color = oldLeg;

        isStunned = false;
    }

    /// <summary>
    /// Thực hiện đúng 100% yêu cầu:
    /// - Hành động spawn chỉ áp dụng với biên trái/phải hoặc khi dính chưởng.
    /// - Biên trên (trần) và biên dưới (địa hình diahinh.png) sẽ bị va đánh bật trở lại (Bounce).
    /// </summary>
    private void CheckScreenBordersAndWrap()
    {
        if (Camera.main == null || isHit) return;

        Vector3 pos = transform.position;

        // 1. Chạm vào biên TRÁI màn hình -> Xuất hiện lại ở biên PHẢI với Y ngẫu nhiên
        if (pos.x <= leftBorderX)
        {
            float randY = Random.Range(minSpawnY, maxSpawnY);
            transform.position = new Vector3(rightBorderX, randY, 0f);
            initialY = randY;
            PickNewWaypoint();
            return;
        }

        // 2. Chạm vào biên PHẢI màn hình -> Xuất hiện lại ở biên TRÁI với Y ngẫu nhiên
        if (pos.x >= rightBorderX + 0.5f)
        {
            float randY = Random.Range(minSpawnY, maxSpawnY);
            transform.position = new Vector3(leftBorderX, randY, 0f);
            initialY = randY;
            PickNewWaypoint();
            return;
        }

        // 3. Chạm vào BIÊN TRÊN (Trần) -> Xuất hiện lại ở biên DƯỚI với X ngẫu nhiên
        if (pos.y >= BackgroundManager.CeilingY)
        {
            float randX = Random.Range(leftBorderX, rightBorderX);
            float safeY = BackgroundManager.GroundSurfaceY + 0.5f; // Đặt cao hơn ngưỡng kích hoạt để tránh lặp vô hạn
            transform.position = new Vector3(randX, safeY, 0f);
            initialY = safeY;
            PickNewWaypoint();
            return;
        }

        // 4. Chạm vào BIÊN DƯỚI (Địa hình diahinh.png) -> Xuất hiện lại ở biên TRÊN với X ngẫu nhiên
        if (pos.y <= BackgroundManager.GroundSurfaceY + 0.35f)
        {
            float randX = Random.Range(leftBorderX, rightBorderX);
            float safeY = BackgroundManager.CeilingY - 0.5f; // Đặt thấp hơn ngưỡng kích hoạt để tránh lặp vô hạn
            transform.position = new Vector3(randX, safeY, 0f);
            initialY = safeY;
            PickNewWaypoint();
            return;
        }
    }

    /// <summary>
    /// Khi trúng chiêu Kamehameha C: B dừng lại hoàn toàn, sau khi nổ xong sẽ hồi sinh ở vị trí mới biên phải
    /// </summary>
    public void OnHitBySkill(float stunDuration)
    {
        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(HitAndRespawnRoutine(stunDuration));
        }
    }

    /// <summary>
    public enum BlockedFace { None, Left, Right, Top, Bottom }

    /// <summary>
    /// Bị bệ vật cản bay (FloatingObstacle) đẩy lùi khi va chạm
    /// </summary>
    public void PushBack(Vector3 pushDelta)
    {
        transform.position += pushDelta;
    }

    /// <summary>
    /// Khi bị khối vật cản chặn lại: tự đổi hướng waypoint bay né vật cản
    /// </summary>
    public void OnBlockedByObstacle(FloatingObstacle obs, BlockedFace face = BlockedFace.None)
    {
        if (obs == null) return;

        randomTimer = 0f;

        if (face == BlockedFace.Left)
        {
            // Bị chặn mặt trái vật cản (vật cản ở bên phải B) -> chọn waypoint mới lùi sang trái hoặc lên/xuống
            currentWaypoint.x = Random.Range(leftBorderX + 1.0f, obs.LeftX - 0.8f);
            currentWaypoint.y = Random.Range(minSpawnY, maxSpawnY);
        }
        else if (face == BlockedFace.Right)
        {
            // Bị chặn mặt phải vật cản (vật cản ở bên trái B) -> chọn waypoint mới sang phải
            currentWaypoint.x = Random.Range(obs.RightX + 0.8f, rightBorderX - 1.0f);
            currentWaypoint.y = Random.Range(minSpawnY, maxSpawnY);
        }
        else if (face == BlockedFace.Top)
        {
            // Bị chặn mặt trên vật cản -> bay lên cao hơn
            currentWaypoint.y = Random.Range(obs.TopSurfaceY + 0.6f, maxSpawnY);
        }
        else if (face == BlockedFace.Bottom)
        {
            // Bị chặn mặt dưới vật cản -> bay xuống thấp hơn
            currentWaypoint.y = Random.Range(minSpawnY, obs.BottomSurfaceY - 0.6f);
        }
        else
        {
            // Mặc định lùi theo hướng trôi của bệ
            if (obs.flyDirection > 0)
                currentWaypoint.x = Mathf.Max(currentWaypoint.x, obs.RightX + 1.5f);
            else
                currentWaypoint.x = Mathf.Min(currentWaypoint.x, obs.LeftX - 1.5f);
        }
    }

    /// <summary>
    /// Kiểm tra va chạm khối đặc 4 mặt với toàn bộ các vật cản đang hoạt động:
    /// Ngăn chặn tuyệt đối B không thể đi xuyên qua bất kỳ mặt nào của vật cản!
    /// </summary>
    public Vector3 ResolveObstacleCollisions(Vector3 targetPos, Vector3 oldPos)
    {
        if (FloatingObstacle.ActiveObstacles == null || FloatingObstacle.ActiveObstacles.Count == 0)
        {
            return targetPos;
        }

        float bHalfW = 0.30f;
        float bHalfH = 0.40f;

        for (int i = 0; i < FloatingObstacle.ActiveObstacles.Count; i++)
        {
            FloatingObstacle obs = FloatingObstacle.ActiveObstacles[i];
            if (obs == null || !obs.gameObject.activeInHierarchy) continue;

            float boxLeft   = obs.LeftX;
            float boxRight  = obs.RightX;
            float boxBottom = obs.BottomSurfaceY;
            float boxTop    = obs.TopSurfaceY;

            // Kiểm tra xem targetPos có xâm phạm vào khối hộp AABB của vật cản không
            bool overlapX = (targetPos.x + bHalfW > boxLeft) && (targetPos.x - bHalfW < boxRight);
            bool overlapY = (targetPos.y + bHalfH > boxBottom) && (targetPos.y - bHalfH < boxTop);

            if (overlapX && overlapY)
            {
                // Xác định hướng B tiếp cận dựa trên vị trí cũ oldPos trước khi di chuyển
                bool wasLeft  = (oldPos.x + bHalfW <= boxLeft + 0.05f);
                bool wasRight = (oldPos.x - bHalfW >= boxRight - 0.05f);
                bool wasAbove = (oldPos.y - bHalfH >= boxTop - 0.05f);
                bool wasBelow = (oldPos.y + bHalfH <= boxBottom + 0.05f);

                if (wasLeft)
                {
                    targetPos.x = boxLeft - bHalfW;
                    OnBlockedByObstacle(obs, BlockedFace.Left);
                }
                else if (wasRight)
                {
                    targetPos.x = boxRight + bHalfW;
                    OnBlockedByObstacle(obs, BlockedFace.Right);
                }
                else if (wasAbove)
                {
                    targetPos.y = boxTop + bHalfH;
                    OnBlockedByObstacle(obs, BlockedFace.Top);
                }
                else if (wasBelow)
                {
                    targetPos.y = boxBottom - bHalfH;
                    OnBlockedByObstacle(obs, BlockedFace.Bottom);
                }
                else
                {
                    // Đẩy ra theo chiều cản gần nhất
                    float penLeft   = (targetPos.x + bHalfW) - boxLeft;
                    float penRight  = boxRight - (targetPos.x - bHalfW);
                    float penTop    = boxTop - (targetPos.y - bHalfH);
                    float penBottom = (targetPos.y + bHalfH) - boxBottom;

                    float minPen = Mathf.Min(Mathf.Min(penLeft, penRight), Mathf.Min(penTop, penBottom));
                    if (minPen == penLeft)
                    {
                        targetPos.x = boxLeft - bHalfW;
                        OnBlockedByObstacle(obs, BlockedFace.Left);
                    }
                    else if (minPen == penRight)
                    {
                        targetPos.x = boxRight + bHalfW;
                        OnBlockedByObstacle(obs, BlockedFace.Right);
                    }
                    else if (minPen == penTop)
                    {
                        targetPos.y = boxTop + bHalfH;
                        OnBlockedByObstacle(obs, BlockedFace.Top);
                    }
                    else
                    {
                        targetPos.y = boxBottom - bHalfH;
                        OnBlockedByObstacle(obs, BlockedFace.Bottom);
                    }
                }
            }
        }

        return targetPos;
    }

    private IEnumerator HitAndRespawnRoutine(float stunDuration)
    {
        isHit = true;

        if (parts != null)
        {
            parts.SetPose(headIdle, bodyIdle, legIdle);
        }

        // B dừng lại tại chỗ trong lúc chưởng phát nổ
        yield return new WaitForSeconds(stunDuration);

        // Hồi sinh ở biên phải với Y ngẫu nhiên
        RespawnAtRightEdge();
        isHit = false;
    }

    private void HandleRandomRoam()
    {
        randomTimer += Time.deltaTime;
        float dist = Vector3.Distance(transform.position, currentWaypoint);

        if (randomTimer >= nextChangeTime || dist < 0.35f)
        {
            PickNewWaypoint();
        }

        Vector3 moveDir = (currentWaypoint - transform.position).normalized;
        if (moveDir != Vector3.zero)
        {
            Vector3 oldPos = transform.position;
            Vector3 targetPos = oldPos + moveDir * moveSpeed * Time.deltaTime;
            transform.position = ResolveObstacleCollisions(targetPos, oldPos);

            // Tự động lật mặt theo hướng bay
            if (moveDir.x > 0.05f) parts.Flip(true);
            else if (moveDir.x < -0.05f) parts.Flip(false);
        }
    }

    private void HandleStraightMovement()
    {
        Vector3 oldPos = transform.position;
        Vector3 targetPos = oldPos;
        targetPos.x -= moveSpeed * Time.deltaTime;

        if (verticalWobble)
        {
            targetPos.y = initialY + Mathf.Sin(Time.time * wobbleSpeed) * wobbleMagnitude;
        }

        // Va đập nảy lại trần và địa hình
        if (targetPos.y > BackgroundManager.CeilingY) targetPos.y = BackgroundManager.CeilingY;
        if (targetPos.y < BackgroundManager.GroundSurfaceY + 0.35f) targetPos.y = BackgroundManager.GroundSurfaceY + 0.35f;

        transform.position = ResolveObstacleCollisions(targetPos, oldPos);
        parts.Flip(false);

        if (transform.position.x <= leftBorderX)
        {
            RespawnAtRightEdge();
        }
    }

    private void UpdateMoveAnimation()
    {
        moveAnimTimer += Time.deltaTime;
        if (moveAnimTimer >= moveFrameRate)
        {
            moveAnimTimer = 0f;
            if (bodyMoveFrames != null && legMoveFrames != null && bodyMoveFrames.Length > 0 && legMoveFrames.Length > 0)
            {
                currentMoveFrame = (currentMoveFrame + 1) % Mathf.Min(bodyMoveFrames.Length, legMoveFrames.Length);
                parts.SetPose(headMove, bodyMoveFrames[currentMoveFrame], legMoveFrames[currentMoveFrame]);
            }
        }
    }

    public void PickNewWaypoint()
    {
        randomTimer = 0f;
        nextChangeTime = Random.Range(changeDirIntervalMin, changeDirIntervalMax);

        if (Camera.main != null)
        {
            float roll = Random.value;
            float targetX;
            float targetY = Random.Range(minSpawnY, maxSpawnY);

            if (roll < borderTriggerChance * 0.7f)
            {
                // Bay dứt khoát về phía BIÊN TRÁI để chạm biên trái và xuất hiện lại ở biên phải
                targetX = leftBorderX - 0.5f;
            }
            else if (roll < borderTriggerChance)
            {
                // Bay dứt khoát về phía BIÊN PHẢI để chạm biên phải và xuất hiện lại ở biên trái
                targetX = rightBorderX + 0.5f;
            }
            else
            {
                // Bay lượn tự do quanh màn hình
                float minX = fullScreenRoam ? (leftBorderX + 1.5f) : (leftBorderX + rightBorderX) * 0.5f;
                float maxX = rightBorderX - 1.0f;
                targetX = Random.Range(minX, maxX);
            }

            currentWaypoint = new Vector3(targetX, targetY, 0f);
        }
    }

    private void LateUpdate()
    {
        if (parts != null)
        {
            parts.characterScale = characterScale;
            parts.headHeight = headHeight;
        }
    }

    private void CalculateScreenBounds()
    {
        if (Camera.main == null) return;

        leftBorderX = BackgroundManager.LeftBorderX - 0.2f;
        rightBorderX = BackgroundManager.RightBorderX + 0.2f;
        minSpawnY = BackgroundManager.GroundSurfaceY + 0.5f;
        maxSpawnY = BackgroundManager.CeilingY - 0.5f;
    }

    public void RespawnAtRightEdge()
    {
        if (parts != null)
        {
            if (parts.headRenderer != null) parts.headRenderer.enabled = true;
            if (parts.bodyRenderer != null) parts.bodyRenderer.enabled = true;
            if (parts.legRenderer != null) parts.legRenderer.enabled = true;
        }

        float randomY = Random.Range(minSpawnY, maxSpawnY);
        transform.position = new Vector3(rightBorderX, randomY, 0f);
        initialY = randomY;
        PickNewWaypoint();
    }

#if UNITY_EDITOR
    [ContextMenu("Tự Động Gán Toàn Bộ Sprite (1-Click)")]
    public void AutoAssignSprites()
    {
        string p = "Assets/Sprites/";
        headIdle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small91.png");
        headMove = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small92.png");

        bodyIdle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "1.png");
        legIdle  = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "22.png");

        bodyMoveFrames = new Sprite[]
        {
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "2.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "3.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "4.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "5.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "6.png")
        };
        legMoveFrames = new Sprite[]
        {
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "23.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "24.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "25.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "26.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p + "27.png")
        };

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("===> ĐÃ TỰ ĐỘNG GÁN TOÀN BỘ SPRITE CHO ENEMYB THÀNH CÔNG! <===");
    }
#endif
}

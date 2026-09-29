using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Điều khiển Đối tượng A (Nhân vật người chơi):
/// - Tự động xuất hiện ở giữa biên trái màn hình.
/// - Di chuyển 4 hướng (Lên/Xuống/Trái/Phải), lật hướng FlipX.
/// - Tốc độ nhanh x2.
/// - Hào quang được nâng cao x2 (0.95) bao trùm cả đầu và thân người khi tụ lực.
/// </summary>
[RequireComponent(typeof(CharacterParts))]
public class PlayerA : MonoBehaviour
{
    [Header("Cấu hình di chuyển (Đã tăng tốc x2)")]
    public float moveSpeed = 8f;
    public float gravity = 25f;

    [Header("3 Mức Nhảy (Dựa vào số lần bấm: 1, 2, 3)")]
    [Tooltip("Mức 1 (Bấm 1 lần): Nhảy cơ bản")]
    public float jumpVelocityLevel1 = 8.0f;
    [Tooltip("Mức 2 (Bấm lần 2 - Double Jump): Nhảy nấc 2")]
    public float jumpVelocityLevel2 = 10.5f;
    [Tooltip("Mức 3 (Bấm lần 3 - Triple Jump): Nhảy nấc 3 cao nhất")]
    public float jumpVelocityLevel3 = 13.0f;

    [Header("Kích Thước Toàn Bộ (Đã tăng x2 lên: 0.2)")]
    [Range(0.05f, 1.5f)]
    [Tooltip("Kéo thanh trượt để thu nhỏ / phóng to A (Mặc định 0.2)")]
    public float characterScale = 0.2f;

    [Header("Độ Cao Đầu của A (Goku - Đã hạ thấp 2px: 1.5375)")]
    [Range(0.5f, 3.0f)]
    [Tooltip("Kéo thanh trượt này để đầu Goku cao lên hoặc thấp xuống theo ý thích (Mặc định: 1.5375)")]
    public float headHeight = 1.5375f;

    [Header("Cản Biên Trái Phải Màn Hình")]
    [Tooltip("Khoảng cách giữ nhân vật A bên trong 2 biên trái và phải màn hình (Mặc định: 0.28)")]
    public float borderPaddingX = 0.28f;

    [Header("Độ Cao Chân của A (Mặc định: -0.6)")]
    [Range(-2.0f, 0.5f)]
    [Tooltip("Kéo thanh trượt này để chân của Goku cao lên hoặc thấp xuống")]
    public float legHeight = -0.6f;

    [Header("Căn Chỉnh Khi Nhảy (Tránh Bị Trùng Đè Lên Nhau)")]
    [Tooltip("Bật xem trước tư thế Nhảy trong Scene view để căn chỉnh trực tiếp")]
    public bool previewJumpPose = false;

    [Range(-1.0f, 1.0f)]
    [Tooltip("Độ bù vị trí đầu khi nhảy (Mặc định: -0.20 để khớp khít cổ áo khi thân nâng cao)")]
    public float jumpHeadOffset = -0.20f;

    [Range(0f, 1.5f)]
    [Tooltip("Độ nâng thân áo khi nhảy để không bị thấp/trùng với chân (Mặc định: 0.50)")]
    public float jumpBodyOffset = 0.50f;

    [Range(0f, 1.5f)]
    [Tooltip("Độ hạ chân khi nhảy (Mặc định: 0.45 khi nhảy, 0.55 khi rơi)")]
    public float jumpLegOffset = 0.45f;

    [Header("Sprite Trạng Thái Đứng Yên (Idle: 17 - 1 - 22)")]
    public Sprite headIdle;
    public Sprite bodyIdle;
    public Sprite legIdle;

    [Header("Sprite Di Chuyển (Đầu: 18, Thân: 2-6, Chân: 23-27)")]
    public Sprite headMove;
    public Sprite[] bodyMoveFrames;
    public Sprite[] legMoveFrames;
    public float moveFrameRate = 0.06f;

    [Header("Sprite Nhảy & Rơi")]
    public Sprite bodyJumpPrep;
    public Sprite legJumpPrep;
    public Sprite bodyJump;
    public Sprite legJump;
    public Sprite bodyFall;
    public Sprite legFall;

    [Header("Sprite Tụ Lực & Bắn (Đầu: 18, Chân: 30)")]
    public Sprite bodyChargePrep;
    public Sprite bodyCharging;
    public Sprite bodyFire;
    public Sprite legCharge;

    [Header("Căn Chỉnh Đầu Khi Tụ Lực Chưởng")]
    [Range(0f, 0.6f)]
    [Tooltip("Độ hạ thấp đầu khi tụ lực chưởng (Mặc định: 0.20 để đầu khom thấp khớp với vai áo)")]
    public float chargeHeadOffset = 0.20f;

    [Tooltip("Bật xem trước tư thế Tụ Lực Chưởng trong Scene view để kéo thanh trượt căn chỉnh trực tiếp")]
    public bool previewChargePose = false;

    [Header("Hiệu Ứng Hào Quang (Small982 đến Small985)")]
    public SpriteRenderer auraRenderer;
    public Sprite[] auraFrames;
    public float auraFrameRate = 0.06f;

    [Range(0f, 6f)]
    [Tooltip("Độ cao hào quang: Mặc định 2.5 để chân hào quang trùng chân nhân vật, ngọn lửa bao trùm đỉnh đầu")]
    public float auraHeight = 2.5f;

    [Range(0.1f, 2f)]
    [Tooltip("Tỉ lệ to nhỏ của hào quang (Mặc định 0.45 cho bộ Small982-985)")]
    public float auraScale = 0.45f;

    [Tooltip("Vị trí hào quang tương đối")]
    public Vector3 auraOffset = new Vector3(0f, 2.5f, 0f);

    [Tooltip("Bật xem trước hào quang ngay trong Scene view (không cần bấm Play)")]
    public bool previewAuraInScene = false;

    [Header("Nút Bấm Tung Chưởng (Attack Button: Small540)")]
    [Tooltip("Sprite nút tung chưởng (Small540.png)")]
    public Sprite attackButtonSprite;

    [Tooltip("Nút UI tung chưởng trên màn hình")]
    public UnityEngine.UI.Button attackButton;

    [Tooltip("Kích thước nút bấm tung chưởng trên màn hình (Mặc định: 150)")]
    public float attackButtonSize = 150f;

    [Tooltip("Khoảng cách từ góc phải-dưới màn hình (X: cách lề phải, Y: cách lề dưới)")]
    public Vector2 attackButtonMargin = new Vector2(130f, 130f);

    [Header("4 Nút Di Chuyển (D-Pad: Small2322.png)")]
    [Tooltip("Sprite cho 4 nút di chuyển (Small2322.png)")]
    public Sprite moveButtonSprite;

    [Tooltip("Kích thước mỗi nút di chuyển (Mặc định: 135)")]
    public float moveButtonSize = 135f;

    [Tooltip("Khoảng cách giữa tâm cụm D-Pad và mỗi nút (Mặc định: 110)")]
    public float moveButtonSpacing = 110f;

    [Tooltip("Tọa độ cụm nút di chuyển từ góc dưới-trái màn hình (X: cách lề trái, Y: cách lề dưới)")]
    public Vector2 moveDpadPosition = new Vector2(210f, 210f);

    private bool isHoldingLeft = false;
    private bool isHoldingRight = false;
    private bool isHoldingDown = false;

    [Header("Quả Cầu Tụ Lực (Small48 - Small51)")]
    public SpriteRenderer chargingBallRenderer;
    public Sprite[] chargingBallFrames;
    public Vector3 ballOffset = new Vector3(0.45f, 0.25f, 0f);

    [Header("Nhân Bản Kẻ Địch B (Số Lượng Vegeta)")]
    [Range(1, 10)]
    [Tooltip("Số lượng nhân vật B cùng xuất hiện trên màn hình (Mặc định: 3)")]
    public int enemyBCount = 3;

    [Tooltip("Prefab của Enemy B (hỗ trợ sinh runtime APK)")]
    public GameObject enemyBPrefab;

    [Tooltip("Sprite đầu đứng của B (Small91.png)")]
    public Sprite enemyBHeadIdle;

    [Tooltip("Sprite đầu di chuyển của B (Small92.png)")]
    public Sprite enemyBHeadMove;

    [Header("Stats")]
    public float maxHp = 100f;
    public float currentHp = 100f;
    public float maxKi = 100f;
    public float currentKi = 100f;

    [Header("Defense States")]
    public bool hasShield = false;
    public bool isMeleeAttacking = false;
    
    // GameObject chứa hình ảnh khiên
    private GameObject shieldVisual;

    [Header("Mana & Life States")]
    public float kiRegenRate = 5f; // Hồi 5 Ki mỗi giây
    public bool isDead = false;

    [Header("Prefabs & Sprites")]
    public GameObject projectileCPrefab;

    [Header("Tấn công & Nhắm mục tiêu")]
    public Transform firePoint;
    public Transform targetEnemyB;

    [Header("Input UI")]
    private bool isChargingOrFiring = false;
    private bool isJumping = false;
    private float verticalVelocity = 0f;
    private float groundY = 0f;
    private int currentJumpCount = 0;
    private FloatingObstacle currentPlatform = null;
    private float moveAnimTimer = 0f;
    private int currentMoveFrame = 0;
    private CharacterParts parts;

    private void Awake()
    {
        parts = GetComponent<CharacterParts>();
        if (moveSpeed < 6f) moveSpeed = 8f;
        if (jumpVelocityLevel1 < 6f) jumpVelocityLevel1 = 8.0f;
        if (jumpVelocityLevel2 < 8f) jumpVelocityLevel2 = 10.5f;
        if (jumpVelocityLevel3 < 10f) jumpVelocityLevel3 = 13.0f;
        if (moveFrameRate > 0.08f) moveFrameRate = 0.06f;

        // Tự động tăng x2 lên 0.2 nếu đang ở scale cũ 0.1
        if (characterScale <= 0.01f || Mathf.Approximately(characterScale, 0.1f)) characterScale = 0.2f;
        if (parts != null)
        {
            parts.characterScale = characterScale;
            if (headHeight < 1.4f) headHeight = 1.6f;
            parts.headHeight = headHeight;
            parts.legHeight = legHeight;
        }

        // Tự động chuyển sang thông số tối ưu cho hào quang Small982-985
        if (Mathf.Approximately(auraHeight, 3.0f) || auraHeight <= 0.1f) auraHeight = 2.5f;
        if (auraScale > 0.8f || auraScale <= 0.05f) auraScale = 0.45f;
        auraOffset.y = auraHeight;

        // Tự động nâng thân áo và hạ chân theo thông số tối ưu mới nhất
        if (Mathf.Approximately(jumpBodyOffset, 0.35f) || jumpBodyOffset <= 0.05f) jumpBodyOffset = 0.50f;
        if (Mathf.Approximately(jumpHeadOffset, -0.35f) || jumpHeadOffset <= -0.5f) jumpHeadOffset = -0.20f;
        if (Mathf.Approximately(jumpLegOffset, 0.25f) || jumpLegOffset <= 0.05f) jumpLegOffset = 0.45f;

#if UNITY_EDITOR
        if (headIdle == null || bodyIdle == null || legIdle == null || auraFrames == null || auraFrames.Length == 0 || (auraFrames[0] != null && auraFrames[0].name.Contains("Small35")) || moveButtonSprite == null)
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
            parts.legHeight = legHeight;

            if (previewJumpPose)
            {
                parts.extraHeadOffset = new Vector3(0f, jumpHeadOffset, 0f);
                parts.extraBodyOffset = new Vector3(0f, jumpBodyOffset, 0f);
                parts.extraLegOffset = new Vector3(0f, -jumpLegOffset, 0f);
                parts.SyncAndApplyAll();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && parts != null && previewJumpPose && !Application.isPlaying)
                    {
                        parts.SetPose(headMove, bodyJump, legJump);
                    }
                };
#endif
            }
            else if (previewChargePose)
            {
                parts.extraHeadOffset = new Vector3(0f, -chargeHeadOffset, 0f);
                parts.extraBodyOffset = Vector3.zero;
                parts.extraLegOffset = Vector3.zero;
                parts.SyncAndApplyAll();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && parts != null && previewChargePose && !Application.isPlaying)
                    {
                        parts.SetPose(headMove, bodyCharging, legCharge);
                    }
                };
#endif
            }
            else if (!Application.isPlaying)
            {
                parts.extraHeadOffset = Vector3.zero;
                parts.extraBodyOffset = Vector3.zero;
                parts.extraLegOffset = Vector3.zero;
                parts.SyncAndApplyAll();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && parts != null && !previewJumpPose && !previewChargePose && !Application.isPlaying)
                    {
                        SetIdlePose();
                    }
                };
#endif
            }
        }

        auraOffset.y = auraHeight;
        if (auraRenderer != null)
        {
            auraRenderer.transform.localPosition = auraOffset;
            auraRenderer.transform.localScale = new Vector3(auraScale, auraScale, 1f);
            if (!Application.isPlaying)
            {
                auraRenderer.gameObject.SetActive(previewAuraInScene);
                if (previewAuraInScene && auraFrames != null && auraFrames.Length > 0)
                {
                    auraRenderer.sprite = auraFrames[0];
                }
            }
        }
    }

    private void Start()
    {
        if (characterScale <= 0.01f || Mathf.Approximately(characterScale, 0.1f)) characterScale = 0.2f;
        if (headHeight < 1.3f || Mathf.Approximately(headHeight, 1.6f)) headHeight = 1.5375f;
        if (parts != null)
        {
            parts.characterScale = characterScale;
            parts.headHeight = headHeight;
            parts.legHeight = legHeight;
        }

        if (Mathf.Approximately(auraHeight, 3.0f) || auraHeight <= 0.1f) auraHeight = 2.5f;
        if (auraScale > 0.8f || auraScale <= 0.05f) auraScale = 0.45f;
        auraOffset.y = auraHeight;

        if (Mathf.Approximately(jumpBodyOffset, 0.35f) || jumpBodyOffset <= 0.05f) jumpBodyOffset = 0.50f;
        if (Mathf.Approximately(jumpHeadOffset, -0.35f) || jumpHeadOffset <= -0.5f) jumpHeadOffset = -0.20f;
        if (Mathf.Approximately(jumpLegOffset, 0.25f) || jumpLegOffset <= 0.05f) jumpLegOffset = 0.45f;

        // Tự động nâng cấp kích thước D-Pad và Attack Button lên chuẩn di động thoải mái
        if (moveButtonSize < 100f) moveButtonSize = 135f;
        if (moveButtonSpacing < 80f) moveButtonSpacing = 110f;
        if (moveDpadPosition.x < 150f) moveDpadPosition = new Vector2(210f, 210f);
        if (attackButtonSize < 120f) attackButtonSize = 150f;
        if (attackButtonMargin.x < 100f) attackButtonMargin = new Vector2(130f, 130f);

#if UNITY_EDITOR
        if (headIdle == null || bodyIdle == null || legIdle == null || auraFrames == null || auraFrames.Length == 0 || (auraFrames[0] != null && auraFrames[0].name.Contains("Small35")))
        {
            AutoAssignSprites();
        }

        if (projectileCPrefab == null)
        {
            string[] guids = AssetDatabase.FindAssets("Projectile_C t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                projectileCPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
#endif

        // Khởi tạo và đảm bảo số lượng Enemy B ở CẢ Editor và Runtime (APK độc lập)
        EnsureEnemyBCount();
        targetEnemyB = GetNearestEnemyB();

        if (Camera.main != null)
        {
            Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(0.08f, 0.5f, 10f));
            spawnPos.z = 0f;
            float currentGround = BackgroundManager.GroundY;
            // Cho A xuất hiện ở chính giữa biên trái, giữ nguyên trọng lực để tự rơi xuống.
            transform.position = spawnPos;
            groundY = currentGround;
        }

        if (auraRenderer != null)
        {
            auraRenderer.transform.localPosition = auraOffset;
            auraRenderer.gameObject.SetActive(false);
        }
        if (chargingBallRenderer != null)
        {
            chargingBallRenderer.transform.localPosition = ballOffset;
            chargingBallRenderer.gameObject.SetActive(false);
        }

        parts.Flip(true);
        SetIdlePose();
        EnsureAttackButton();
        
        // Kích hoạt trạng thái nhảy để A tự động chịu tác động của trọng lực và rơi xuống đất từ vị trí spawn trên không
        isJumping = true;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHP(currentHp, maxHp);
            HUDManager.Instance.UpdateKi(currentKi, maxKi);
        }
    }

    private float invulnerableTimer = 0f;
    private Coroutine blinkCoroutine = null;

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        if (hasShield) return; // Không mất máu nếu có khiên
        if (invulnerableTimer > 0f) return; // Bất tử tạm thời sau khi bị đánh

        invulnerableTimer = 1.0f; // 1 giây miễn nhiễm sát thương sau khi nhận đòn
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(DamageBlinkRoutine());

        currentHp = Mathf.Clamp(currentHp - amount, 0, maxHp);
        if (HUDManager.Instance != null) HUDManager.Instance.UpdateHP(currentHp, maxHp);

        if (currentHp <= 0 && !isDead)
        {
            isDead = true;
            StopAllCoroutines();
            isChargingOrFiring = false;
            isMeleeAttacking = false;
            if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
            if (chargingBallRenderer != null) chargingBallRenderer.gameObject.SetActive(false);
            if (shieldVisual != null) shieldVisual.SetActive(false);
            if (parts != null && parts.bodyRenderer != null) parts.bodyRenderer.color = Color.white;

            // Kích hoạt Game Over qua GameManager (hiển thị GameOverUI + dừng game)
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
            else
            {
                // Fallback nếu chưa có GameManager
                Time.timeScale = 0f;
                if (HUDManager.Instance != null) HUDManager.Instance.ShowGameOver();
            }
            Debug.Log("Player A is dead! GAME OVER.");
        }
    }

    private IEnumerator DamageBlinkRoutine()
    {
        float timer = 0f;
        bool visible = true;
        while (timer < 1.0f)
        {
            visible = !visible;
            if (parts != null)
            {
                Color c = visible ? Color.white : new Color(1f, 0.4f, 0.4f, 0.4f);
                if (parts.headRenderer != null) parts.headRenderer.color = c;
                if (parts.bodyRenderer != null) parts.bodyRenderer.color = c;
                if (parts.legRenderer != null) parts.legRenderer.color = c;
            }
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }
        if (parts != null)
        {
            if (parts.headRenderer != null) parts.headRenderer.color = Color.white;
            if (parts.bodyRenderer != null) parts.bodyRenderer.color = Color.white;
            if (parts.legRenderer != null) parts.legRenderer.color = Color.white;
        }
        blinkCoroutine = null;
    }

    public void ResetPlayerState(float hp = -1f, float ki = -1f)
    {
        StopAllCoroutines();
        isDead = false;
        hasShield = false;
        isChargingOrFiring = false;
        isMeleeAttacking = false;
        isHoldingLeft = false;
        isHoldingRight = false;
        isHoldingDown = false;
        isJumping = false;
        currentJumpCount = 0;
        verticalVelocity = 0f;
        currentPlatform = null;
        invulnerableTimer = 0f;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // Đặt Goku đứng vững chãi trên sàn đất ngay lập tức
        float currentGround = BackgroundManager.GroundY;
        Vector3 pos = transform.position;
        pos.y = currentGround;
        transform.position = pos;
        groundY = currentGround;

        currentHp = hp > 0 ? hp : maxHp;
        currentKi = ki >= 0 ? ki : maxKi;

        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
        if (chargingBallRenderer != null) chargingBallRenderer.gameObject.SetActive(false);
        if (shieldVisual != null) shieldVisual.SetActive(false);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHP(currentHp, maxHp);
            HUDManager.Instance.UpdateKi(currentKi, maxKi);
        }
        if (parts != null)
        {
            parts.extraHeadOffset = Vector3.zero;
            parts.extraBodyOffset = Vector3.zero;
            parts.extraLegOffset = Vector3.zero;
            parts.SyncAndApplyAll();
            parts.SetPose(headIdle, bodyIdle, legIdle);
            if (parts.headRenderer != null) parts.headRenderer.color = Color.white;
            if (parts.bodyRenderer != null) parts.bodyRenderer.color = Color.white;
            if (parts.legRenderer != null) parts.legRenderer.color = Color.white;
        }
    }


    public void RestoreHp(float amount)
    {
        currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);
        if (HUDManager.Instance != null) HUDManager.Instance.UpdateHP(currentHp, maxHp);
    }

    public bool UseKi(float amount)
    {
        if (currentKi >= amount)
        {
            currentKi -= amount;
            if (HUDManager.Instance != null) HUDManager.Instance.UpdateKi(currentKi, maxKi);
            return true;
        }
        return false;
    }

    public void RestoreKi(float amount)
    {
        currentKi = Mathf.Clamp(currentKi + amount, 0, maxKi);
        if (HUDManager.Instance != null) HUDManager.Instance.UpdateKi(currentKi, maxKi);
    }

    private void Update()
    {
        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
        }

        if (isDead) return;

        // Luôn cập nhật nhảy và trọng lực liên tục mọi frame, bảo đảm nhân vật không bao giờ kẹt lơ lửng trên không
        HandleJump();

        if (isChargingOrFiring) return;

        HandleAttackInput();
        HandleMovement();
    }

    private void HandleAttackInput()
    {
        if (isDead || isChargingOrFiring) return;

        // 1. Phím tắt bàn phím tiện lợi trên PC / Editor
        if (Input.GetKeyDown(KeyCode.Alpha1)) { Attack1_Kamehameha(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { Attack2_KiBlast(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { Attack3_Melee(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { Defense1_Shield(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { Defense2_SolarFlare(); return; }
        
        // Phím cũ (J, K, Enter) gọi đòn cơ bản
        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Return))
        {
            Attack1_Kamehameha();
            return;
        }

        // 2. Xử lý cảm ứng trên thiết bị di động (Mobile Touch)
        if (Input.touchSupported && Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    if (IsPointerOverUI(t.fingerId)) continue;
                    if (t.position.x < Screen.width * 0.5f) continue;

                    Attack1_Kamehameha();
                    return;
                }
            }
        }
        else
        {
            // 3. Chuột trên PC / Editor (Click chuột trái ngoài UI)
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI(-1)) return;
                Attack1_Kamehameha();
                return;
            }
        }
    }

    /// <summary>
    /// Kiểm tra con trỏ hoặc ngón tay cảm ứng có đang đè lên UI hay không (Hỗ trợ đa điểm fingerId)
    /// </summary>
    public static bool IsPointerOverUI(int fingerId = -1)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;

        if (fingerId >= 0)
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(fingerId);
        }

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }
        }

        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    private void LateUpdate()
    {
        if (parts != null)
        {
            parts.headHeight = headHeight;
            parts.legHeight = legHeight;
            parts.characterScale = characterScale;

            if (isChargingOrFiring || (!Application.isPlaying && previewChargePose))
            {
                parts.extraHeadOffset = new Vector3(0f, -chargeHeadOffset, 0f);
                parts.extraBodyOffset = Vector3.zero;
                parts.extraLegOffset = Vector3.zero;
            }
            else if (isJumping || (!Application.isPlaying && previewJumpPose))
            {
                parts.extraHeadOffset = new Vector3(0f, jumpHeadOffset, 0f);
                parts.extraBodyOffset = new Vector3(0f, jumpBodyOffset, 0f);
                float curLeg = (verticalVelocity < 0f && Application.isPlaying) ? (jumpLegOffset + 0.10f) : jumpLegOffset;
                parts.extraLegOffset = new Vector3(0f, -curLeg, 0f);
            }
            else
            {
                parts.extraHeadOffset = Vector3.zero;
                parts.extraBodyOffset = Vector3.zero;
                parts.extraLegOffset = Vector3.zero;
            }

            parts.SyncAndApplyAll();
        }

        auraOffset.y = auraHeight;
        if (auraRenderer != null)
        {
            auraRenderer.transform.localPosition = auraOffset;
            auraRenderer.transform.localScale = new Vector3(auraScale, auraScale, 1f);
        }
    }

    private void HandleMovement()
    {
        if (isDead) return;

        float currentGround = BackgroundManager.GroundY;
        groundY = currentGround;

        float inputX = Input.GetAxisRaw("Horizontal");
        if (isHoldingLeft) inputX = -1f;
        else if (isHoldingRight) inputX = 1f;

        float inputY = Input.GetAxisRaw("Vertical");
        if (isHoldingDown) inputY = -1f;

        // Nếu người chơi đang đứng trên bệ vật cản
        if (!isJumping && currentPlatform != null)
        {
            // Nếu bệ bị hủy hoặc không còn hoạt động
            if (!currentPlatform.gameObject.activeInHierarchy)
            {
                currentPlatform = null;
                isJumping = true;
                verticalVelocity = 0f;
            }
            else
            {
                // Kiểm tra xem A có bước hụt chân khỏi mép bệ không
                if (transform.position.x < currentPlatform.LeftX - 0.25f ||
                    transform.position.x > currentPlatform.RightX + 0.25f)
                {
                    currentPlatform = null;
                    isJumping = true;
                    verticalVelocity = 0f;
                }
                else
                {
                    // Di chuyển cùng bệ theo vận tốc trôi ngang của bệ
                    transform.position += currentPlatform.Velocity * Time.deltaTime;
                    // Giữ cao độ luôn khớp mặt bệ
                    Vector3 p = transform.position;
                    p.y = currentPlatform.TopSurfaceY + 0.27f;
                    transform.position = p;
                }
            }
        }

        // Khóa di chuyển xuống (S / Down) khi đang đứng trên sàn hoặc trên bệ (vì là vật cản cứng)
        if (!isJumping && inputY < 0f)
        {
            inputY = 0f;
        }

        Vector3 moveDir = new Vector3(inputX, 0f, 0f);
        if (moveDir != Vector3.zero)
        {
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            if (inputX > 0) parts.Flip(true);
            else if (inputX < 0) parts.Flip(false);

            if (!isJumping)
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
        }
        else if (!isJumping)
        {
            SetIdlePose();
            currentMoveFrame = 0;
        }

        // Đảm bảo không bao giờ lọt xuống dưới sàn chính diahinh hoặc vượt trần
        Vector3 curPos = transform.position;
        if (!isJumping && currentPlatform == null && curPos.y < groundY)
        {
            curPos.y = groundY;
            transform.position = curPos;
        }

        // Cản tường 2 bên của khối vật cản khi A đang trên không
        if (isJumping && FloatingObstacle.ActiveObstacles != null)
        {
            for (int i = 0; i < FloatingObstacle.ActiveObstacles.Count; i++)
            {
                FloatingObstacle obs = FloatingObstacle.ActiveObstacles[i];
                if (obs == null || !obs.gameObject.activeInHierarchy) continue;

                if (curPos.y + 0.30f >= obs.BottomSurfaceY && curPos.y - 0.20f <= obs.TopSurfaceY)
                {
                    if (curPos.x > obs.LeftX - 0.25f && curPos.x < obs.transform.position.x)
                    {
                        curPos.x = obs.LeftX - 0.25f;
                        transform.position = curPos;
                    }
                    else if (curPos.x < obs.RightX + 0.25f && curPos.x > obs.transform.position.x)
                    {
                        curPos.x = obs.RightX + 0.25f;
                        transform.position = curPos;
                    }
                }
            }
        }

        if (curPos.y > BackgroundManager.CeilingY)
        {
            curPos.y = BackgroundManager.CeilingY;
            transform.position = curPos;
        }

        // Cản tuyệt đối bởi 2 biên trái và phải màn hình
        float leftBorder = BackgroundManager.LeftBorderX;
        float rightBorder = BackgroundManager.RightBorderX;
        if (Mathf.Approximately(leftBorder, rightBorder) && Camera.main != null)
        {
            float camH = Camera.main.orthographicSize * 2f;
            float camW = camH * Camera.main.aspect;
            leftBorder = Camera.main.transform.position.x - (camW / 2f);
            rightBorder = Camera.main.transform.position.x + (camW / 2f);
        }

        float minX = leftBorder + borderPaddingX;
        float maxX = rightBorder - borderPaddingX;

        if (curPos.x < minX)
        {
            curPos.x = minX;
            transform.position = curPos;
        }
        else if (curPos.x > maxX)
        {
            curPos.x = maxX;
            transform.position = curPos;
        }
    }

    private void HandleJump()
    {
        float currentGround = BackgroundManager.GroundY;

        // Tự động kích hoạt rơi nếu nhân vật đang ở trên không mà không có bệ đỡ
        if (!isJumping && currentPlatform == null && transform.position.y > currentGround + 0.05f)
        {
            isJumping = true;
        }

        bool jumpKeyPressed = Input.GetKeyDown(KeyCode.Space) ||
                              Input.GetKeyDown(KeyCode.W) ||
                              Input.GetKeyDown(KeyCode.UpArrow);

        if (jumpKeyPressed && !isChargingOrFiring)
        {
            TryJump();
        }

        if (isJumping)
        {
            verticalVelocity -= gravity * Time.deltaTime;
            transform.position += new Vector3(0f, verticalVelocity * Time.deltaTime, 0f);

            // Giới hạn trần trên khi nhảy
            if (transform.position.y >= BackgroundManager.CeilingY)
            {
                Vector3 p = transform.position;
                p.y = BackgroundManager.CeilingY;
                transform.position = p;
                if (verticalVelocity > 0f) verticalVelocity = 0f;
            }

            if (verticalVelocity > 0f)
            {
                // Kiểm tra va chạm đáy bệ (cộp đầu vào đáy khối đặc khi nhảy từ dưới lên)
                CheckHeadHitObstacleBottom();
            }
            else if (verticalVelocity < 0f)
            {
                if (parts != null && !isChargingOrFiring) parts.SetPose(headMove, bodyFall, legFall);

                // Kiểm tra đáp xuống các bệ vật cản bay (FloatingObstacle)
                CheckLandOnObstacle();
            }

            // Kiểm tra đáp xuống sàn địa hình chính (diahinh)
            if (transform.position.y <= currentGround)
            {
                Vector3 pos = transform.position;
                pos.y = currentGround;
                transform.position = pos;
                LandOnSurface(null);
            }
        }
    }

    private void CheckHeadHitObstacleBottom()
    {
        if (FloatingObstacle.ActiveObstacles == null || FloatingObstacle.ActiveObstacles.Count == 0) return;

        float playerX = transform.position.x;
        float playerHeadY = transform.position.y + 0.35f;

        for (int i = 0; i < FloatingObstacle.ActiveObstacles.Count; i++)
        {
            FloatingObstacle obs = FloatingObstacle.ActiveObstacles[i];
            if (obs == null || !obs.gameObject.activeInHierarchy) continue;

            if (playerX >= obs.LeftX - 0.2f && playerX <= obs.RightX + 0.2f)
            {
                // Nếu đầu A đụng vào đáy khối đặc
                if (playerHeadY >= obs.BottomSurfaceY && transform.position.y < obs.BottomSurfaceY)
                {
                    Vector3 p = transform.position;
                    p.y = obs.BottomSurfaceY - 0.36f;
                    transform.position = p;
                    verticalVelocity = 0f;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Nhảy 3 mức dựa theo số lần bấm:
    /// - Lần 1: Nhảy mức 1 (cơ bản)
    /// - Lần 2: Nhảy mức 2 (Double Jump trên không)
    /// - Lần 3: Nhảy mức 3 (Triple Jump cao nhất)
    /// </summary>
    public void TryJump()
    {
        if (isDead || isChargingOrFiring) return;
        if (currentJumpCount >= 3) return; // Tối đa 3 mức nhảy

        currentJumpCount++;
        isJumping = true;
        currentPlatform = null; // Bật rời khỏi bệ

        float chosenVelocity = jumpVelocityLevel1;
        if (currentJumpCount == 1) chosenVelocity = jumpVelocityLevel1;
        else if (currentJumpCount == 2) chosenVelocity = jumpVelocityLevel2;
        else if (currentJumpCount >= 3) chosenVelocity = jumpVelocityLevel3;

        verticalVelocity = chosenVelocity; // Áp dụng ngay tức thì không chờ đợi!

        if (parts != null)
        {
            parts.extraHeadOffset = new Vector3(0f, jumpHeadOffset, 0f);
            parts.extraBodyOffset = new Vector3(0f, jumpBodyOffset, 0f);
            parts.extraLegOffset = new Vector3(0f, -jumpLegOffset, 0f);
            parts.SyncAndApplyAll();
            parts.SetPose(headMove, bodyJump, legJump);
        }
    }

    private void CheckLandOnObstacle()
    {
        if (FloatingObstacle.ActiveObstacles == null || FloatingObstacle.ActiveObstacles.Count == 0) return;

        float playerX = transform.position.x;
        float playerY = transform.position.y;

        for (int i = 0; i < FloatingObstacle.ActiveObstacles.Count; i++)
        {
            FloatingObstacle obs = FloatingObstacle.ActiveObstacles[i];
            if (obs == null || !obs.gameObject.activeInHierarchy) continue;

            float obsTop = obs.TopSurfaceY;
            float targetStandY = obsTop + 0.27f;

            // Kiểm tra xem A có nằm trong khoảng ngang của bệ không
            if (playerX >= obs.LeftX - 0.2f && playerX <= obs.RightX + 0.2f)
            {
                // Khi đang rơi xuống gần chạm bề mặt trên bệ (trong khoảng 0.25f)
                if (playerY <= targetStandY + 0.25f && playerY >= targetStandY - 0.25f)
                {
                    Vector3 pos = transform.position;
                    pos.y = targetStandY;
                    transform.position = pos;
                    LandOnSurface(obs);
                    return;
                }
            }
        }
    }

    private void LandOnSurface(FloatingObstacle platform)
    {
        currentPlatform = platform;
        isJumping = false;
        currentJumpCount = 0; // Reset số lần nhảy về 0
        verticalVelocity = 0f;

        if (parts != null)
        {
            parts.extraHeadOffset = Vector3.zero;
            parts.extraBodyOffset = Vector3.zero;
            parts.extraLegOffset = Vector3.zero;
            parts.SyncAndApplyAll();
        }
        SetIdlePose();
    }

    public void Attack1_Kamehameha()
    {
        if (!isChargingOrFiring && UseKi(20f))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
            StartCoroutine(ChargeAndFireRoutine());
        }
    }

    public void SetIdlePose()
    {
        parts.SetPose(headIdle, bodyIdle, legIdle);
    }

    private IEnumerator ChargeAndFireRoutine()
    {
        isChargingOrFiring = true;

        parts.SetPose(headMove, bodyChargePrep, legCharge);
        yield return new WaitForSeconds(0.06f);

        parts.SetPose(headMove, bodyCharging, legCharge);

        auraOffset.y = auraHeight;
        if (auraRenderer != null)
        {
            auraRenderer.transform.localPosition = auraOffset;
            auraRenderer.transform.localScale = new Vector3(auraScale, auraScale, 1f);
            auraRenderer.gameObject.SetActive(true);
        }
        if (chargingBallRenderer != null)
        {
            chargingBallRenderer.transform.localPosition = ballOffset;
            chargingBallRenderer.gameObject.SetActive(true);
        }

        float chargeDuration = 0.4f;
        float elapsed = 0f;
        int auraIdx = 0;
        int ballIdx = 0;
        float fxTimer = 0f;

        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            fxTimer += Time.deltaTime;

            if (fxTimer >= auraFrameRate)
            {
                fxTimer = 0f;
                if (auraFrames != null && auraFrames.Length > 0 && auraRenderer != null)
                {
                    auraRenderer.sprite = auraFrames[auraIdx % auraFrames.Length];
                    auraIdx++;
                }
                if (chargingBallFrames != null && chargingBallFrames.Length > 0 && chargingBallRenderer != null)
                {
                    chargingBallRenderer.sprite = chargingBallFrames[ballIdx % chargingBallFrames.Length];
                    ballIdx++;
                }
            }

            if (chargingBallRenderer != null)
            {
                chargingBallRenderer.transform.Rotate(0f, 0f, 1080f * Time.deltaTime);
            }

            yield return null;
        }

        if (chargingBallRenderer != null) chargingBallRenderer.gameObject.SetActive(false);

        parts.SetPose(headMove, bodyFire, legCharge);

        if (projectileCPrefab != null)
        {
            Vector3 spawnPt = firePoint != null ? firePoint.position : transform.position + new Vector3(0.5f, 0.2f, 0f);
            GameObject bulletObj = Instantiate(projectileCPrefab, spawnPt, Quaternion.identity);
            KamehamehaC bullet = bulletObj.GetComponent<KamehamehaC>();
            if (bullet != null)
            {
                targetEnemyB = GetNearestEnemyB(float.MaxValue); // Kamehameha khoá mục tiêu toàn bản đồ
                bullet.Init(targetEnemyB);
            }
        }

        yield return new WaitForSeconds(0.15f);

        if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
        isChargingOrFiring = false;

        if (isJumping)
        {
            if (verticalVelocity < 0f)
                parts.SetPose(headMove, bodyFall, legFall);
            else
                parts.SetPose(headMove, bodyJump, legJump);
        }
        else
        {
            SetIdlePose();
        }
    }
    // --- NEW SKILLS --- //

    public void Attack2_KiBlast()
    {
        if (isChargingOrFiring) return;
        if (!UseKi(5f)) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();

        GameObject kiObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        kiObj.name = "KiBlast";
        kiObj.transform.position = firePoint != null ? firePoint.position : transform.position + new Vector3(0.5f, 0.2f, 0f);
        kiObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        Collider col = kiObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = kiObj.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Sprites/Default"));
            r.material.color = new Color(1f, 0.6f, 0f, 1f);
        }

        TrailRenderer tr = kiObj.AddComponent<TrailRenderer>();
        tr.time = 0.2f;
        tr.startWidth = 0.3f;
        tr.endWidth = 0f;
        tr.material = new Material(Shader.Find("Sprites/Default"));
        tr.startColor = Color.yellow;
        tr.endColor = new Color(1f, 0f, 0f, 0f);

        targetEnemyB = GetNearestEnemyB(10f); // Ki Blast bị giới hạn quét 10 đơn vị
        StartCoroutine(KiBlastFlightRoutine(kiObj, targetEnemyB));
    }

    private IEnumerator KiBlastFlightRoutine(GameObject kiObj, Transform target)
    {
        float speed = 25f;
        float lifeTime = 2f;
        float elapsed = 0f;
        Vector3 targetPos = target != null ? target.position : kiObj.transform.position + Vector3.right * 10f;

        while (kiObj != null && elapsed < lifeTime)
        {
            if (target != null && target.gameObject.activeInHierarchy) targetPos = target.position;

            kiObj.transform.position = Vector3.MoveTowards(kiObj.transform.position, targetPos, speed * Time.deltaTime);
            elapsed += Time.deltaTime;

            if (Vector3.Distance(kiObj.transform.position, targetPos) < 1.2f)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                if (target != null)
                {
                    EnemyB eb = target.GetComponent<EnemyB>();
                    if (eb != null)
                    {
                        if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
                        eb.StartCoroutine("DieAndRespawnRoutine");
                    }
                    NPC_Frieza frieza = target.GetComponent<NPC_Frieza>();
                    if (frieza != null)
                    {
                        if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
                        frieza.TakeHit();
                    }
                    NPC_Cell cell = target.GetComponent<NPC_Cell>();
                    if (cell != null)
                    {
                        if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
                        cell.TakeHit();
                    }
                    NPC_Piccolo piccolo = target.GetComponent<NPC_Piccolo>();
                    if (piccolo != null)
                    {
                        if (GameManager.Instance != null) GameManager.Instance.AddScore(15);
                        piccolo.TakeHit();
                    }
                    Boss_PiccoloA bossPiccolo = target.GetComponent<Boss_PiccoloA>();
                    if (bossPiccolo != null && !bossPiccolo.isDead)
                    {
                        if (GameManager.Instance != null) GameManager.Instance.AddScore(15);
                        bossPiccolo.TakeHit(1);
                    }
                }
                Destroy(kiObj);
                yield break;
            }
            yield return null;
        }
        if (kiObj != null) Destroy(kiObj);
    }

    public void Attack3_Melee()
    {
        if (isChargingOrFiring) return;
        if (!UseKi(10f)) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
        StartCoroutine(MeleeRoutine());
    }

    private IEnumerator MeleeRoutine()
    {
        isChargingOrFiring = true;
        isMeleeAttacking = true;
        parts.SetPose(headMove, bodyJump, legJump);

        Vector3 startPos = transform.position;
        // Lướt tới phía trước 3m tuỳ hướng quay mặt
        Vector3 dashPos = startPos + new Vector3(parts.transform.localScale.x > 0 ? 3f : -3f, 0f, 0f);
        
        if (parts.bodyRenderer != null) parts.bodyRenderer.color = Color.red;

        float dashTime = 0.15f;
        float t = 0;

        while (t < dashTime)
        {
            transform.position = Vector3.Lerp(startPos, dashPos, t / dashTime);
            t += Time.deltaTime;
            
            targetEnemyB = GetNearestEnemyB(7f); // Quét kẻ địch gần
            if (targetEnemyB != null && Vector3.Distance(transform.position, targetEnemyB.position) < 2.5f)
            {
                EnemyB eb = targetEnemyB.GetComponent<EnemyB>();
                if (eb != null && eb.gameObject.activeInHierarchy)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    if (GameManager.Instance != null) GameManager.Instance.AddScore(20);
                    eb.StartCoroutine("DieAndRespawnRoutine"); 
                }
                NPC_Frieza frieza = targetEnemyB.GetComponent<NPC_Frieza>();
                if (frieza != null && frieza.gameObject.activeInHierarchy)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    if (GameManager.Instance != null) GameManager.Instance.AddScore(20);
                    frieza.TakeHit();
                }
                NPC_Cell cell = targetEnemyB.GetComponent<NPC_Cell>();
                if (cell != null && cell.gameObject.activeInHierarchy)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    if (GameManager.Instance != null) GameManager.Instance.AddScore(20);
                    cell.TakeHit(2);
                }
                NPC_Piccolo piccolo = targetEnemyB.GetComponent<NPC_Piccolo>();
                if (piccolo != null && piccolo.gameObject.activeInHierarchy && !piccolo.isDead)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    if (GameManager.Instance != null) GameManager.Instance.AddScore(20);
                    piccolo.TakeHit(1);
                }
                Boss_PiccoloA bossPiccolo = targetEnemyB.GetComponent<Boss_PiccoloA>();
                if (bossPiccolo != null && bossPiccolo.gameObject.activeInHierarchy && !bossPiccolo.isDead)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
                    if (GameManager.Instance != null) GameManager.Instance.AddScore(20);
                    bossPiccolo.TakeHit(1);
                }
            }
            yield return null;
        }

        t = 0;
        while (t < dashTime)
        {
            transform.position = Vector3.Lerp(dashPos, startPos, t / dashTime);
            t += Time.deltaTime;
            yield return null;
        }

        if (parts.bodyRenderer != null) parts.bodyRenderer.color = Color.white;
        transform.position = startPos;
        isMeleeAttacking = false;
        isChargingOrFiring = false;
    }

    public void Defense1_Shield()
    {
        if (hasShield) return;
        if (!UseKi(30f)) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
        StartCoroutine(ShieldRoutine());
    }

    private IEnumerator ShieldRoutine()
    {
        hasShield = true;
        
        if (shieldVisual == null)
        {
            shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldVisual.name = "EnergyShield";
            shieldVisual.transform.SetParent(this.transform);
            shieldVisual.transform.localPosition = new Vector3(0, 0.5f, 0);
            Collider col = shieldVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            Renderer r = shieldVisual.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Sprites/Default"));
                r.material.color = new Color(0f, 0.8f, 1f, 0.4f);
            }
        }
        
        shieldVisual.SetActive(true);

        float duration = 3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = 2.0f + Mathf.Sin(elapsed * 10f) * 0.2f;
            shieldVisual.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        shieldVisual.SetActive(false);
        hasShield = false;
    }

    public void Defense2_SolarFlare()
    {
        if (!UseKi(40f)) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosion();
        StartCoroutine(SolarFlareRoutine());
    }

    private IEnumerator SolarFlareRoutine()
    {
        GameObject flashObj = new GameObject("FlashScreen");
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            flashObj.transform.SetParent(canvas.transform, false);
            UnityEngine.UI.Image img = flashObj.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.white;
            RectTransform rt = flashObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            Destroy(flashObj, 0.5f);
        }

        EnemyB[] enemies = FindObjectsByType<EnemyB>(FindObjectsSortMode.None);
        foreach (var eb in enemies)
        {
            if (eb.gameObject.activeInHierarchy)
            {
                eb.StartCoroutine(eb.StunRoutine(2.5f));
            }
        }
        NPC_Piccolo[] piccolos = FindObjectsByType<NPC_Piccolo>(FindObjectsSortMode.None);
        foreach (var pic in piccolos)
        {
            if (pic != null && pic.gameObject.activeInHierarchy && !pic.isDead)
            {
                pic.StartCoroutine(pic.StunRoutine(2.5f));
            }
        }
        Boss_PiccoloA[] pBosses = FindObjectsByType<Boss_PiccoloA>(FindObjectsSortMode.None);
        foreach (var pb in pBosses)
        {
            if (pb != null && pb.gameObject.activeInHierarchy && !pb.isDead)
            {
                pb.StartCoroutine(pb.StunRoutine(2.5f));
            }
        }
        yield return null;
    }

    /// <summary>
    /// Tìm kẻ địch B gần nhất để Kamehameha C tự động bám đuổi
    /// Giới hạn khoảng cách maxDistance để tránh chiêu bị quá bá đạo (OP)
    /// </summary>
    public Transform GetNearestEnemyB(float maxDistance = 15f)
    {
        Transform best = null;
        float minDist = float.MaxValue;
        Vector3 pos = transform.position;

        EnemyB[] enemies = FindObjectsByType<EnemyB>(FindObjectsSortMode.None);
        if (enemies != null)
        {
            foreach (var e in enemies)
            {
                if (e == null || !e.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(pos, e.transform.position);
                if (d < minDist && d <= maxDistance)
                {
                    minDist = d;
                    best = e.transform;
                }
            }
        }

        NPC_Cell[] cells = FindObjectsByType<NPC_Cell>(FindObjectsSortMode.None);
        if (cells != null)
        {
            foreach (var c in cells)
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(pos, c.transform.position);
                if (d < minDist && d <= maxDistance)
                {
                    minDist = d;
                    best = c.transform;
                }
            }
        }

        NPC_Frieza[] friezas = FindObjectsByType<NPC_Frieza>(FindObjectsSortMode.None);
        if (friezas != null)
        {
            foreach (var f in friezas)
            {
                if (f == null || !f.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(pos, f.transform.position);
                if (d < minDist && d <= maxDistance)
                {
                    minDist = d;
                    best = f.transform;
                }
            }
        }

        NPC_Piccolo[] piccolos = FindObjectsByType<NPC_Piccolo>(FindObjectsSortMode.None);
        if (piccolos != null)
        {
            foreach (var p in piccolos)
            {
                if (p == null || !p.gameObject.activeInHierarchy || p.isDead) continue;
                float d = Vector3.Distance(pos, p.transform.position);
                if (d < minDist && d <= maxDistance)
                {
                    minDist = d;
                    best = p.transform;
                }
            }
        }

        Boss_PiccoloA[] pBossList = FindObjectsByType<Boss_PiccoloA>(FindObjectsSortMode.None);
        if (pBossList != null)
        {
            foreach (var pb in pBossList)
            {
                if (pb == null || !pb.gameObject.activeInHierarchy || pb.isDead) continue;
                float d = Vector3.Distance(pos, pb.transform.position);
                if (d < minDist && d <= maxDistance)
                {
                    minDist = d;
                    best = pb.transform;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Đảm bảo số lượng Enemy B luôn đủ theo cấu hình enemyBCount (Chạy được ở cả Editor và Runtime APK)
    /// </summary>
    public void EnsureEnemyBCount()
    {
        // Chỉ sinh Vegeta (EnemyB) ở Cấp 1. Ở Cấp 2 (Piccolo) và Cấp 3 (Cell) không sinh Vegeta!
        if (Application.isPlaying && LevelManager.Instance != null && LevelManager.Instance.currentLevel != 1)
        {
            return;
        }

        EnemyB[] existing = FindObjectsByType<EnemyB>(FindObjectsSortMode.None);
        int currentCount = existing != null ? existing.Length : 0;

        if (existing != null)
        {
            foreach (var e in existing)
            {
                if (e != null)
                {
                    CharacterParts cp = e.GetComponent<CharacterParts>();
                    if (cp != null)
                    {
                        cp.characterScale = characterScale;
                        cp.headHeight = 1.1375f;
                        cp.headSize = 0.75f;
                        cp.headScale = new Vector3(0.75f, 0.75f, 1f);
                        cp.SyncAndApplyAll();
                    }
                    e.characterScale = characterScale;
                    e.headHeight = 1.1375f;
                }
            }
        }

        for (int i = currentCount; i < enemyBCount; i++)
        {
            CreateSingleEnemyB(i + 1);
        }

        targetEnemyB = GetNearestEnemyB();
#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Xóa Toàn Bộ B Để Tạo Lại")]
    public void ClearAndRecreateAllEnemyB()
    {
        EnemyB[] existing = FindObjectsByType<EnemyB>(FindObjectsSortMode.None);
        if (existing != null)
        {
            foreach (var e in existing)
            {
                if (e != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(e.gameObject);
                    else Destroy(e.gameObject);
#else
                    Destroy(e.gameObject);
#endif
                }
            }
        }
        EnsureEnemyBCount();
    }

    /// <summary>
    /// Tạo 1 đối tượng B (Vegeta): An toàn 100% khi chạy độc lập trong file APK (không lỗi thiếu AssetDatabase)
    /// </summary>
    public GameObject CreateSingleEnemyB(int id = 1)
    {
        // 1. Ưu tiên sinh từ Prefab nếu có
        if (enemyBPrefab != null)
        {
            GameObject obj = Instantiate(enemyBPrefab);
            obj.name = "Enemy_B_" + id;
            if (Camera.main != null)
            {
                float randomX = Random.Range(0.72f, 0.94f);
                float randomY = Random.Range(0.18f, 0.82f);
                Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(randomX, randomY, 10f));
                spawnPos.z = 0f;
                obj.transform.position = spawnPos;
            }
            return obj;
        }

        // 2. Nếu trong Scene đã có ít nhất 1 con B, nhân bản (clone) nhanh
        EnemyB existingOne = FindFirstObjectByType<EnemyB>();
        if (existingOne != null)
        {
            GameObject obj = Instantiate(existingOne.gameObject);
            obj.name = "Enemy_B_" + id;
            if (Camera.main != null)
            {
                float randomX = Random.Range(0.72f, 0.94f);
                float randomY = Random.Range(0.18f, 0.82f);
                Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(randomX, randomY, 10f));
                spawnPos.z = 0f;
                obj.transform.position = spawnPos;
            }
            return obj;
        }

        // 3. Khởi tạo mới từ mã nguồn với các sprite đã serialized sẵn
        GameObject enemyObj = new GameObject("Enemy_B_" + id);
        CharacterParts cParts = enemyObj.AddComponent<CharacterParts>();
        EnemyB eB = enemyObj.AddComponent<EnemyB>();

        GameObject h = new GameObject("Head");
        h.transform.SetParent(enemyObj.transform, false);
        h.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        SpriteRenderer hSr = h.AddComponent<SpriteRenderer>();
        hSr.sortingOrder = 3;

        GameObject b = new GameObject("Body");
        b.transform.SetParent(enemyObj.transform, false);
        b.transform.localScale = Vector3.one;
        SpriteRenderer bSr = b.AddComponent<SpriteRenderer>();
        bSr.sortingOrder = 2;

        GameObject l = new GameObject("Leg");
        l.transform.SetParent(enemyObj.transform, false);
        l.transform.localScale = Vector3.one;
        SpriteRenderer lSr = l.AddComponent<SpriteRenderer>();
        lSr.sortingOrder = 1;

        cParts.headRenderer = hSr;
        cParts.bodyRenderer = bSr;
        cParts.legRenderer = lSr;
        cParts.headHeight = 1.1375f;
        cParts.headOffset = new Vector3(0f, 1.1375f, 0f);
        cParts.legOffset = new Vector3(0f, -0.6f, 0f);
        cParts.headSize = 0.75f;
        cParts.headScale = new Vector3(0.75f, 0.75f, 1f);
        cParts.characterScale = characterScale;
        cParts.SyncAndApplyAll();

        eB.characterScale = characterScale;
        eB.headHeight = 1.1375f;

        // Gán sprite đã serialize của B
        eB.headIdle = enemyBHeadIdle;
        eB.headMove = enemyBHeadMove;
        eB.bodyIdle = bodyIdle;
        eB.legIdle = legIdle;
        eB.bodyMoveFrames = bodyMoveFrames;
        eB.legMoveFrames = legMoveFrames;

#if UNITY_EDITOR
        if (eB.headIdle == null) eB.headIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Small91.png");
        if (eB.headMove == null) eB.headMove = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Small92.png");
        eB.AutoAssignSprites();
#endif

        if (Camera.main != null)
        {
            float randomX = Random.Range(0.72f, 0.94f);
            float randomY = Random.Range(0.18f, 0.82f);
            Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(randomX, randomY, 10f));
            spawnPos.z = 0f;
            enemyObj.transform.position = spawnPos;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(enemyObj);
#endif
        return enemyObj;
    }

#if UNITY_EDITOR
    [ContextMenu("Tạo Lại Enemy B Chuẩn 100% (1-Click)")]
    public void CreateEnemyBInScene()
    {
        EnsureEnemyBCount();
    }

    [ContextMenu("Tự Động Gán Toàn Bộ Sprite (1-Click)")]
    public void AutoAssignSprites()
    {
        string p = "Assets/Sprites/";
        headIdle = AssetDatabase.LoadAssetAtPath<Sprite>(p + "17.png");
        bodyIdle = AssetDatabase.LoadAssetAtPath<Sprite>(p + "1.png");
        legIdle  = AssetDatabase.LoadAssetAtPath<Sprite>(p + "22.png");

        headMove = AssetDatabase.LoadAssetAtPath<Sprite>(p + "18.png");
        bodyMoveFrames = new Sprite[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "2.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "3.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "4.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "5.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "6.png")
        };
        legMoveFrames = new Sprite[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "23.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "24.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "25.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "26.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "27.png")
        };

        bodyJumpPrep = AssetDatabase.LoadAssetAtPath<Sprite>(p + "7.png");
        legJumpPrep  = AssetDatabase.LoadAssetAtPath<Sprite>(p + "27.png");
        bodyJump     = AssetDatabase.LoadAssetAtPath<Sprite>(p + "8.png");
        legJump      = AssetDatabase.LoadAssetAtPath<Sprite>(p + "28.png");
        bodyFall     = AssetDatabase.LoadAssetAtPath<Sprite>(p + "8.png");
        legFall      = AssetDatabase.LoadAssetAtPath<Sprite>(p + "29.png");

        bodyChargePrep = AssetDatabase.LoadAssetAtPath<Sprite>(p + "13.png");
        bodyCharging   = AssetDatabase.LoadAssetAtPath<Sprite>(p + "14.png");
        bodyFire       = AssetDatabase.LoadAssetAtPath<Sprite>(p + "15.png");
        legCharge      = AssetDatabase.LoadAssetAtPath<Sprite>(p + "30.png");

        auraFrames = new Sprite[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small982.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small983.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small984.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small985.png")
        };

        attackButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small540.png");
        moveButtonSprite   = AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small2322.png");

        chargingBallFrames = new Sprite[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small48.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small49.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small50.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small51.png")
        };

        enemyBHeadIdle = AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small91.png");
        enemyBHeadMove = AssetDatabase.LoadAssetAtPath<Sprite>(p + "Small92.png");
        if (enemyBPrefab == null)
        {
            enemyBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_B.prefab");
            if (enemyBPrefab == null)
            {
                enemyBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemy_B.prefab");
            }
        }

        if (projectileCPrefab == null)
        {
            projectileCPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Projectile_C.prefab");
        }

        EnsureAttackButton();
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Tạo Nút Tung Chưởng & Di Chuyển UI (1-Click)")]
    public void CreateAttackButtonContextMenu()
    {
        EnsureAttackButton();
        EditorUtility.SetDirty(this);
    }
#endif

    public void EnsureAttackButton()
    {
        if (attackButtonSprite == null)
        {
#if UNITY_EDITOR
            attackButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Small540.png");
#endif
        }

        if (moveButtonSprite == null)
        {
#if UNITY_EDITOR
            moveButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Small2322.png");
#endif
        }

        // 1. Tìm hoặc tạo EventSystem
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Tìm hoặc tạo Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // 3. Nút Tung Chưởng AttackButton (Góc Dưới-Phải: Small540.png)
        Transform btnTransform = canvas.transform.Find("AttackButton");
        GameObject btnObj = null;
        if (btnTransform == null)
        {
            btnObj = new GameObject("AttackButton");
            btnObj.transform.SetParent(canvas.transform, false);
        }
        else
        {
            btnObj = btnTransform.gameObject;
        }

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (rt == null) rt = btnObj.AddComponent<RectTransform>();

        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(attackButtonSize, attackButtonSize);
        rt.anchoredPosition = new Vector2(-attackButtonMargin.x, attackButtonMargin.y);

        // Đĩa nền mờ phía sau nút tung chưởng giúp nút nổi bật rõ trên mọi khung cảnh
        Transform bgTrans = btnObj.transform.Find("Attack_BG");
        GameObject bgObj = bgTrans != null ? bgTrans.gameObject : new GameObject("Attack_BG");
        bgObj.transform.SetParent(btnObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        if (bgRt == null) bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = new Vector2(16f, 16f);
        bgRt.anchoredPosition = Vector2.zero;
        UnityEngine.UI.Image bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
        if (bgImg == null) bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.28f);
        bgImg.raycastTarget = false;

        UnityEngine.UI.Image img = btnObj.GetComponent<UnityEngine.UI.Image>();
        if (img == null) img = btnObj.AddComponent<UnityEngine.UI.Image>();
        if (attackButtonSprite != null) img.sprite = attackButtonSprite;
        img.preserveAspect = true;
        img.raycastTarget = true;

        UnityEngine.UI.Button btn = btnObj.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) btn = btnObj.AddComponent<UnityEngine.UI.Button>();

        UnityEngine.UI.ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.8f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = colors;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Attack1_Kamehameha);

        // Kích hoạt chưởng tức thì ngay khi ngón tay vừa chạm vào nút (PointerDown)
        UnityEngine.EventSystems.EventTrigger atkTrigger = btnObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (atkTrigger == null) atkTrigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        atkTrigger.triggers.Clear();
        var atkDownEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        atkDownEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        atkDownEntry.callback.AddListener((data) => { Attack1_Kamehameha(); });
        atkTrigger.triggers.Add(atkDownEntry);

        attackButton = btn;

        // 4. Các Nút Kỹ Năng Phụ (Ki Blast, Melee, Shield, Solar Flare)
        // 4 Nút Skill sử dụng Sprite mới cắt
        CreateSkillButton(canvas.transform, "Btn_KiBlast", "5 Ki", new Vector2(-20, 300), "btn_ki_blast", Attack2_KiBlast);
        CreateSkillButton(canvas.transform, "Btn_Melee", "10 Ki", new Vector2(-150, 300), "btn_melee", Attack3_Melee);
        CreateSkillButton(canvas.transform, "Btn_Shield", "30 Ki", new Vector2(-300, 150), "btn_shield", Defense1_Shield);
        CreateSkillButton(canvas.transform, "Btn_Solar", "40 Ki", new Vector2(-300, 250), "btn_solar_flare", Defense2_SolarFlare);

        // 5. Cụm 4 Nút Di Chuyển D-Pad (Góc Dưới-Trái: Small2322.png)
        EnsureDpadButtons(canvas);
    }

    private void CreateSkillButton(Transform parent, string name, string costText, Vector2 anchoredPos, string spriteName, UnityEngine.Events.UnityAction action)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        UnityEngine.UI.Image bg = btnObj.AddComponent<UnityEngine.UI.Image>();
        
        Sprite sp = Resources.Load<Sprite>(spriteName);
        if (sp != null)
        {
            bg.sprite = sp;
            bg.color = Color.white; // Không dùng màu đè lên sprite
        }
        else
        {
            bg.color = new Color(1f, 1f, 1f, 0.5f);
        }

        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0); 
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(100, 100); // Hình vuông cho icon tròn

        // Chữ hiển thị lượng Ki (nhỏ, ở góc dưới)
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.color = Color.yellow;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.LowerCenter;
        txt.text = costText;
        
        // Thêm viền đen cho chữ dễ đọc
        UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = new Vector2(0, -20); // Dịch chữ xuống dưới đáy

        btn.onClick.RemoveAllListeners();
        // Bỏ onClick để tránh lỗi xả chiêu 2 lần (double cast) khi bấm trên điện thoại. Chỉ dùng PointerDown bên dưới.

        // Hỗ trợ PointerDown cho cảm ứng cực nhạy (giống nút đánh chính)
        UnityEngine.EventSystems.EventTrigger trigger = btnObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        trigger.triggers.Clear();
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { action.Invoke(); });
        trigger.triggers.Add(entry);
    }

    private void EnsureDpadButtons(Canvas canvas)
    {
        Transform dpadTransform = canvas.transform.Find("MoveDpad");
        GameObject dpadObj = null;
        if (dpadTransform == null)
        {
            dpadObj = new GameObject("MoveDpad");
            dpadObj.transform.SetParent(canvas.transform, false);
        }
        else
        {
            dpadObj = dpadTransform.gameObject;
        }

        RectTransform dpadRt = dpadObj.GetComponent<RectTransform>();
        if (dpadRt == null) dpadRt = dpadObj.AddComponent<RectTransform>();
        dpadRt.anchorMin = new Vector2(0f, 0f);
        dpadRt.anchorMax = new Vector2(0f, 0f);
        dpadRt.pivot = new Vector2(0.5f, 0.5f);
        dpadRt.anchoredPosition = moveDpadPosition;

        // Đĩa nền mờ phía sau cụm D-Pad giúp người chơi dễ định vị vùng điều khiển trên điện thoại
        Transform bgTrans = dpadObj.transform.Find("Dpad_BG");
        GameObject bgObj = bgTrans != null ? bgTrans.gameObject : new GameObject("Dpad_BG");
        bgObj.transform.SetParent(dpadObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        if (bgRt == null) bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        float bgSize = moveButtonSpacing * 2f + moveButtonSize + 30f;
        bgRt.sizeDelta = new Vector2(bgSize, bgSize);
        bgRt.anchoredPosition = Vector2.zero;
        UnityEngine.UI.Image bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
        if (bgImg == null) bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.22f);
        bgImg.raycastTarget = false;

        // 4 nút: Lên (Nhảy / W), Xuống (S), Trái (A), Phải (D)
        CreateSingleDirButton(dpadObj, "Button_Up", new Vector2(0f, moveButtonSpacing), "▲", () => { TryJump(); }, null);
        CreateSingleDirButton(dpadObj, "Button_Down", new Vector2(0f, -moveButtonSpacing), "▼", () => { isHoldingDown = true; }, () => { isHoldingDown = false; });
        CreateSingleDirButton(dpadObj, "Button_Left", new Vector2(-moveButtonSpacing, 0f), "◄", () => { isHoldingLeft = true; }, () => { isHoldingLeft = false; });
        CreateSingleDirButton(dpadObj, "Button_Right", new Vector2(moveButtonSpacing, 0f), "►", () => { isHoldingRight = true; }, () => { isHoldingRight = false; });
    }

    private void CreateSingleDirButton(GameObject parent, string name, Vector2 pos, string label, System.Action onDown, System.Action onUp)
    {
        Transform child = parent.transform.Find(name);
        GameObject btnObj = child != null ? child.gameObject : new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (rt == null) rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(moveButtonSize, moveButtonSize);
        rt.anchoredPosition = pos;

        UnityEngine.UI.Image img = btnObj.GetComponent<UnityEngine.UI.Image>();
        if (img == null) img = btnObj.AddComponent<UnityEngine.UI.Image>();
        if (moveButtonSprite != null) img.sprite = moveButtonSprite;
        img.preserveAspect = true;
        img.raycastTarget = true;

        UnityEngine.UI.Button btn = btnObj.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) btn = btnObj.AddComponent<UnityEngine.UI.Button>();

        UnityEngine.UI.ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.8f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;

        // Nhãn mũi tên to rõ ràng (▲, ▼, ◄, ►)
        Transform textChild = btnObj.transform.Find("Label");
        GameObject textObj = textChild != null ? textChild.gameObject : new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        if (textRt == null) textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        UnityEngine.UI.Text txt = textObj.GetComponent<UnityEngine.UI.Text>();
        if (txt == null) txt = textObj.AddComponent<UnityEngine.UI.Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 44;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.raycastTarget = false;
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) txt.font = f;

        // Sự kiện giữ chuột / chạm tay (PointerDown, PointerUp)
        UnityEngine.EventSystems.EventTrigger trigger = btnObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        trigger.triggers.Clear();

        if (onDown != null)
        {
            var downEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            downEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            downEntry.callback.AddListener((data) => { onDown(); });
            trigger.triggers.Add(downEntry);
        }

        if (onUp != null)
        {
            var upEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            upEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            upEntry.callback.AddListener((data) => { onUp(); });
            trigger.triggers.Add(upEntry);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Quản lý Background Cuộn Vô Tận (Ghép từ bg1.png ở trên và diahinh.png ở dưới theo mẫu bg2.png)
/// - bg1 (thành phố + mây trời) ở trên (sortingOrder = -10)
/// - diahinh (thảm cỏ + khối đất) ở dưới (sortingOrder = -5)
/// - Các khối nối đầu đuôi nhau (head-to-tail) cuộn đều sang trái
/// - Định nghĩa cao độ biên chuẩn cho toàn bộ game:
///   + GroundSurfaceY: Mặt trên thảm cỏ (biên dưới - vật cản)
///   + GroundY: Tọa độ đứng chuẩn của nhân vật A trên thảm cỏ
///   + CeilingY: Trần trên của màn hình
///   + LeftBorderX / RightBorderX: Biên trái và phải màn hình
/// </summary>
[ExecuteAlways]
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    [Header("Sprite Nền (Tự động nạp nếu trống)")]
    public Sprite bgTopSprite;       // Assets/Sprites/bg1.png (3040x1217)
    public Sprite bgGroundSprite;    // Assets/Sprites/diahinh.png (2325x157)

    [Header("Tốc Độ Cuộn Nền (Head-to-Tail Scrolling)")]
    [Tooltip("Tốc độ trôi của background sang trái (Mặc định: 2.0)")]
    public float scrollSpeed = 2.0f;

    [Header("Thứ Tự Hiển Thị (Sorting Order)")]
    public int sortingOrderTop = -10;
    public int sortingOrderGround = -5;

    [Header("Hiệu Chỉnh Cao Độ Biên (Tùy Chọn)")]
    [Tooltip("Độ bù cao độ đứng của Goku trên thảm cỏ (Mặc định: 0.27)")]
    public float feetOffset = 0.27f;

    [Tooltip("Bù độ cao trần trên")]
    public float ceilingOffset = -0.20f;

    // Tọa độ biên tĩnh để các script khác (PlayerA, EnemyB, KamehamehaC) truy cập trực tiếp
    public static float GroundSurfaceY = -3.86f;
    public static float GroundY = -3.59f;
    public static float CeilingY = 4.80f;
    public static float LeftBorderX = -8.88f;
    public static float RightBorderX = 8.88f;

    // Danh sách các GameObject con tạo chuỗi cuộn vô tận
    private List<Transform> topPieces = new List<Transform>();
    private List<Transform> groundPieces = new List<Transform>();

    private float topPieceWidth = 22.13f;
    private float groundPieceWidth = 16.90f;
    private Transform levelVisuals;
    private SpriteRenderer namekPlanet;
    private SpriteRenderer spacePlanet;
    private readonly List<SpriteRenderer> spaceStars = new List<SpriteRenderer>();
    private static Sprite starSprite;
    private static Sprite namekPlanetSprite;
    private static Sprite spacePlanetSprite;

    private const int TOP_PIECE_COUNT = 3;
    private const int GROUND_PIECE_COUNT = 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<BackgroundManager>() == null)
        {
            GameObject bgGo = new GameObject("BackgroundManager");
            bgGo.AddComponent<BackgroundManager>();
        }
    }

    private void Awake()
    {
        Instance = this;
        AutoLoadSprites();
        UpdateBoundaries();
        SetupPieces();
    }

    private void OnEnable()
    {
        Instance = this;
        AutoLoadSprites();
        UpdateBoundaries();
        SetupPieces();
    }

    private void Start()
    {
        Instance = this;
        AutoLoadSprites();
        UpdateBoundaries();
        SetupPieces();
    }

    private void Update()
    {
        UpdateBoundaries();

        if (Application.isPlaying && scrollSpeed > 0f)
        {
            ScrollPieces(topPieces, topPieceWidth, scrollSpeed);
            ScrollPieces(groundPieces, groundPieceWidth, scrollSpeed);
        }
    }

    public void UpdateBoundaries()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;
        float camBottom = cam.transform.position.y - cam.orthographicSize;
        float camTop = cam.transform.position.y + cam.orthographicSize;

        // Tỉ lệ từ mẫu bg2 (157px diahinh / 1376px tổng)
        float groundH = camH * (157f / 1376f); // ~1.141f
        GroundSurfaceY = camBottom + groundH;  // ~ -3.859f
        GroundY = GroundSurfaceY + feetOffset; // ~ -3.59f
        CeilingY = camTop + ceilingOffset;      // ~ 4.80f

        LeftBorderX = cam.transform.position.x - (camW / 2f);
        RightBorderX = cam.transform.position.x + (camW / 2f);
    }

    public void SetupPieces()
    {
        if (bgTopSprite == null || bgGroundSprite == null)
        {
            AutoLoadSprites();
            if (bgTopSprite == null || bgGroundSprite == null) return;
        }

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        float camH = cam.orthographicSize * 2f;
        float camBottom = cam.transform.position.y - cam.orthographicSize;
        float camTop = cam.transform.position.y + cam.orthographicSize;

        float groundH = camH * (157f / 1376f);
        float topH = camH; // Bầu trời lấp đầy toàn bộ màn hình

        // 1. Tính toán kích thước và vị trí mảnh Top (bg1)
        float nativeTopH = bgTopSprite.rect.height / bgTopSprite.pixelsPerUnit;
        float scaleTop = topH / nativeTopH;
        topPieceWidth = (bgTopSprite.rect.width / bgTopSprite.pixelsPerUnit) * scaleTop;
        float topCenterY = cam.transform.position.y; // Căn giữa camera

        // 2. Tính toán kích thước và vị trí mảnh Ground (diahinh)
        float nativeGroundH = bgGroundSprite.rect.height / bgGroundSprite.pixelsPerUnit;
        float scaleGround = groundH / nativeGroundH;
        groundPieceWidth = (bgGroundSprite.rect.width / bgGroundSprite.pixelsPerUnit) * scaleGround;
        float groundCenterY = camBottom + (groundH / 2f);

        // Khởi tạo hoặc cập nhật các GameObject con
        SetupContainer("Top_Layers", topPieces, TOP_PIECE_COUNT, bgTopSprite, scaleTop, topPieceWidth, topCenterY, sortingOrderTop);
        SetupContainer("Ground_Layers", groundPieces, GROUND_PIECE_COUNT, bgGroundSprite, scaleGround, groundPieceWidth, groundCenterY, sortingOrderGround);
    }

    private void SetupContainer(string containerName, List<Transform> list, int count, Sprite sprite, float scale, float width, float centerY, int sortOrder)
    {
        Transform container = transform.Find(containerName);
        if (container == null)
        {
            GameObject go = new GameObject(containerName);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            container = go.transform;
        }

        list.Clear();
        float startX = Camera.main != null ? (Camera.main.transform.position.x - 12f) : -10f;

        for (int i = 0; i < count; i++)
        {
            string childName = containerName + "_" + i;
            Transform child = container.Find(childName);
            if (child == null)
            {
                GameObject pieceGo = new GameObject(childName);
                pieceGo.transform.SetParent(container);
                child = pieceGo.transform;
            }

            child.localScale = new Vector3(scale, scale, 1f);
            child.position = new Vector3(startX + i * width, centerY, 0f);

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) sr = child.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortOrder;

            list.Add(child);
        }
    }

    private void ScrollPieces(List<Transform> pieces, float pieceWidth, float speed)
    {
        if (pieces == null || pieces.Count == 0) return;

        float moveAmount = speed * Time.deltaTime;

        for (int i = 0; i < pieces.Count; i++)
        {
            Transform p = pieces[i];
            if (p == null) continue;

            p.position += Vector3.left * moveAmount;
        }

        // Kiểm tra mảnh nào đã trôi hoàn toàn sang mép trái màn hình thì quấn sang sau mảnh phải nhất
        for (int i = 0; i < pieces.Count; i++)
        {
            Transform p = pieces[i];
            if (p == null) continue;

            // Nếu cạnh phải của mảnh đã đi qua mép trái màn hình (LeftBorderX - 2f)
            if (p.position.x + (pieceWidth / 2f) < LeftBorderX - 1f)
            {
                // Tìm mảnh đang ở vị trí X lớn nhất (bên phải nhất)
                float maxRightX = float.MinValue;
                for (int j = 0; j < pieces.Count; j++)
                {
                    if (pieces[j] != null && pieces[j].position.x > maxRightX)
                    {
                        maxRightX = pieces[j].position.x;
                    }
                }

                // Chuyển mảnh hiện tại sang nối tiếp ngay sau mảnh bên phải nhất
                Vector3 newPos = p.position;
                newPos.x = maxRightX + pieceWidth - 0.02f; // Trừ nhẹ 0.02f để tránh khe hở pixel
                p.position = newPos;
            }
        }
    }

    public void AutoLoadSprites()
    {
#if UNITY_EDITOR
        string p = "Assets/Sprites/";
        if (bgTopSprite == null)
        {
            bgTopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(p + "bg1.png");
        }
        if (bgGroundSprite == null)
        {
            bgGroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(p + "diahinh.png");
        }
#endif
    }

    /// <summary>
    /// Đổi màu tint background theo cấp độ:
    /// Level 1 - Trái Đất: trắng (gốc)
    /// Level 2 - Namek:    xanh lam nhạt
    /// Level 3 - Không Gian: tím/đen
    /// Nếu có sprite bg_namek.png / bg_space.png trong Resources thì dùng trực tiếp.
    /// </summary>
    public void SetLevelTint(int level)
    {
        Color topTint    = Color.white;
        Color groundTint = Color.white;
        Color camBg      = new Color(0.40f, 0.70f, 1.0f);

        EnsureLevelVisuals();

        if (level == 2)
        {
            topTint = Color.white;
            groundTint = new Color(0.32f, 0.82f, 0.36f, 1f);
            camBg = new Color(0.035f, 0.31f, 0.25f);
        }
        else if (level == 3)
        {
            topTint = Color.white;
            groundTint = new Color(0.52f, 0.48f, 0.62f, 1f);
            camBg = new Color(0.012f, 0.018f, 0.075f);
        }
        else
        {
            topTint    = Color.white;
            groundTint = Color.white;
            camBg      = new Color(0.40f, 0.70f, 1.0f);
        }

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = camBg;
        }

        bool showCity = level == 1;
        bool showGround = true;
        foreach (var t in topPieces)
        {
            if (t == null) continue;
            SpriteRenderer renderer = t.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = bgTopSprite;
                renderer.color = topTint;
                renderer.enabled = showCity;
            }
        }
        foreach (var g in groundPieces)
        {
            if (g == null) continue;
            SpriteRenderer renderer = g.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = bgGroundSprite;
                renderer.color = groundTint;
                renderer.enabled = showGround;
            }
        }

        if (namekPlanet != null) namekPlanet.enabled = level == 2;
        if (spacePlanet != null) spacePlanet.enabled = level == 3;
        foreach (SpriteRenderer star in spaceStars)
            if (star != null) star.enabled = level == 3;
    }

    private void EnsureLevelVisuals()
    {
        if (levelVisuals == null)
        {
            GameObject visuals = new GameObject("LevelWorldVisuals");
            visuals.transform.SetParent(transform, false);
            levelVisuals = visuals.transform;
        }

        if (starSprite == null)
            starSprite = CreateCircleSprite(16, Color.white, new Color(0.7f, 0.9f, 1f));
        if (namekPlanetSprite == null)
            namekPlanetSprite = CreateCircleSprite(96, new Color(0.10f, 0.48f, 0.22f), new Color(0.55f, 0.95f, 0.42f));
        if (spacePlanetSprite == null)
            spacePlanetSprite = CreateCircleSprite(96, new Color(0.56f, 0.12f, 0.20f), new Color(1f, 0.57f, 0.24f));

        if (namekPlanet == null)
            namekPlanet = CreateWorldSprite("NamekPlanet", namekPlanetSprite, new Vector2(0.78f, 0.76f), 2.2f, -9);
        if (spacePlanet == null)
            spacePlanet = CreateWorldSprite("SpacePlanet", spacePlanetSprite, new Vector2(0.18f, 0.78f), 3.0f, -9);

        if (spaceStars.Count == 0)
        {
            for (int i = 0; i < 95; i++)
            {
                float x = ((i * 73) % 997) / 997f;
                float y = ((i * 193 + 41) % 991) / 991f;
                SpriteRenderer star = CreateWorldSprite("SpaceStar_" + i, starSprite, new Vector2(x, y), 0.035f + (i % 4) * 0.012f, -8);
                spaceStars.Add(star);
            }
        }
    }

    private SpriteRenderer CreateWorldSprite(string objectName, Sprite sprite, Vector2 viewportPosition, float size, int order)
    {
        GameObject visual = new GameObject(objectName);
        visual.transform.SetParent(levelVisuals, false);
        if (Camera.main != null)
        {
            Vector3 position = Camera.main.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, 10f));
            position.z = 0f;
            visual.transform.position = position;
        }
        visual.transform.localScale = Vector3.one * size;
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    private static Sprite CreateCircleSprite(int resolution, Color innerColor, Color outerColor)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((resolution - 1) * 0.5f, (resolution - 1) * 0.5f);
        float radius = resolution * 0.47f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                if (distance > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float band = Mathf.Clamp01(0.5f + 0.30f * Mathf.Sin(y * 0.28f) + 0.20f * (1f - distance));
                texture.SetPixel(x, y, Color.Lerp(outerColor, innerColor, band));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), resolution * 0.5f);
    }
}

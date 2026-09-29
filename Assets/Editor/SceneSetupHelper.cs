using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SceneSetupHelper
{
    static SceneSetupHelper()
    {
        EditorApplication.delayCall += SetupScene;
    }

    [MenuItem("Tools/Cài Đặt Toàn Bộ Scene & Background (1-Click)")]
    public static void SetupScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        // 1. Kiểm tra hoặc tạo BackgroundManager
        BackgroundManager bgMgr = Object.FindFirstObjectByType<BackgroundManager>();
        if (bgMgr == null)
        {
            GameObject bgGo = new GameObject("BackgroundManager");
            bgGo.transform.position = Vector3.zero;
            bgMgr = bgGo.AddComponent<BackgroundManager>();
            Debug.Log("[SceneSetupHelper] Đã tạo mới GameObject BackgroundManager trong Scene.");
        }

        bgMgr.AutoLoadSprites();
        bgMgr.UpdateBoundaries();
        bgMgr.SetupPieces();

        // 2. Đồng bộ PlayerA
        PlayerA playerA = Object.FindFirstObjectByType<PlayerA>();
        if (playerA == null)
        {
            GameObject playerGo = new GameObject("PlayerA");
            playerA = playerGo.AddComponent<PlayerA>();
            CharacterParts cParts = playerA.GetComponent<CharacterParts>();
            
            // Tự động tạo 3 bộ phận Head, Body, Leg để không bị mất hình
            GameObject h = new GameObject("Head");
            h.transform.SetParent(playerGo.transform);
            h.transform.localPosition = Vector3.zero;
            SpriteRenderer hSr = h.AddComponent<SpriteRenderer>();
            hSr.sortingOrder = 5;

            GameObject b = new GameObject("Body");
            b.transform.SetParent(playerGo.transform);
            b.transform.localPosition = Vector3.zero;
            SpriteRenderer bSr = b.AddComponent<SpriteRenderer>();
            bSr.sortingOrder = 4;

            GameObject l = new GameObject("Leg");
            l.transform.SetParent(playerGo.transform);
            l.transform.localPosition = Vector3.zero;
            SpriteRenderer lSr = l.AddComponent<SpriteRenderer>();
            lSr.sortingOrder = 3;

            cParts.headRenderer = hSr;
            cParts.bodyRenderer = bSr;
            cParts.legRenderer = lSr;
            
            Debug.Log("[SceneSetupHelper] Đã tự động tạo mới PlayerA cùng các bộ phận Head/Body/Leg trong Scene.");
        }
        
        if (playerA != null)
        {
            float targetGround = BackgroundManager.GroundY;
            Vector3 pos = playerA.transform.position;
            pos.y = targetGround;
            playerA.transform.position = pos;
            playerA.headHeight = 1.5375f;
            playerA.characterScale = 0.2f;
            CharacterParts cp = playerA.GetComponent<CharacterParts>();
            if (cp != null)
            {
                // Kiểm tra và tạo lại các phần thân thể nếu chúng bị mất
                if (cp.headRenderer == null)
                {
                    Transform t = playerA.transform.Find("Head");
                    GameObject h = t != null ? t.gameObject : new GameObject("Head");
                    h.transform.SetParent(playerA.transform, false);
                    cp.headRenderer = h.GetComponent<SpriteRenderer>();
                    if (cp.headRenderer == null) cp.headRenderer = h.AddComponent<SpriteRenderer>();
                    cp.headRenderer.sortingOrder = 5;
                }
                if (cp.bodyRenderer == null)
                {
                    Transform t = playerA.transform.Find("Body");
                    GameObject b = t != null ? t.gameObject : new GameObject("Body");
                    b.transform.SetParent(playerA.transform, false);
                    cp.bodyRenderer = b.GetComponent<SpriteRenderer>();
                    if (cp.bodyRenderer == null) cp.bodyRenderer = b.AddComponent<SpriteRenderer>();
                    cp.bodyRenderer.sortingOrder = 4;
                }
                if (cp.legRenderer == null)
                {
                    Transform t = playerA.transform.Find("Leg");
                    GameObject l = t != null ? t.gameObject : new GameObject("Leg");
                    l.transform.SetParent(playerA.transform, false);
                    cp.legRenderer = l.GetComponent<SpriteRenderer>();
                    if (cp.legRenderer == null) cp.legRenderer = l.AddComponent<SpriteRenderer>();
                    cp.legRenderer.sortingOrder = 3;
                }

                cp.headHeight = 1.5375f;
                cp.characterScale = 0.2f;
                cp.SyncAndApplyAll();
            }

            // Tự động gán toàn bộ sprites cho A và B
            playerA.AutoAssignSprites();
            
            // Gán trực tiếp sprite lên màn hình Editor để PlayerA hiển thị ngay, không bị tàng hình
            if (cp != null)
            {
                cp.headRenderer.sprite = playerA.headIdle;
                cp.bodyRenderer.sprite = playerA.bodyIdle;
                cp.legRenderer.sprite = playerA.legIdle;
            }

            // Gán đạn Projectile_C
            if (playerA.projectileCPrefab == null)
            {
                playerA.projectileCPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Projectile_C.prefab");
            }

            // Đảm bảo thư mục Assets/Prefabs tồn tại
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Đảm bảo đủ số lượng Enemy B trong Scene
            playerA.EnsureEnemyBCount();

            // Tạo Prefab Enemy_B.prefab từ đối tượng Enemy B đầu tiên nếu chưa có
            EnemyB firstB = Object.FindFirstObjectByType<EnemyB>();
            if (firstB != null)
            {
                string prefabPath = "Assets/Prefabs/Enemy_B.prefab";
                GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existingPrefab == null)
                {
                    PrefabUtility.SaveAsPrefabAsset(firstB.gameObject, prefabPath);
                    AssetDatabase.Refresh();
                }
                playerA.enemyBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            // Đảm bảo UI Canvas, D-Pad lớn và nút chưởng lưu cứng vào Scene
            playerA.EnsureAttackButton();
            SetupManagersAndHUD();

            EditorUtility.SetDirty(playerA);
        }

        // 3. Đồng bộ và lưu toàn bộ EnemyB trong Scene
        EnemyB[] enemies = Object.FindObjectsByType<EnemyB>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (e != null)
            {
                e.headHeight = 1.1375f;
                e.characterScale = 0.2f;
                CharacterParts cp = e.GetComponent<CharacterParts>();
                if (cp != null)
                {
                    cp.headHeight = 1.1375f;
                    cp.characterScale = 0.2f;
                    cp.SyncAndApplyAll();
                }
                e.AutoAssignSprites();
                EditorUtility.SetDirty(e);
            }
        }

        // 4. Kiểm tra hoặc tạo ObstacleManager
        ObstacleManager obsMgr = Object.FindFirstObjectByType<ObstacleManager>();
        if (obsMgr == null)
        {
            GameObject obsGo = new GameObject("ObstacleManager");
            obsGo.transform.position = Vector3.zero;
            obsMgr = obsGo.AddComponent<ObstacleManager>();
            Debug.Log("[SceneSetupHelper] Đã tạo mới GameObject ObstacleManager trong Scene.");
        }
        obsMgr.AutoLoadSprite();
        EditorUtility.SetDirty(obsMgr);

        EditorUtility.SetDirty(bgMgr);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("===> [SceneSetupHelper] CÀI ĐẶT TOÀN BỘ SCENE, UI D-PAD LỚN VÀ ENEMY B THÀNH CÔNG! <===");
    }

    [MenuItem("Tools/Build Game ra File Chạy .exe (Windows 64-bit)")]
    public static void BuildStandaloneWindows()
    {
        string buildDir = "E:/solo/Build_Game";
        if (!System.IO.Directory.Exists(buildDir))
        {
            System.IO.Directory.CreateDirectory(buildDir);
        }

        string exePath = System.IO.Path.Combine(buildDir, "Lab_NRO_2D.exe");
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log("[Build] Đang tiến hành build game ra: " + exePath);
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("===> BUILD GAME THÀNH CÔNG! File chạy: " + exePath);
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError("[Build] Build game thất bại với kết quả: " + report.summary.result);
        }
    }

    [MenuItem("Tools/Xuất Toàn Bộ Dự Án Thành 1 File .unitypackage")]
    public static void ExportUnityPackage()
    {
        string exportPath = "E:/solo/Lab_NRO_2D_FullProject.unitypackage";
        string[] assetPaths = new string[] { "Assets" };
        AssetDatabase.ExportPackage(assetPaths, exportPath, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
        Debug.Log("===> ĐÃ XUẤT THÀNH CÔNG FILE UNITY PACKAGE: " + exportPath);
        EditorUtility.RevealInFinder(exportPath);
    }

    [MenuItem("Tools/Build Game ra File Cài Đặt .apk (Android)")]
    public static void BuildAndroidAPK()
    {
        // Tự động chuẩn bị toàn bộ Scene trước khi build
        SetupScene();

        // 1. Cấu hình Package Name hợp lệ cho Android
        string currentId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        if (string.IsNullOrEmpty(currentId) || currentId.Contains("DefaultCompany"))
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.solo.nro2d");
        }

        // 2. Cố định màn hình ngang (Landscape)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        string buildDir = "E:/solo/Build_Android";
        if (!System.IO.Directory.Exists(buildDir))
        {
            System.IO.Directory.CreateDirectory(buildDir);
        }

        string apkPath = System.IO.Path.Combine(buildDir, "Lab_NRO_2D.apk");
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log("[Build Android] Đang tiến hành build file APK ra: " + apkPath);
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("===> BUILD FILE APK THÀNH CÔNG! File cài đặt: " + apkPath);
            EditorUtility.RevealInFinder(apkPath);
        }
        else
        {
            Debug.LogError("[Build Android] Build APK chưa thành công: " + report.summary.result);
        }
    }

    private static void SetupManagersAndHUD()
    {
        // 1. Tạo GameManager
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            Debug.Log("[SceneSetupHelper] Đã tạo GameManager.");
        }

        // 2. Tạo AudioManager
        AudioManager am = Object.FindFirstObjectByType<AudioManager>();
        if (am == null)
        {
            GameObject amObj = new GameObject("AudioManager");
            amObj.AddComponent<AudioManager>();
            Debug.Log("[SceneSetupHelper] Đã tạo AudioManager.");
        }

        // 3. Tạo ItemSpawner
        ItemSpawner sp = Object.FindFirstObjectByType<ItemSpawner>();
        if (sp == null)
        {
            GameObject spObj = new GameObject("ItemSpawner");
            spObj.AddComponent<ItemSpawner>();
            Debug.Log("[SceneSetupHelper] Đã tạo ItemSpawner.");
        }

        // 3b. Tạo LevelManager, GameOverUI, WinUI
        if (Object.FindFirstObjectByType<LevelManager>() == null)
        {
            GameObject lmObj = new GameObject("LevelManager");
            lmObj.AddComponent<LevelManager>();
        }
        if (Object.FindFirstObjectByType<GameOverUI>() == null)
        {
            GameObject goObj = new GameObject("GameOverUI");
            goObj.AddComponent<GameOverUI>();
        }
        if (Object.FindFirstObjectByType<WinUI>() == null)
        {
            GameObject winObj = new GameObject("WinUI");
            winObj.AddComponent<WinUI>();
        }

        // 4. Tạo HUDManager và các element giao diện
        HUDManager hud = Object.FindFirstObjectByType<HUDManager>();
        if (hud != null)
        {
            Object.DestroyImmediate(hud.gameObject);
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {

            GameObject hudObj = new GameObject("HUDManager");
            hudObj.transform.SetParent(canvas.transform, false);
            RectTransform hudObjRt = hudObj.AddComponent<RectTransform>();
            hudObjRt.anchorMin = Vector2.zero;
            hudObjRt.anchorMax = Vector2.one;
            hudObjRt.sizeDelta = Vector2.zero;
            hudObjRt.anchoredPosition = Vector2.zero;
            hud = hudObj.AddComponent<HUDManager>();

            // --- TẠO THANH HP ---
            GameObject hpBg = new GameObject("HP_Bg");
            hpBg.transform.SetParent(hudObj.transform, false);
            UnityEngine.UI.Image hpBgImg = hpBg.AddComponent<UnityEngine.UI.Image>();
            hpBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Màu xám tối (Frame)
            RectTransform hpBgRt = hpBg.GetComponent<RectTransform>();
            hpBgRt.anchorMin = new Vector2(0, 1);
            hpBgRt.anchorMax = new Vector2(0, 1);
            hpBgRt.pivot = new Vector2(0, 1);
            hpBgRt.anchoredPosition = new Vector2(20, -20);
            hpBgRt.sizeDelta = new Vector2(300, 30);

            GameObject hpFill = new GameObject("HP_Fill");
            hpFill.transform.SetParent(hpBg.transform, false);
            UnityEngine.UI.Image hpFillImg = hpFill.AddComponent<UnityEngine.UI.Image>();
            hpFillImg.color = Color.red;
            // Dùng neo (anchors) thay vì fillAmount để tự scale, không cần sprite!
            RectTransform hpFillRt = hpFill.GetComponent<RectTransform>();
            hpFillRt.anchorMin = new Vector2(0f, 0f);
            hpFillRt.anchorMax = new Vector2(1f, 1f);
            hpFillRt.pivot = new Vector2(0f, 0.5f);
            hpFillRt.offsetMin = new Vector2(2f, 2f); // Thụt vào 2px
            hpFillRt.offsetMax = new Vector2(-2f, -2f);
            hud.hpBar = hpFillImg;

            GameObject hpTextObj = new GameObject("HP_Text");
            hpTextObj.transform.SetParent(hpBg.transform, false);
            UnityEngine.UI.Text hpTxt = hpTextObj.AddComponent<UnityEngine.UI.Text>();
            hpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpTxt.fontSize = 20;
            hpTxt.color = Color.white;
            hpTxt.alignment = TextAnchor.MiddleCenter;
            hpTxt.text = "HP: 100/100";
            RectTransform hpTxtRt = hpTextObj.GetComponent<RectTransform>();
            hpTxtRt.anchorMin = Vector2.zero;
            hpTxtRt.anchorMax = Vector2.one;
            hpTxtRt.sizeDelta = Vector2.zero;
            hud.hpText = hpTxt; 

            // --- TẠO THANH KI ---
            GameObject kiBg = new GameObject("Ki_Bg");
            kiBg.transform.SetParent(hudObj.transform, false);
            UnityEngine.UI.Image kiBgImg = kiBg.AddComponent<UnityEngine.UI.Image>();
            kiBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform kiBgRt = kiBg.GetComponent<RectTransform>();
            kiBgRt.anchorMin = new Vector2(0, 1);
            kiBgRt.anchorMax = new Vector2(0, 1);
            kiBgRt.pivot = new Vector2(0, 1);
            kiBgRt.anchoredPosition = new Vector2(20, -60);
            kiBgRt.sizeDelta = new Vector2(250, 25);

            GameObject kiFill = new GameObject("Ki_Fill");
            kiFill.transform.SetParent(kiBg.transform, false);
            UnityEngine.UI.Image kiFillImg = kiFill.AddComponent<UnityEngine.UI.Image>();
            kiFillImg.color = Color.blue;
            RectTransform kiFillRt = kiFill.GetComponent<RectTransform>();
            kiFillRt.anchorMin = new Vector2(0f, 0f);
            kiFillRt.anchorMax = new Vector2(1f, 1f);
            kiFillRt.pivot = new Vector2(0f, 0.5f);
            kiFillRt.offsetMin = new Vector2(2f, 2f);
            kiFillRt.offsetMax = new Vector2(-2f, -2f);
            hud.kiBar = kiFillImg;

            GameObject kiTextObj = new GameObject("Ki_Text");
            kiTextObj.transform.SetParent(kiBg.transform, false);
            UnityEngine.UI.Text kiTxt = kiTextObj.AddComponent<UnityEngine.UI.Text>();
            kiTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            kiTxt.fontSize = 18;
            kiTxt.color = Color.white;
            kiTxt.alignment = TextAnchor.MiddleCenter;
            kiTxt.text = "KI: 100/100";
            RectTransform kiTxtRt = kiTextObj.GetComponent<RectTransform>();
            kiTxtRt.anchorMin = Vector2.zero;
            kiTxtRt.anchorMax = Vector2.one;
            kiTxtRt.sizeDelta = Vector2.zero;
            hud.kiText = kiTxt; // Cần thêm biến kiText vào HUDManager

            // --- TẠO ĐIỂM SỐ (TOP-RIGHT) ---
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(hudObj.transform, false);
            UnityEngine.UI.Text scoreTxt = scoreObj.AddComponent<UnityEngine.UI.Text>();
            scoreTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreTxt.fontSize = 28;
            scoreTxt.color = Color.yellow;
            scoreTxt.alignment = TextAnchor.MiddleRight;
            scoreTxt.text = "SCORE: 0";
            RectTransform scoreRt = scoreObj.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(1, 1);
            scoreRt.anchorMax = new Vector2(1, 1);
            scoreRt.pivot = new Vector2(1, 1);
            scoreRt.anchoredPosition = new Vector2(-20, -70); // Xuống dưới các nút âm thanh một chút
            scoreRt.sizeDelta = new Vector2(300, 40);
            hud.scoreText = scoreTxt;

            GameObject highScoreObj = new GameObject("HighScoreText");
            highScoreObj.transform.SetParent(hudObj.transform, false);
            UnityEngine.UI.Text highScoreTxt = highScoreObj.AddComponent<UnityEngine.UI.Text>();
            highScoreTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            highScoreTxt.fontSize = 20;
            highScoreTxt.color = new Color(1f, 0.5f, 0f); // Màu cam
            highScoreTxt.alignment = TextAnchor.MiddleRight;
            highScoreTxt.text = "HIGH SCORE: 0";
            RectTransform highScoreRt = highScoreObj.GetComponent<RectTransform>();
            highScoreRt.anchorMin = new Vector2(1, 1);
            highScoreRt.anchorMax = new Vector2(1, 1);
            highScoreRt.pivot = new Vector2(1, 1);
            highScoreRt.anchoredPosition = new Vector2(-20, -110);
            highScoreRt.sizeDelta = new Vector2(300, 40);
            hud.highScoreText = highScoreTxt; // Cần thêm biến highScoreText vào HUDManager

            // --- TẠO NÚT SOUND & MUSIC ---
            GameObject soundBtnObj = CreateAudioButton("SoundBtn", "SOUND ON", new Vector2(-20, -20), hudObj.transform);
            hud.soundButton = soundBtnObj.GetComponent<UnityEngine.UI.Button>();
            hud.soundButton.onClick.AddListener(hud.ToggleSound);

            GameObject musicBtnObj = CreateAudioButton("MusicBtn", "MUSIC ON", new Vector2(-180, -20), hudObj.transform);
            hud.musicButton = musicBtnObj.GetComponent<UnityEngine.UI.Button>();
            hud.musicButton.onClick.AddListener(hud.ToggleMusic);
            
            Debug.Log("[SceneSetupHelper] Đã tạo HUDManager (Máu, Ki, Điểm, Âm thanh).");
        }
    }

    private static GameObject CreateAudioButton(string name, string text, Vector2 anchoredPos, Transform parent)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        UnityEngine.UI.Image bg = btnObj.AddComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(150, 40);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = text;

        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        return btnObj;
    }
}

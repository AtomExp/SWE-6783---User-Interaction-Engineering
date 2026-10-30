using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// One-click setup for the gameplay HUD and the Score Dashboard (menu: Think N Sync).
// Everything it creates is ordinary UI that can be restyled by hand afterwards.
public static class GameplaySetupMenu
{
    const string ScenesFolder = "Assets/Scenes";
    const string FontPath = "Assets/UI/Fonts/Baloo2-ExtraBold SDF.asset";
    const string TitleMaterialPath = "Assets/UI/Fonts/Baloo2-ExtraBold SDF - Title.mat";
    const string BackgroundPath = "Assets/Sprites/ThinkNSync_Background.png";

    // Sprites drawn by UIArtGenerator (already in the repo), so these screens match the menus.
    const string GoldPill = "Assets/UI/Generated/Pill_FFD43B.png";
    const string BluePill = "Assets/UI/Generated/Pill_3F95FF.png";
    const string OrangePanel = "Assets/UI/Generated/Panel_FFA53A.png";
    const string CardSprite = "Assets/UI/Generated/Card.png";

    static readonly Color Navy = new Color32(0x1B, 0x2A, 0x5E, 0xFF);
    static readonly Color Gold = new Color32(0xFF, 0xD4, 0x3B, 0xFF);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);

    // Order of the build list. Scenes not listed here are added after these.
    static readonly string[] SceneOrder =
    {
        SceneNames.MainMenu, SceneNames.DifficultySelection, SceneNames.Instructions, SceneNames.StudyPhase,
        SceneNames.GameLevel, SceneNames.ScoreDashboard, SceneNames.Options
    };

    // ---------- Build list ----------

    [MenuItem("Think N Sync/Add All Scenes To Build Settings")]
    public static void AddScenesToBuildSettings()
    {
        var paths = Directory.GetFiles(ScenesFolder, "*.unity", SearchOption.TopDirectoryOnly)
            .Select(p => p.Replace('\\', '/'))
            .ToList();

        // Keep each scene's enabled/disabled choice; drop entries whose scene file is gone.
        var enabled = EditorBuildSettings.scenes
            .Where(s => File.Exists(s.path))
            .GroupBy(s => s.path)
            .ToDictionary(g => g.Key, g => g.First().enabled);

        int Order(string path)
        {
            int i = System.Array.IndexOf(SceneOrder, Path.GetFileNameWithoutExtension(path));
            return i < 0 ? int.MaxValue : i;
        }

        EditorBuildSettings.scenes = paths
            .OrderBy(Order).ThenBy(p => p)
            .Select(p => new EditorBuildSettingsScene(p, !enabled.TryGetValue(p, out bool on) || on))
            .ToArray();

        Debug.Log("Build scenes: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
    }

    // ---------- Gameplay ----------

    [MenuItem("Think N Sync/Add Gameplay To Open Scene")]
    public static void AddGameplayToOpenScene()
    {
        var board = Object.FindFirstObjectByType<MemoryBoard>();
        if (board == null)
        {
            EditorUtility.DisplayDialog("Add Gameplay", "Open the scene with the MemoryBoard (GameLevel) first.", "OK");
            return;
        }
        if (Object.FindFirstObjectByType<GameManager>() != null)
        {
            EditorUtility.DisplayDialog("Add Gameplay", "This scene already has a GameManager.", "OK");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Add Gameplay", "No Canvas found in this scene.", "OK");
            return;
        }
        EnsureEventSystem();

        var root = Stretch("Gameplay UI", canvas.transform, Vector2.zero, Vector2.one);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Add Gameplay");

        // HUD along the top, timer on the right, pairs counter at the bottom.
        var hud = Stretch("HUD", root, Vector2.zero, Vector2.one);
        var hudComponent = hud.gameObject.AddComponent<HUDController>();
        var score = CreateText("Score", hud, "Score: 0", 36, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.88f), new Vector2(0.4f, 0.98f), true);
        var level = CreateText("Level", hud, "Level 1", 40, TextAlignmentOptions.Top, new Vector2(0.4f, 0.9f), new Vector2(0.6f, 0.99f), true);
        var difficulty = CreateText("Difficulty", hud, "Easy", 24, TextAlignmentOptions.Top, new Vector2(0.4f, 0.86f), new Vector2(0.6f, 0.91f), false);
        var lives = CreateText("Lives", hud, "Lives: 5", 36, TextAlignmentOptions.TopRight, new Vector2(0.6f, 0.88f), new Vector2(0.98f, 0.98f), true);
        var message = CreateText("Message", hud, "Memorize the cards!", 30, TextAlignmentOptions.Center, new Vector2(0.15f, 0.8f), new Vector2(0.85f, 0.87f), true);
        var timer = CreateText("Timer", hud, "10", 72, TextAlignmentOptions.Center, new Vector2(0.86f, 0.42f), new Vector2(0.99f, 0.58f), true);
        timer.color = Gold;
        var pairs = CreateText("Pairs", hud, "Pairs: 0/3", 30, TextAlignmentOptions.Center, new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.07f), true);

        Assign(hudComponent, "scoreText", score);
        Assign(hudComponent, "levelText", level);
        Assign(hudComponent, "difficultyText", difficulty);
        Assign(hudComponent, "livesText", lives);
        Assign(hudComponent, "messageText", message);
        Assign(hudComponent, "timerText", timer);
        Assign(hudComponent, "pairsText", pairs);

        // Game over panel: the controller stays active, the panel itself starts hidden.
        var gameOver = Stretch("Game Over", root, Vector2.zero, Vector2.one);
        var gameOverComponent = gameOver.gameObject.AddComponent<GameOverPanel>();
        var overlay = Stretch("Panel", gameOver, Vector2.zero, Vector2.one);
        CreateImage(overlay.gameObject, null, Dim, false);
        var card = Stretch("Card", overlay, new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.9f));
        CreateImage(card.gameObject, CardSprite, Color.white, true);
        var title = CreateText("Title", card, "GAME OVER", 64, TextAlignmentOptions.Center, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.97f), true);
        title.color = Color.white;
        var badge = CreateText("New Best", card, "NEW BEST!", 34, TextAlignmentOptions.Center, new Vector2(0f, 0.75f), new Vector2(1f, 0.83f), true);
        badge.color = Gold;
        var finalScore = CreateText("Final Score", card, "Score: 0", 48, TextAlignmentOptions.Center, new Vector2(0f, 0.64f), new Vector2(1f, 0.75f), false);
        finalScore.color = Navy;
        var best = CreateText("Best Score", card, "Best: 0", 28, TextAlignmentOptions.Center, new Vector2(0f, 0.57f), new Vector2(1f, 0.64f), false);
        best.color = Navy;
        var details = CreateText("Details", card, "", 24, TextAlignmentOptions.Center, new Vector2(0.05f, 0.39f), new Vector2(0.95f, 0.57f), false);
        details.color = Navy;
        var playAgain = CreateButton("Play Again Button", card, "PLAY AGAIN", GoldPill, Navy, new Vector2(0.12f, 0.26f), new Vector2(0.88f, 0.36f));
        var dashboard = CreateButton("Score Dashboard Button", card, "SCORES", BluePill, Color.white, new Vector2(0.12f, 0.145f), new Vector2(0.88f, 0.245f));
        var menu = CreateButton("Main Menu Button", card, "MAIN MENU", BluePill, Color.white, new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.13f));

        Assign(gameOverComponent, "panelRoot", overlay.gameObject);
        Assign(gameOverComponent, "finalScoreText", finalScore);
        Assign(gameOverComponent, "bestScoreText", best);
        Assign(gameOverComponent, "detailsText", details);
        Assign(gameOverComponent, "newBestBadge", badge.gameObject);
        Assign(gameOverComponent, "playAgainButton", playAgain);
        Assign(gameOverComponent, "scoreDashboardButton", dashboard);
        Assign(gameOverComponent, "mainMenuButton", menu);
        overlay.gameObject.SetActive(false);

        var managerObject = new GameObject("GameManager");
        Undo.RegisterCreatedObjectUndo(managerObject, "Add Gameplay");
        var manager = managerObject.AddComponent<GameManager>();
        Assign(manager, "board", board);
        Assign(hudComponent, "manager", manager);
        Assign(gameOverComponent, "manager", manager);

        // GameManager builds each level; the board shouldn't also build its default 5x4 layout.
        var boardSettings = new SerializedObject(board);
        boardSettings.FindProperty("buildOnStart").boolValue = false;
        boardSettings.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        Selection.activeGameObject = managerObject;
        Debug.Log("Gameplay added. Save the scene, then press Play.");
    }

    // ---------- Score Dashboard ----------

    [MenuItem("Think N Sync/Create Score Dashboard Scene")]
    public static void CreateScoreDashboardScene()
    {
        string path = $"{ScenesFolder}/{SceneNames.ScoreDashboard}.unity";
        if (File.Exists(path) && !EditorUtility.DisplayDialog("Score Dashboard", $"{path} already exists. Replace it?", "Replace", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(0x12, 0x36, 0x22, 0xFF);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var canvas = canvasObject.transform;
        EnsureEventSystem();

        var background = Stretch("Background", canvas, Vector2.zero, Vector2.one);
        CreateImage(background.gameObject, BackgroundPath, Color.white, false);

        var screen = Stretch("Score Dashboard", canvas, Vector2.zero, Vector2.one);
        var controller = screen.gameObject.AddComponent<ScoreDashboardController>();

        var banner = Stretch("Banner", screen, new Vector2(0.28f, 0.84f), new Vector2(0.72f, 0.97f));
        CreateImage(banner.gameObject, OrangePanel, Color.white, true);
        CreateText("Title", banner, "SCORE DASHBOARD", 72, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, true);

        TMP_Text easyBest, easyGames, hardBest, hardGames;
        BestCard(screen, "Easy", new Vector2(0.05f, 0.56f), new Vector2(0.3f, 0.8f), out easyBest, out easyGames);
        BestCard(screen, "Hard", new Vector2(0.05f, 0.28f), new Vector2(0.3f, 0.52f), out hardBest, out hardGames);

        var all = CreateButton("All Button", screen, "ALL", GoldPill, Navy, new Vector2(0.34f, 0.72f), new Vector2(0.44f, 0.8f));
        var easy = CreateButton("Easy Button", screen, "EASY", GoldPill, Navy, new Vector2(0.45f, 0.72f), new Vector2(0.55f, 0.8f));
        var hard = CreateButton("Hard Button", screen, "HARD", GoldPill, Navy, new Vector2(0.56f, 0.72f), new Vector2(0.66f, 0.8f));
        var recent = CreateButton("Recent Button", screen, "RECENT", BluePill, Color.white, new Vector2(0.72f, 0.72f), new Vector2(0.83f, 0.8f));
        var top = CreateButton("Top Button", screen, "TOP", BluePill, Color.white, new Vector2(0.84f, 0.72f), new Vector2(0.95f, 0.8f));

        var list = Stretch("History", screen, new Vector2(0.34f, 0.14f), new Vector2(0.95f, 0.7f));
        CreateImage(list.gameObject, CardSprite, Color.white, true);
        var scroll = list.gameObject.AddComponent<ScrollRect>();
        var viewport = Stretch("Viewport", list, Vector2.zero, Vector2.one);
        viewport.offsetMin = new Vector2(24f, 24f);
        viewport.offsetMax = new Vector2(-24f, -24f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Stretch("Content", viewport, new Vector2(0f, 1f), Vector2.one);
        content.pivot = new Vector2(0.5f, 1f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var empty = CreateText("Empty", list, "No games played yet.\nPlay a round to see your scores here!", 34,
            TextAlignmentOptions.Center, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.65f), false);
        empty.color = Navy;

        var back = CreateButton("Back Button", screen, "BACK", BluePill, Color.white, new Vector2(0.05f, 0.03f), new Vector2(0.22f, 0.12f));
        var clear = CreateButton("Clear Button", screen, "CLEAR HISTORY", BluePill, Color.white, new Vector2(0.75f, 0.03f), new Vector2(0.95f, 0.12f));

        Assign(controller, "easyBestText", easyBest);
        Assign(controller, "easyGamesText", easyGames);
        Assign(controller, "hardBestText", hardBest);
        Assign(controller, "hardGamesText", hardGames);
        Assign(controller, "historyContent", content);
        Assign(controller, "emptyText", empty);
        Assign(controller, "allButton", all);
        Assign(controller, "easyButton", easy);
        Assign(controller, "hardButton", hard);
        Assign(controller, "recentButton", recent);
        Assign(controller, "topButton", top);
        Assign(controller, "backButton", back);
        Assign(controller, "clearHistoryButton", clear);

        EditorSceneManager.SaveScene(scene, path);
        AddScenesToBuildSettings();
        Debug.Log($"Created {path}.");
    }

    static void BestCard(Transform parent, string mode, Vector2 min, Vector2 max, out TMP_Text best, out TMP_Text games)
    {
        var card = Stretch($"{mode} Best", parent, min, max);
        CreateImage(card.gameObject, CardSprite, Color.white, true);
        var label = CreateText("Label", card, $"BEST - {mode.ToUpper()}", 34, TextAlignmentOptions.Center, new Vector2(0f, 0.66f), new Vector2(1f, 0.95f), false);
        label.color = Navy;
        best = CreateText("Score", card, "--", 48, TextAlignmentOptions.Center, new Vector2(0f, 0.3f), new Vector2(1f, 0.66f), true);
        games = CreateText("Games", card, "0 games", 26, TextAlignmentOptions.Center, new Vector2(0f, 0.06f), new Vector2(1f, 0.3f), false);
        games.color = Navy;
    }

    // ---------- Helpers ----------

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        // The project uses the Input System package only, so the UI needs InputSystemUIInputModule.
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    static RectTransform Stretch(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    static Image CreateImage(GameObject go, string spritePath, Color color, bool sliced)
    {
        var image = go.AddComponent<Image>();
        image.sprite = spritePath != null ? AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) : null;
        image.color = color;
        if (sliced && image.sprite != null)
            image.type = Image.Type.Sliced;
        return image;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax, bool outlined)
    {
        var rect = Stretch(name, parent, anchorMin, anchorMax);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null)
            label.font = font;
        if (outlined)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath);
            if (material != null)
                label.fontSharedMaterial = material;
        }
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    static Button CreateButton(string name, Transform parent, string text, string spritePath, Color textColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rect = Stretch(name, parent, anchorMin, anchorMax);
        var image = CreateImage(rect.gameObject, spritePath, Color.white, true);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        rect.gameObject.AddComponent<ButtonBounce>();

        var label = CreateText("Label", rect, text, 34, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, textColor == Color.white);
        label.color = textColor;
        return button;
    }

    static void Assign(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"{target.GetType().Name} has no field '{field}'.");
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

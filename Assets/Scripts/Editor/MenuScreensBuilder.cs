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

// Builds the Difficulty Selection and Instructions scenes from the mockups
// (Think N Sync > Build Difficulty and Instructions Screens).
// Running it again regenerates both scenes and overwrites any manual edits made to them.
public static class MenuScreensBuilder
{
    const string ScenesFolder = "Assets/Scenes";
    const string BackgroundPath = "Assets/Sprites/ThinkNSync_Background.png";
    const string LogoPath = "Assets/Sprites/ThinkNSync_Logo.png";
    const string FontPath = "Assets/UI/Fonts/Baloo2-ExtraBold SDF.asset";
    const string TitleMaterialPath = "Assets/UI/Fonts/Baloo2-ExtraBold SDF - Title.mat";

    // Kept separate from Kavya's DifficultySelection scene so rebuilding never overwrites hers.
    const string DifficultySceneName = "DifficultySelection_Yasoda";

    static readonly Vector2 ReferenceResolution = new(1920, 1080);
    const float BackgroundAspect = 1672f / 941f;

    // Colours picked from the mockups.
    static readonly Color Navy = Hex("1B2A5E");
    static readonly Color BodyBlue = Hex("1E3E86");
    static readonly Color Gold = Hex("FFD43B");
    static readonly Color Yellow = Hex("FFE066");
    static readonly Color Orange = Hex("FFA53A");
    static readonly Color Green = Hex("6BF23F");
    static readonly Color Red = Hex("FF4A50");
    static readonly Color Blue = Hex("3F95FF");
    static readonly Color ShapeRed = Hex("F0384A");
    static readonly Color ShapeGreen = Hex("4CD13A");
    static readonly Color ShapeBlue = Hex("3D8BFF");
    static readonly Color ShapePurple = Hex("A04CF0");
    static readonly Color ShapeOrange = Hex("FF8A1F");

    static TMP_FontAsset font;
    static Material titleMaterial;

    [MenuItem("Think N Sync/Build Difficulty and Instructions Screens")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog("Build menu screens",
                $"This regenerates the {DifficultySceneName} and Instructions scenes. Any manual edits to those two scenes will be overwritten.",
                "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        BuildAll();
        EditorSceneManager.OpenScene($"{ScenesFolder}/{DifficultySceneName}.unity");
    }

    // Also callable from the command line: -executeMethod MenuScreensBuilder.BuildAll
    public static void BuildAll()
    {
        UIArtGenerator.Reset();
        MakeSprite(BackgroundPath);
        MakeSprite(LogoPath);
        LoadFonts();

        BuildDifficultyScene();
        BuildInstructionsScene();
        AddScenesToBuild();
        AssetDatabase.SaveAssets();
    }

    // ---------- Difficulty Selection ----------

    static void BuildDifficultyScene()
    {
        var canvas = NewScreenScene(out var eventSystem);

        Logo(canvas, new Vector2(0, 400), new Vector2(690, 230));
        Tagline(canvas, new Vector2(0, 250), new Vector2(600, 66), 32);
        Banner(canvas, "SELECT DIFFICULTY", new Vector2(0, 95), new Vector2(940, 150), 92);

        var easy = DifficultyButton(canvas, "Easy Button", "EASY", "Fewer objects  •  No time limit",
            Green, Hex("1F5E0C"), new Vector2(0, -85), UIArtGenerator.Star);
        var hard = DifficultyButton(canvas, "Hard Button", "HARD", "More objects  •  10-second study",
            Red, Hex("7A0E14"), new Vector2(0, -265), UIArtGenerator.Crown);
        // Small gem on the crown, as in the mockup.
        Img("Gem", hard.transform.Find("Icon"), Glossy(UIArtGenerator.Diamond, ShapeBlue), new Vector2(0, -12), new Vector2(34, 34));

        var back = BackButton(canvas, new Vector2(0, -435));

        var screen = canvas.gameObject.AddComponent<DifficultySelectionScreen>();
        SetRefs(screen, ("easyButton", easy), ("hardButton", hard), ("backButton", back));
        eventSystem.firstSelectedGameObject = easy.gameObject;

        SaveScene(DifficultySceneName);
    }

    static Button DifficultyButton(Transform parent, string name, string title, string subtitle,
        Color color, Color subtitleColor, Vector2 pos, string iconSprite)
    {
        var size = new Vector2(760, 150);
        var button = GlossyButton(name, parent, color, pos, size);
        var t = button.transform;

        Img("Icon Glow", t, Plain(UIArtGenerator.Circle), new Vector2(-270, 2), new Vector2(124, 124), new Color(1, 1, 1, 0.22f));
        Img("Icon", t, Glossy(iconSprite, Gold), new Vector2(-270, 4), new Vector2(116, 116));
        Label("Title", t, title, 76, Color.white, new Vector2(45, 22), new Vector2(500, 84), true);
        Label("Subtitle", t, subtitle, 30, subtitleColor, new Vector2(45, -38), new Vector2(540, 44), false);

        Sparks(parent, pos + new Vector2(-size.x / 2 - 46, 0), color, false);
        Sparks(parent, pos + new Vector2(size.x / 2 + 46, 0), color, true);
        return button;
    }

    // ---------- Instructions ----------

    static void BuildInstructionsScene()
    {
        var canvas = NewScreenScene(out var eventSystem);

        Logo(canvas, new Vector2(-715, 440), new Vector2(440, 147));
        Tagline(canvas, new Vector2(-715, 352), new Vector2(420, 50), 24);
        Banner(canvas, "HOW TO PLAY", new Vector2(170, 405), new Vector2(900, 160), 104);

        // Steps follow the game rules in the project plan.
        var step1 = InstructionCard(canvas, new Vector2(-392, 95), 1, ShapeBlue, "Read the Rule",
            "Each round starts with a matching rule, like: match two objects with the same shape.");
        RuleIllustration(step1);

        var step2 = InstructionCard(canvas, new Vector2(392, 95), 2, ShapeGreen, "Observe the Objects",
            "Remember every object and where it is.\n<color=#2E9E1F>Easy:</color> no time limit, press Ready.\n<color=#D9262E>Hard:</color> 10 seconds, then they hide.");
        ObserveIllustration(step2);

        var step3 = InstructionCard(canvas, new Vector2(-392, -215), 3, ShapePurple, "Select Two Objects",
            "Use the mouse to click two objects that match the rule. Selected objects light up.");
        SelectIllustration(step3);

        var step4 = InstructionCard(canvas, new Vector2(392, -215), 4, ShapeOrange, "Find All Pairs",
            "Correct matches score points. Wrong ones cost a life. Find every pair to clear the level!");
        FinishIllustration(step4);

        var back = BackButton(canvas, new Vector2(0, -450));

        var screen = canvas.gameObject.AddComponent<InstructionsScreen>();
        SetRefs(screen, ("backButton", back));
        eventSystem.firstSelectedGameObject = back.gameObject;

        SaveScene(SceneNames.Instructions);
    }

    static Transform InstructionCard(Transform parent, Vector2 pos, int number, Color badgeColor, string title, string body)
    {
        var card = Sliced($"Step {number}", parent, Plain(UIArtGenerator.Card), pos, new Vector2(750, 290), 64f / 40f).transform;

        Sliced("Title Tab", card, Glossy(UIArtGenerator.Pill, Gold), new Vector2(-20, 118), new Vector2(520, 72), 128f / 72f);
        Label("Title", card, title, 38, Navy, new Vector2(-5, 120), new Vector2(450, 60), false, TextAlignmentOptions.Left);

        Img("Badge Ring", card, Plain(UIArtGenerator.Circle), new Vector2(-345, 118), new Vector2(118, 118));
        Img("Badge", card, Glossy(UIArtGenerator.Disc, badgeColor), new Vector2(-345, 118), new Vector2(104, 104));
        Label("Number", card, number.ToString(), 66, Color.white, new Vector2(-345, 122), new Vector2(100, 100), true);

        var text = Label("Body", card, body, 28, BodyBlue, new Vector2(-160, -38), new Vector2(400, 190), false, TextAlignmentOptions.TopLeft);
        text.enableAutoSizing = true;
        text.fontSizeMin = 20;
        text.fontSizeMax = 28;
        text.lineSpacing = -8;

        return Rect("Illustration", card, new Vector2(205, -30), new Vector2(300, 200));
    }

    static void RuleIllustration(Transform illustration)
    {
        var board = BoardPanel(illustration, new Vector2(290, 190));
        Label("Rule", board, "SAME SHAPE", 32, Gold, new Vector2(0, 52), new Vector2(260, 50), false);
        Img("Star A", board, Glossy(UIArtGenerator.Star, Gold), new Vector2(-72, -28), new Vector2(92, 92));
        Label("Equals", board, "=", 64, Color.white, new Vector2(0, -24), new Vector2(60, 80), true);
        Img("Star B", board, Glossy(UIArtGenerator.Star, ShapeBlue), new Vector2(72, -28), new Vector2(92, 92));
    }

    static void ObserveIllustration(Transform illustration)
    {
        var board = BoardPanel(illustration, new Vector2(300, 200));
        var shapes = new[]
        {
            (UIArtGenerator.Star, Gold), (UIArtGenerator.Heart, ShapeRed), (UIArtGenerator.Triangle, ShapeGreen),
            (UIArtGenerator.Square, ShapeBlue), (UIArtGenerator.Ball, ShapePurple), (UIArtGenerator.Diamond, ShapeOrange)
        };
        for (int i = 0; i < shapes.Length; i++)
        {
            var cell = GridCell(board, i);
            Img(shapes[i].Item1, cell, Glossy(shapes[i].Item1, shapes[i].Item2), Vector2.zero, new Vector2(62, 62));
        }
    }

    static void SelectIllustration(Transform illustration)
    {
        var board = BoardPanel(illustration, new Vector2(300, 200));
        int[] selected = { 0, 4 };
        for (int i = 0; i < 6; i++)
        {
            var cell = GridCell(board, i);
            if (selected.Contains(i))
            {
                Sliced("Selected", cell, Plain(UIArtGenerator.SelectRing), Vector2.zero, new Vector2(104, 104), 1f).color = Gold;
                Img("Star", cell, Glossy(UIArtGenerator.Star, Gold), Vector2.zero, new Vector2(62, 62));
            }
            else
            {
                Label("Hidden", cell, "?", 48, Hex("6FA584"), new Vector2(0, 2), new Vector2(80, 80), false);
            }
        }
        Img("Cursor", board, Plain(UIArtGenerator.Cursor), new Vector2(30, -78), new Vector2(64, 64));
    }

    static void FinishIllustration(Transform illustration)
    {
                Img("Glow", illustration, Plain(UIArtGenerator.Circle), new Vector2(0, 22), new Vector2(190, 190), new Color(1, 0.9f, 0.4f, 0.35f));
        Img("Trophy", illustration, Glossy(UIArtGenerator.Trophy, Gold), new Vector2(0, 20), new Vector2(160, 160));
        Img("Trophy Star", illustration, Glossy(UIArtGenerator.Star, Hex("FFF6B0")), new Vector2(0, 54), new Vector2(46, 46));
        for (int i = 0; i < 3; i++)
            Img("Life", illustration, Glossy(UIArtGenerator.Heart, ShapeRed), new Vector2(-52 + i * 52, -80), new Vector2(44, 44));
    }

    static Transform BoardPanel(Transform parent, Vector2 size) =>
        Sliced("Board", parent, Plain(UIArtGenerator.Board), Vector2.zero, size, 44f / 22f).transform;

    // 3 x 2 grid of tiles inside a board.
    static Transform GridCell(Transform board, int index)
    {
        var pos = new Vector2((index % 3 - 1) * 90, index < 3 ? 44 : -44);
        return Sliced($"Cell {index + 1}", board, Plain(UIArtGenerator.Cell), pos, new Vector2(82, 82), 28f / 14f).transform;
    }

    // ---------- Shared pieces ----------

    static Transform NewScreenScene(out EventSystem eventSystem)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("123622");
        cameraObject.transform.position = new Vector3(0, 0, -10);

        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
        {
            layer = LayerMask.NameToLayer("UI")
        };
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        // Expand keeps the whole 1920x1080 layout on screen at any aspect ratio.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        eventSystem = eventSystemObject.GetComponent<EventSystem>();

        // Background always covers the screen without stretching, cropping the edges on other aspect ratios.
        var background = Img("Background", canvasObject.transform, AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath),
            Vector2.zero, ReferenceResolution);
        background.preserveAspect = false;
        var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = BackgroundAspect;

        return canvasObject.transform;
    }

    static void Logo(Transform parent, Vector2 pos, Vector2 size)
    {
        Img("Logo", parent, AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath), pos, size);
    }

    static void Tagline(Transform parent, Vector2 pos, Vector2 size, float fontSize)
    {
        var pill = Sliced("Tagline", parent, Glossy(UIArtGenerator.Pill, Yellow), pos, size, 128f / size.y);
        Label("Text", pill.transform, "Train Your Mind. Find the Match!", fontSize, Navy, new Vector2(0, 2), size - new Vector2(30, 0), false);
    }

    static void Banner(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize)
    {
        var banner = Sliced("Banner", parent, Glossy(UIArtGenerator.Panel, Orange), pos, size, 56f / 40f).transform;
        Label("Text", banner, text, fontSize, Color.white, new Vector2(0, 4), size - new Vector2(120, 10), true);
        var half = size.x / 2;
        Img("Star Left", banner, Glossy(UIArtGenerator.Star, Gold), new Vector2(-half, 8), new Vector2(100, 100), rotation: 12);
        Img("Star Right", banner, Glossy(UIArtGenerator.Star, Gold), new Vector2(half, 8), new Vector2(100, 100), rotation: -12);
    }

    static Button BackButton(Transform parent, Vector2 pos)
    {
        var size = new Vector2(420, 112);
        var button = GlossyButton("Back Button", parent, Blue, pos, size);
        var t = button.transform;
        Img("Arrow Circle", t, Plain(UIArtGenerator.Circle), new Vector2(-140, 0), new Vector2(84, 84));
        Img("Arrow", t, Glossy(UIArtGenerator.Arrow, Hex("2A6FE0")), new Vector2(-142, 0), new Vector2(60, 60));
        Label("Text", t, "BACK", 62, Color.white, new Vector2(40, 2), new Vector2(240, 90), true);
        return button;
    }

    static Button GlossyButton(string name, Transform parent, Color color, Vector2 pos, Vector2 size)
    {
        var image = Sliced(name, parent, Glossy(UIArtGenerator.Pill, color), pos, size, 128f / size.y);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        button.colors = colors;
        image.gameObject.AddComponent<ButtonBounce>();
        return button;
    }

    // Three little dashes beside a button, as in the mockup.
    static void Sparks(Transform parent, Vector2 pos, Color color, bool rightSide)
    {
        float dir = rightSide ? 1 : -1;
        var group = Rect(rightSide ? "Sparks Right" : "Sparks Left", parent, pos, new Vector2(80, 120));
        Img("Spark", group, Glossy(UIArtGenerator.Pill, color), new Vector2(0, 42), new Vector2(46, 18), rotation: dir * 28);
        Img("Spark", group, Glossy(UIArtGenerator.Pill, color), new Vector2(dir * 6, 0), new Vector2(46, 18));
        Img("Spark", group, Glossy(UIArtGenerator.Pill, color), new Vector2(0, -42), new Vector2(46, 18), rotation: -dir * 28);
    }

    // ---------- Low-level helpers ----------

    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }

    static Sprite Glossy(string shape, Color color) => UIArtGenerator.Glossy(shape, color);

    static Sprite Plain(string name) => UIArtGenerator.Plain(name);

    static Image Img(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color? color = null, float rotation = 0)
    {
        var rect = Rect(name, parent, pos, size);
        rect.localEulerAngles = new Vector3(0, 0, rotation);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color ?? Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    // cornerScale shrinks (>1) or grows (<1) the sprite's rounded corners relative to its pixel size.
    static Image Sliced(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, float cornerScale)
    {
        var image = Img(name, parent, sprite, pos, size);
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = cornerScale;
        return image;
    }

    static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color, Vector2 pos, Vector2 box,
        bool outlined, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var rect = Rect(name, parent, pos, box);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        if (outlined)
            label.fontSharedMaterial = titleMaterial;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    static void SetRefs(Object target, params (string field, Object value)[] refs)
    {
        var serialized = new SerializedObject(target);
        foreach (var (field, value) in refs)
            serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void LoadFonts()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        titleMaterial = AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath);
        if (titleMaterial == null)
        {
            titleMaterial = new Material(font.material) { name = Path.GetFileNameWithoutExtension(TitleMaterialPath) };
            AssetDatabase.CreateAsset(titleMaterial, TitleMaterialPath);
        }

        // White title text with a navy outline and a soft drop shadow.
        titleMaterial.SetFloat("_FaceDilate", 0.15f);
        titleMaterial.SetFloat("_OutlineWidth", 0.22f);
        titleMaterial.SetColor("_OutlineColor", Navy);
        titleMaterial.EnableKeyword("UNDERLAY_ON");
        titleMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.45f));
        titleMaterial.SetFloat("_UnderlayOffsetY", -0.7f);
        titleMaterial.SetFloat("_UnderlaySoftness", 0.25f);
        EditorUtility.SetDirty(titleMaterial);
    }

    static void MakeSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
    }

    static void SaveScene(string sceneName)
    {
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), $"{ScenesFolder}/{sceneName}.unity");
    }

    // MainMenu first (scene 0), then these screens. Entries whose scene file no longer exists are dropped.
    static void AddScenesToBuild()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToList();
        var wanted = new[] { SceneNames.MainMenu, SceneNames.DifficultySelection, DifficultySceneName, SceneNames.Instructions }
            .Select(n => $"{ScenesFolder}/{n}.unity")
            .Where(File.Exists);
        foreach (var path in wanted)
        {
            if (scenes.All(s => s.path != path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        var mainMenu = scenes.FindIndex(s => s.path == $"{ScenesFolder}/{SceneNames.MainMenu}.unity");
        if (mainMenu > 0)
        {
            var entry = scenes[mainMenu];
            scenes.RemoveAt(mainMenu);
            scenes.Insert(0, entry);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Color Hex(string hex) => UIArtGenerator.Hex(hex);
}

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CreateSugorokuScene
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const int BoardCount = 20;

    [MenuItem("Tools/Create Sugoroku Scene")]
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Generated");

        EnsureSpriteAsset("Assets/Generated/Tile.png", new Color32(255, 255, 255, 255));
        EnsureSpriteAsset("Assets/Generated/BoardSurface.png", new Color32(255, 255, 255, 255));
        EnsureSpriteAsset("Assets/Generated/Panel.png", new Color32(255, 255, 255, 255));

        var tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Tile.png");
        var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Panel.png");
        var boardSurfaceSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/BoardSurface.png");
        EnsureImportedSprite("Assets/ThirdParty/Kenney/BoardgamePack/Pieces/player1.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/BoardgamePack/Pieces/player2.png", 100);
        for (var i = 1; i <= 6; i++)
        {
            EnsureImportedSprite($"Assets/Resources/KenneyDice/dice{i}.png", 100);
        }

        var player1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/BoardgamePack/Pieces/player1.png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Player1.png");
        var player2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/BoardgamePack/Pieces/player2.png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Player2.png");
        var diceSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/KenneyDice/dice6.png")
            .OfType<Sprite>()
            .FirstOrDefault()
            ?? AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/DiceSides/dice6.png").OfType<Sprite>().FirstOrDefault();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "SampleScene";

        CreateCamera();
        CreateBoard(tileSprite, boardSurfaceSprite, out var player1Waypoints, out var player2Waypoints);
        CreatePlayer("Player1", player1Sprite, player1Waypoints, 10);
        CreatePlayer("Player2", player2Sprite, player2Waypoints, 11);
        CreateDice(diceSprite, panelSprite);
        CreateGameControl();
        CreateUi(panelSprite);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    private static void EnsureSpriteAsset(string path, Color32 color)
    {
        if (!File.Exists(path))
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[64 * 64];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }

        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static void EnsureImportedSprite(string path, int pixelsPerUnit)
    {
        if (!File.Exists(path))
        {
            return;
        }

        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsToUnits = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.25f;
        camera.backgroundColor = new Color32(244, 246, 248, 255);
        cameraObject.transform.position = new Vector3(0f, -0.1f, -10f);
        cameraObject.tag = "MainCamera";
    }

    private static void CreateBoard(
        Sprite tileSprite,
        Sprite boardSurfaceSprite,
        out Transform[] player1Waypoints,
        out Transform[] player2Waypoints)
    {
        var board = new GameObject("Board");

        var surface = new GameObject("BoardSurface");
        surface.transform.SetParent(board.transform);
        surface.transform.position = new Vector3(0f, 0.12f, 0.2f);
        surface.transform.localScale = new Vector3(4.95f, 3.95f, 1f);
        var surfaceRenderer = surface.AddComponent<SpriteRenderer>();
        surfaceRenderer.sprite = boardSurfaceSprite;
        surfaceRenderer.color = new Color32(255, 255, 255, 255);
        surfaceRenderer.sortingOrder = -2;

        var p1Root = new GameObject("Player1Waypoints");
        var p2Root = new GameObject("Player2Waypoints");
        p1Root.transform.SetParent(board.transform);
        p2Root.transform.SetParent(board.transform);

        player1Waypoints = new Transform[BoardCount];
        player2Waypoints = new Transform[BoardCount];

        for (var i = 0; i < BoardCount; i++)
        {
            var column = i % 5;
            var row = i / 5;
            if (row % 2 == 1)
            {
                column = 4 - column;
            }

            var x = (column - 2f) * 0.86f;
            var y = 1.38f - row * 0.82f;

            var tile = new GameObject($"Tile_{i + 1:00}");
            tile.transform.SetParent(board.transform);
            tile.transform.position = new Vector3(x, y, 0f);
            tile.transform.localScale = new Vector3(0.76f, 0.76f, 1f);

            var tileRenderer = tile.AddComponent<SpriteRenderer>();
            tileRenderer.sprite = tileSprite;
            tileRenderer.color = GetTileColor(i);
            tileRenderer.sortingOrder = 0;

            var label = new GameObject($"TileLabel_{i + 1:00}");
            label.transform.SetParent(tile.transform);
            label.transform.localPosition = new Vector3(-0.32f, 0.28f, -0.02f);
            var text = label.AddComponent<TextMesh>();
            text.text = (i + 1).ToString();
            text.fontSize = 48;
            text.characterSize = 0.075f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color32(100, 116, 139, 255);
            label.GetComponent<MeshRenderer>().sortingOrder = 1;

            var p1Waypoint = new GameObject($"P1Waypoint_{i + 1:00}");
            p1Waypoint.transform.SetParent(p1Root.transform);
            p1Waypoint.transform.position = new Vector3(x - 0.17f, y - 0.18f, 0f);
            player1Waypoints[i] = p1Waypoint.transform;

            var p2Waypoint = new GameObject($"P2Waypoint_{i + 1:00}");
            p2Waypoint.transform.SetParent(p2Root.transform);
            p2Waypoint.transform.position = new Vector3(x + 0.17f, y - 0.18f, 0f);
            player2Waypoints[i] = p2Waypoint.transform;
        }
    }

    private static Color32 GetTileColor(int index)
    {
        if (index == 0)
        {
            return new Color32(220, 252, 231, 255);
        }

        if (index == BoardCount - 1)
        {
            return new Color32(254, 240, 138, 255);
        }

        return index % 2 == 0
            ? new Color32(248, 250, 252, 255)
            : new Color32(226, 232, 240, 255);
    }

    private static void CreatePlayer(string name, Sprite sprite, Transform[] waypoints, int sortingOrder)
    {
        var player = new GameObject(name);
        player.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        var renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        var follower = player.AddComponent<FollowThePath>();
        follower.waypoints = waypoints;
    }

    private static void CreateDice(Sprite diceSprite, Sprite panelSprite)
    {
        var tray = new GameObject("DiceTray");
        tray.transform.position = new Vector3(3.2f, -2.2f, 0.04f);
        tray.transform.localScale = new Vector3(1.45f, 1.45f, 1f);
        var trayRenderer = tray.AddComponent<SpriteRenderer>();
        trayRenderer.sprite = panelSprite;
        trayRenderer.color = new Color32(255, 255, 255, 255);
        trayRenderer.sortingOrder = 8;

        var dice = new GameObject("Dice");
        dice.transform.position = new Vector3(3.2f, -2.2f, 0f);
        dice.transform.localScale = new Vector3(0.96f, 0.96f, 1f);
        var renderer = dice.AddComponent<SpriteRenderer>();
        renderer.sprite = diceSprite;
        renderer.sortingOrder = 10;
        dice.AddComponent<BoxCollider2D>();
        dice.AddComponent<Dice>();
    }

    private static void CreateGameControl()
    {
        var gameControl = new GameObject("GameControl");
        gameControl.AddComponent<GameControl>();
    }

    private static void CreateUi(Sprite panelSprite)
    {
        var canvas = new GameObject("Canvas");
        var canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.AddComponent<GraphicRaycaster>();

        CreatePanel(canvas.transform, "TopBar", panelSprite, new Vector2(0f, 234f), new Vector2(960f, 72f), new Color32(17, 24, 39, 255));
        CreateText(canvas.transform, "TitleText", "Sugoroku", new Vector2(-372f, 236f), new Vector2(190f, 36f), 28, new Color32(255, 255, 255, 255), TextAnchor.MiddleLeft);
        CreateText(canvas.transform, "TurnText", "Player 1's turn", new Vector2(0f, 236f), new Vector2(300f, 36f), 27, new Color32(96, 165, 250, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "DiceResultText", "Ready", new Vector2(360f, 236f), new Vector2(210f, 32f), 20, new Color32(226, 232, 240, 255), TextAnchor.MiddleRight);

        CreatePlayerCard(canvas.transform, "Player1", new Vector2(-252f, -216f), new Color32(37, 99, 235, 255), panelSprite);
        CreatePlayerCard(canvas.transform, "Player2", new Vector2(252f, -216f), new Color32(220, 38, 38, 255), panelSprite);

        CreatePanel(canvas.transform, "DiceStatusPanel", panelSprite, new Vector2(0f, -216f), new Vector2(170f, 64f), new Color32(255, 255, 255, 240));
        CreateText(canvas.transform, "DiceLabelText", "DICE", new Vector2(0f, -201f), new Vector2(120f, 20f), 12, new Color32(100, 116, 139, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "RollStateText", "READY", new Vector2(0f, -226f), new Vector2(120f, 30f), 22, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);

        CreatePanel(canvas.transform, "WinnerPanel", panelSprite, new Vector2(0f, 94f), new Vector2(420f, 76f), new Color32(255, 255, 255, 246));
        CreateText(canvas.transform, "WhoWinsText", "Player wins", new Vector2(0f, 94f), new Vector2(380f, 54f), 38, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);

        CreateText(canvas.transform, "Player1MoveText", "Player 1", new Vector2(-78f, 196f), new Vector2(120f, 28f), 17, new Color32(96, 165, 250, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "Player2MoveText", "Player 2", new Vector2(78f, 196f), new Vector2(120f, 28f), 17, new Color32(248, 113, 113, 255), TextAnchor.MiddleCenter);
    }

    private static void CreatePanel(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 anchoredPosition,
        Vector2 size,
        Color32 color)
    {
        var panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        var rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        var image = panelObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
    }

    private static void CreatePlayerCard(Transform parent, string playerName, Vector2 anchoredPosition, Color32 accentColor, Sprite panelSprite)
    {
        CreatePanel(parent, $"{playerName}Panel", panelSprite, anchoredPosition, new Vector2(248f, 72f), new Color32(255, 255, 255, 235));
        CreateText(parent, $"{playerName}StatusText", playerName.Replace("Player", "Player "), anchoredPosition + new Vector2(-62f, 17f), new Vector2(120f, 26f), 21, accentColor, TextAnchor.MiddleLeft);
        CreateText(parent, $"{playerName}PositionText", "1/20", anchoredPosition + new Vector2(78f, 17f), new Vector2(72f, 26f), 21, new Color32(15, 23, 42, 255), TextAnchor.MiddleRight);
        CreateProgressBar(parent, playerName, anchoredPosition + new Vector2(0f, -19f), accentColor, panelSprite);
    }

    private static void CreateProgressBar(Transform parent, string playerName, Vector2 anchoredPosition, Color32 fillColor, Sprite panelSprite)
    {
        var trackObject = new GameObject($"{playerName}ProgressTrack");
        trackObject.transform.SetParent(parent, false);
        var trackRect = trackObject.AddComponent<RectTransform>();
        trackRect.sizeDelta = new Vector2(204f, 8f);
        trackRect.anchoredPosition = anchoredPosition;
        var trackImage = trackObject.AddComponent<Image>();
        trackImage.sprite = panelSprite;
        trackImage.color = new Color32(203, 213, 225, 255);

        var fillObject = new GameObject($"{playerName}ProgressFill");
        fillObject.transform.SetParent(trackObject.transform, false);
        var fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = panelSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;
        fillImage.color = fillColor;
    }

    private static void CreateText(
        Transform parent,
        string name,
        string value,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        Color32 color,
        TextAnchor alignment)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
    }
}

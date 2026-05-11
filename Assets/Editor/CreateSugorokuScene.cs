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

    [MenuItem("Tools/Create Sugoroku Scene")]
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Generated");

        EnsureSpriteAsset("Assets/Generated/Tile.png", new Color32(235, 232, 220, 255));
        EnsureSpriteAsset("Assets/Generated/Player1.png", new Color32(66, 135, 245, 255));
        EnsureSpriteAsset("Assets/Generated/Player2.png", new Color32(237, 84, 86, 255));

        var tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Tile.png");
        var player1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Player1.png");
        var player2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Player2.png");
        var diceSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/DiceSides/dice6.png")
            .OfType<Sprite>()
            .FirstOrDefault();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "SampleScene";

        CreateCamera();
        CreateBoard(tileSprite, out var player1Waypoints, out var player2Waypoints);
        CreatePlayer("Player1", player1Sprite, player1Waypoints, 5);
        CreatePlayer("Player2", player2Sprite, player2Waypoints, 6);
        CreateDice(diceSprite);
        CreateGameControl();
        CreateUi();

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

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.5f;
        camera.backgroundColor = new Color32(248, 248, 244, 255);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.tag = "MainCamera";
    }

    private static void CreateBoard(Sprite tileSprite, out Transform[] player1Waypoints, out Transform[] player2Waypoints)
    {
        var board = new GameObject("Board");
        var p1Root = new GameObject("Player1Waypoints");
        var p2Root = new GameObject("Player2Waypoints");
        p1Root.transform.SetParent(board.transform);
        p2Root.transform.SetParent(board.transform);

        const int count = 20;
        player1Waypoints = new Transform[count];
        player2Waypoints = new Transform[count];

        for (var i = 0; i < count; i++)
        {
            var column = i % 10;
            var row = i / 10;
            if (row % 2 == 1)
            {
                column = 9 - column;
            }

            var x = (column - 4.5f) * 0.85f;
            var y = (1.2f - row) * 0.85f;

            var tile = new GameObject($"Tile_{i + 1:00}");
            tile.transform.SetParent(board.transform);
            tile.transform.position = new Vector3(x, y, 0f);
            tile.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            var tileRenderer = tile.AddComponent<SpriteRenderer>();
            tileRenderer.sprite = tileSprite;
            tileRenderer.color = i == count - 1 ? new Color32(250, 212, 96, 255) : Color.white;
            tileRenderer.sortingOrder = 0;

            var p1Waypoint = new GameObject($"P1Waypoint_{i + 1:00}");
            p1Waypoint.transform.SetParent(p1Root.transform);
            p1Waypoint.transform.position = new Vector3(x, y + 0.18f, 0f);
            player1Waypoints[i] = p1Waypoint.transform;

            var p2Waypoint = new GameObject($"P2Waypoint_{i + 1:00}");
            p2Waypoint.transform.SetParent(p2Root.transform);
            p2Waypoint.transform.position = new Vector3(x, y - 0.18f, 0f);
            player2Waypoints[i] = p2Waypoint.transform;
        }
    }

    private static void CreatePlayer(string name, Sprite sprite, Transform[] waypoints, int sortingOrder)
    {
        var player = new GameObject(name);
        player.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        var renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        var follower = player.AddComponent<FollowThePath>();
        follower.waypoints = waypoints;
    }

    private static void CreateDice(Sprite diceSprite)
    {
        var dice = new GameObject("Dice");
        dice.transform.position = new Vector3(0f, -2.7f, 0f);
        dice.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
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

    private static void CreateUi()
    {
        var canvas = new GameObject("Canvas");
        var canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();

        CreateText(canvas.transform, "WhoWinsText", "Player wins", new Vector2(0f, 155f), 34);
        CreateText(canvas.transform, "Player1MoveText", "Player 1 turn", new Vector2(-220f, 210f), 26);
        CreateText(canvas.transform, "Player2MoveText", "Player 2 turn", new Vector2(220f, 210f), 26);

        // UI is display-only, so no EventSystem is needed.
        // StandaloneInputModule uses the legacy Input API and throws when the
        // project is set to the Input System package only.
    }

    private static void CreateText(Transform parent, string name, string value, Vector2 anchoredPosition, int fontSize)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(360f, 48f);
        rectTransform.anchoredPosition = anchoredPosition;

        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color32(30, 30, 30, 255);
    }
}

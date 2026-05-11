using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
        EnsureSpriteAsset("Assets/Generated/Player3.png", new Color32(22, 163, 74, 255));
        EnsureSpriteAsset("Assets/Generated/Player4.png", new Color32(147, 51, 234, 255));

        var tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Tile.png");
        var generatedPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Panel.png");
        var boardSurfaceSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/BoardSurface.png");
        EnsureImportedSprite("Assets/ThirdParty/Kenney/UIPack/button_rectangle_flat.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/UIPack/button_rectangle_depth_flat.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/UIPack/slide_horizontal_grey.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/UIPack/slide_horizontal_color.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/GameIcons/basket.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/GameIcons/trophy.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/GameIcons/warning.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/GameIcons/star.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/BoardgamePack/Pieces/player1.png", 100);
        EnsureImportedSprite("Assets/ThirdParty/Kenney/BoardgamePack/Pieces/player2.png", 100);
        for (var i = 1; i <= 6; i++)
        {
            EnsureImportedSprite($"Assets/Resources/KenneyDice/dice{i}.png", 100);
        }

        var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/UIPack/button_rectangle_flat.png") ?? generatedPanelSprite;
        var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/UIPack/button_rectangle_depth_flat.png") ?? panelSprite;
        var progressTrackSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/UIPack/slide_horizontal_grey.png") ?? panelSprite;
        var progressFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/UIPack/slide_horizontal_color.png") ?? panelSprite;
        var statIcons = new StatIconSprites
        {
            Money = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/GameIcons/basket.png"),
            IfPoint = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/GameIcons/trophy.png"),
            Mental = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/GameIcons/warning.png"),
            Virtue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/GameIcons/star.png")
        };
        var uiClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/Kenney/InterfaceSounds/click_001.ogg");
        var uiConfirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/Kenney/InterfaceSounds/confirmation_001.ogg");
        var uiErrorClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ThirdParty/Kenney/InterfaceSounds/error_001.ogg");

        var player1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/BoardgamePack/Pieces/player1.png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Player1.png");
        var player2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ThirdParty/Kenney/BoardgamePack/Pieces/player2.png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Player2.png");
        var player3Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Player3.png");
        var player4Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/Player4.png");
        var diceSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/KenneyDice/dice6.png")
            .OfType<Sprite>()
            .FirstOrDefault()
            ?? AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/DiceSides/dice6.png").OfType<Sprite>().FirstOrDefault();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "SampleScene";

        CreateCamera();
        CreateBoard(tileSprite, boardSurfaceSprite, out var playerWaypoints);
        CreatePlayer("Player1", player1Sprite, playerWaypoints[0], 10);
        CreatePlayer("Player2", player2Sprite, playerWaypoints[1], 11);
        CreatePlayer("Player3", player3Sprite, playerWaypoints[2], 12);
        CreatePlayer("Player4", player4Sprite, playerWaypoints[3], 13);
        CreateDice(diceSprite, panelSprite);
        CreateGameControl();
        CreateUiSoundPlayer(uiClickClip, uiConfirmClip, uiErrorClip);
        CreateUi(panelSprite, buttonSprite, progressTrackSprite, progressFillSprite, statIcons);
        CreateEventSystem();

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
            importer.spritePixelsPerUnit = pixelsPerUnit;
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
        out Transform[][] playerWaypoints)
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

        playerWaypoints = new Transform[4][];
        var waypointRoots = new Transform[4];
        for (var playerIndex = 0; playerIndex < 4; playerIndex++)
        {
            var root = new GameObject($"Player{playerIndex + 1}Waypoints");
            root.transform.SetParent(board.transform);
            waypointRoots[playerIndex] = root.transform;
            playerWaypoints[playerIndex] = new Transform[BoardCount];
        }

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

            var offsets = new[]
            {
                new Vector2(-0.17f, -0.18f),
                new Vector2(0.17f, -0.18f),
                new Vector2(-0.17f, 0.14f),
                new Vector2(0.17f, 0.14f)
            };

            for (var playerIndex = 0; playerIndex < 4; playerIndex++)
            {
                var waypoint = new GameObject($"P{playerIndex + 1}Waypoint_{i + 1:00}");
                waypoint.transform.SetParent(waypointRoots[playerIndex]);
                waypoint.transform.position = new Vector3(x + offsets[playerIndex].x, y + offsets[playerIndex].y, 0f);
                playerWaypoints[playerIndex][i] = waypoint.transform;
            }
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

    private static void CreateUiSoundPlayer(AudioClip clickClip, AudioClip confirmClip, AudioClip errorClip)
    {
        var soundObject = new GameObject("UiSoundPlayer");
        var audioSource = soundObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        var player = soundObject.AddComponent<UiSoundPlayer>();
        player.clickClip = clickClip;
        player.confirmClip = confirmClip;
        player.errorClip = errorClip;
    }

    private static void CreateEventSystem()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreateUi(
        Sprite panelSprite,
        Sprite buttonSprite,
        Sprite progressTrackSprite,
        Sprite progressFillSprite,
        StatIconSprites statIcons)
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
        CreateText(canvas.transform, "TitleText", "大学院生すごろく", new Vector2(-350f, 236f), new Vector2(260f, 36f), 25, new Color32(255, 255, 255, 255), TextAnchor.MiddleLeft);
        CreateText(canvas.transform, "TurnText", "Player 1's turn", new Vector2(0f, 236f), new Vector2(300f, 36f), 27, new Color32(96, 165, 250, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "DiceResultText", "Ready", new Vector2(360f, 236f), new Vector2(210f, 32f), 20, new Color32(226, 232, 240, 255), TextAnchor.MiddleRight);
        CreateButton(canvas.transform, "PauseButton", "||", new Vector2(452f, 236f), new Vector2(46f, 36f), buttonSprite, new Color32(71, 85, 105, 255));
        CreateText(canvas.transform, "EventLogText", "研究生活、開始。", new Vector2(0f, 195f), new Vector2(520f, 28f), 17, new Color32(30, 41, 59, 255), TextAnchor.MiddleCenter);

        CreatePlayerCard(canvas.transform, "Player1", new Vector2(-315f, -187f), new Color32(37, 99, 235, 255), panelSprite, progressTrackSprite, progressFillSprite, statIcons);
        CreatePlayerCard(canvas.transform, "Player2", new Vector2(-105f, -187f), new Color32(220, 38, 38, 255), panelSprite, progressTrackSprite, progressFillSprite, statIcons);
        CreatePlayerCard(canvas.transform, "Player3", new Vector2(105f, -187f), new Color32(22, 163, 74, 255), panelSprite, progressTrackSprite, progressFillSprite, statIcons);
        CreatePlayerCard(canvas.transform, "Player4", new Vector2(315f, -187f), new Color32(147, 51, 234, 255), panelSprite, progressTrackSprite, progressFillSprite, statIcons);

        CreatePanel(canvas.transform, "DiceStatusPanel", panelSprite, new Vector2(0f, -252f), new Vector2(150f, 52f), new Color32(255, 255, 255, 240));
        CreateText(canvas.transform, "DiceLabelText", "DICE", new Vector2(-48f, -252f), new Vector2(60f, 20f), 12, new Color32(100, 116, 139, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "RollStateText", "READY", new Vector2(32f, -252f), new Vector2(80f, 28f), 20, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);
        CreateButton(canvas.transform, "SkillButton", "一旦逃避", new Vector2(355f, -252f), new Vector2(160f, 44f), buttonSprite, new Color32(99, 102, 241, 255));

        CreatePanel(canvas.transform, "WinnerPanel", panelSprite, new Vector2(0f, 42f), new Vector2(560f, 308f), new Color32(255, 255, 255, 248));
        CreateText(canvas.transform, "WhoWinsText", "進路発表", new Vector2(0f, 156f), new Vector2(480f, 42f), 32, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "ResultSummaryText", string.Empty, new Vector2(0f, 18f), new Vector2(500f, 220f), 15, new Color32(30, 41, 59, 255), TextAnchor.UpperLeft);

        CreateText(canvas.transform, "Player1MoveText", "Player 1", new Vector2(-78f, 196f), new Vector2(120f, 28f), 17, new Color32(96, 165, 250, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "Player2MoveText", "Player 2", new Vector2(78f, 196f), new Vector2(120f, 28f), 17, new Color32(248, 113, 113, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "Player3MoveText", "Player 3", new Vector2(234f, 196f), new Vector2(120f, 28f), 17, new Color32(74, 222, 128, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "Player4MoveText", "Player 4", new Vector2(390f, 196f), new Vector2(120f, 28f), 17, new Color32(192, 132, 252, 255), TextAnchor.MiddleCenter);

        CreateEventModal(canvas.transform, panelSprite, buttonSprite);
        CreateMenuScreens(canvas.transform, panelSprite, buttonSprite);
        CreatePauseAndReconnectScreens(canvas.transform, panelSprite, buttonSprite);
    }

    private static GameObject CreatePanel(
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
        return panelObject;
    }

    private static void CreateEventModal(Transform parent, Sprite panelSprite, Sprite buttonSprite)
    {
        var panel = CreatePanel(parent, "EventModalPanel", panelSprite, new Vector2(0f, 30f), new Vector2(560f, 286f), new Color32(255, 255, 255, 250));
        CreateText(panel.transform, "EventModalEyebrowText", "EVENT", new Vector2(-230f, 112f), new Vector2(72f, 20f), 13, new Color32(100, 116, 139, 255), TextAnchor.MiddleLeft);
        CreateText(panel.transform, "EventModalTitleText", "イベント", new Vector2(0f, 82f), new Vector2(488f, 34f), 24, new Color32(15, 23, 42, 255), TextAnchor.MiddleLeft);
        CreateText(panel.transform, "EventModalDescriptionText", "説明", new Vector2(0f, 36f), new Vector2(488f, 52f), 15, new Color32(51, 65, 85, 255), TextAnchor.MiddleLeft);

        CreateButton(panel.transform, "EventChoice1Button", "選択肢1", new Vector2(0f, -26f), new Vector2(488f, 42f), buttonSprite, new Color32(37, 99, 235, 255));
        CreateButton(panel.transform, "EventChoice2Button", "選択肢2", new Vector2(0f, -78f), new Vector2(488f, 42f), buttonSprite, new Color32(15, 23, 42, 255));
        CreateButton(panel.transform, "EventChoice3Button", "選択肢3", new Vector2(0f, -130f), new Vector2(488f, 42f), buttonSprite, new Color32(71, 85, 105, 255));
    }

    private static void CreateMenuScreens(Transform parent, Sprite panelSprite, Sprite buttonSprite)
    {
        var titlePanel = CreatePanel(parent, "TitlePanel", panelSprite, Vector2.zero, new Vector2(960f, 540f), new Color32(15, 23, 42, 248));
        CreateText(titlePanel.transform, "TitleScreenTitleText", "大学院生すごろく", new Vector2(0f, 122f), new Vector2(540f, 54f), 36, new Color32(255, 255, 255, 255), TextAnchor.MiddleCenter);
        CreateText(titlePanel.transform, "TitleScreenSubtitleText", "金、メンタル、IF、徳で生き残る研究生活", new Vector2(0f, 72f), new Vector2(540f, 28f), 17, new Color32(203, 213, 225, 255), TextAnchor.MiddleCenter);
        CreateButton(titlePanel.transform, "StartGameButton", "ゲーム開始", new Vector2(0f, 8f), new Vector2(240f, 46f), buttonSprite, new Color32(37, 99, 235, 255));
        CreateButton(titlePanel.transform, "OpenSettingsButton", "設定", new Vector2(0f, -48f), new Vector2(240f, 42f), buttonSprite, new Color32(71, 85, 105, 255));
        CreateButton(titlePanel.transform, "OpenAchievementsButton", "実績", new Vector2(0f, -100f), new Vector2(240f, 42f), buttonSprite, new Color32(71, 85, 105, 255));

        var settingsPanel = CreatePanel(parent, "SettingsPanel", panelSprite, Vector2.zero, new Vector2(560f, 300f), new Color32(255, 255, 255, 250));
        CreateText(settingsPanel.transform, "SettingsTitleText", "設定", new Vector2(0f, 104f), new Vector2(460f, 40f), 30, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);
        CreateText(settingsPanel.transform, "SettingsPlayersText", "Players 4", new Vector2(0f, 62f), new Vector2(220f, 24f), 18, new Color32(30, 41, 59, 255), TextAnchor.MiddleCenter);
        CreateButton(settingsPanel.transform, "PlayersDownButton", "-", new Vector2(-136f, 62f), new Vector2(56f, 34f), buttonSprite, new Color32(71, 85, 105, 255));
        CreateButton(settingsPanel.transform, "PlayersUpButton", "+", new Vector2(136f, 62f), new Vector2(56f, 34f), buttonSprite, new Color32(37, 99, 235, 255));
        CreateText(settingsPanel.transform, "SettingsCpuText", "CPU 3", new Vector2(0f, 18f), new Vector2(220f, 24f), 18, new Color32(30, 41, 59, 255), TextAnchor.MiddleCenter);
        CreateButton(settingsPanel.transform, "CpuDownButton", "-", new Vector2(-136f, 18f), new Vector2(56f, 34f), buttonSprite, new Color32(71, 85, 105, 255));
        CreateButton(settingsPanel.transform, "CpuUpButton", "+", new Vector2(136f, 18f), new Vector2(56f, 34f), buttonSprite, new Color32(37, 99, 235, 255));
        CreateText(settingsPanel.transform, "SettingsVolumeText", "SE Volume 80%", new Vector2(0f, -28f), new Vector2(220f, 24f), 18, new Color32(30, 41, 59, 255), TextAnchor.MiddleCenter);
        CreateButton(settingsPanel.transform, "VolumeDownButton", "SE -", new Vector2(-136f, -28f), new Vector2(72f, 34f), buttonSprite, new Color32(71, 85, 105, 255));
        CreateButton(settingsPanel.transform, "VolumeUpButton", "SE +", new Vector2(136f, -28f), new Vector2(72f, 34f), buttonSprite, new Color32(37, 99, 235, 255));
        CreateText(settingsPanel.transform, "AccountLinkText", "アカウント連携: 未実装", new Vector2(0f, -76f), new Vector2(320f, 24f), 15, new Color32(100, 116, 139, 255), TextAnchor.MiddleCenter);
        CreateButton(settingsPanel.transform, "SettingsBackButton", "戻る", new Vector2(0f, -118f), new Vector2(160f, 38f), buttonSprite, new Color32(15, 23, 42, 255));

        var achievementsPanel = CreatePanel(parent, "AchievementsPanel", panelSprite, Vector2.zero, new Vector2(560f, 300f), new Color32(255, 255, 255, 250));
        CreateText(achievementsPanel.transform, "AchievementsTitleText", "実績", new Vector2(0f, 104f), new Vector2(460f, 40f), 30, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);
        CreateText(achievementsPanel.transform, "AchievementsText", "修了回数 0\n最高スコア 0\n最後の進路 未記録", new Vector2(0f, 24f), new Vector2(360f, 120f), 20, new Color32(30, 41, 59, 255), TextAnchor.MiddleCenter);
        CreateButton(achievementsPanel.transform, "AchievementsBackButton", "戻る", new Vector2(0f, -118f), new Vector2(160f, 38f), buttonSprite, new Color32(15, 23, 42, 255));
    }

    private static void CreatePauseAndReconnectScreens(Transform parent, Sprite panelSprite, Sprite buttonSprite)
    {
        var pausePanel = CreatePanel(parent, "PausePanel", panelSprite, Vector2.zero, new Vector2(420f, 286f), new Color32(255, 255, 255, 250));
        CreateText(pausePanel.transform, "PauseTitleText", "ポーズ", new Vector2(0f, 94f), new Vector2(320f, 38f), 30, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);
        CreateButton(pausePanel.transform, "ResumeButton", "再開", new Vector2(0f, 38f), new Vector2(220f, 42f), buttonSprite, new Color32(37, 99, 235, 255));
        CreateButton(pausePanel.transform, "SimulateDisconnectButton", "切断テスト", new Vector2(0f, -12f), new Vector2(220f, 42f), buttonSprite, new Color32(71, 85, 105, 255));
        CreateButton(pausePanel.transform, "GiveUpButton", "諦める", new Vector2(0f, -62f), new Vector2(220f, 42f), buttonSprite, new Color32(190, 18, 60, 255));
        CreateButton(pausePanel.transform, "QuitToTitleButton", "タイトルへ", new Vector2(0f, -112f), new Vector2(220f, 42f), buttonSprite, new Color32(15, 23, 42, 255));

        var reconnectPanel = CreatePanel(parent, "ReconnectPanel", panelSprite, Vector2.zero, new Vector2(500f, 220f), new Color32(15, 23, 42, 250));
        CreateText(reconnectPanel.transform, "ReconnectTitleText", "通信が切断されました", new Vector2(0f, 58f), new Vector2(420f, 34f), 26, new Color32(255, 255, 255, 255), TextAnchor.MiddleCenter);
        CreateText(reconnectPanel.transform, "ReconnectBodyText", "保存済みのターン状態から復帰します。", new Vector2(0f, 16f), new Vector2(420f, 26f), 16, new Color32(203, 213, 225, 255), TextAnchor.MiddleCenter);
        CreateButton(reconnectPanel.transform, "ReconnectButton", "再接続", new Vector2(0f, -56f), new Vector2(180f, 42f), buttonSprite, new Color32(37, 99, 235, 255));
    }

    private static void CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        Sprite panelSprite,
        Color32 color)
    {
        var buttonObject = CreatePanel(parent, name, panelSprite, anchoredPosition, size, color);
        var image = buttonObject.GetComponent<Image>();
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color32(
            (byte)Mathf.Min(color.r + 24, 255),
            (byte)Mathf.Min(color.g + 24, 255),
            (byte)Mathf.Min(color.b + 24, 255),
            255);
        colors.pressedColor = new Color32(
            (byte)Mathf.Max(color.r - 24, 0),
            (byte)Mathf.Max(color.g - 24, 0),
            (byte)Mathf.Max(color.b - 24, 0),
            255);
        colors.disabledColor = new Color32(148, 163, 184, 180);
        button.colors = colors;

        var textName = name == "SkillButton" ? "SkillButtonText" : name.Replace("Button", "Text");
        CreateText(buttonObject.transform, textName, label, Vector2.zero, size - new Vector2(28f, 0f), 15, new Color32(255, 255, 255, 255), TextAnchor.MiddleLeft);
    }

    private static void CreatePlayerCard(
        Transform parent,
        string playerName,
        Vector2 anchoredPosition,
        Color32 accentColor,
        Sprite panelSprite,
        Sprite progressTrackSprite,
        Sprite progressFillSprite,
        StatIconSprites statIcons)
    {
        CreatePanel(parent, $"{playerName}Panel", panelSprite, anchoredPosition, new Vector2(198f, 92f), new Color32(255, 255, 255, 235));
        CreateText(parent, $"{playerName}StatusText", playerName.Replace("Player", "P"), anchoredPosition + new Vector2(-21f, 28f), new Vector2(118f, 22f), 15, accentColor, TextAnchor.MiddleLeft);
        CreateText(parent, $"{playerName}PositionText", "1/20", anchoredPosition + new Vector2(63f, 28f), new Vector2(58f, 22f), 15, new Color32(15, 23, 42, 255), TextAnchor.MiddleRight);
        CreateStatChip(parent, $"{playerName}Money", statIcons.Money, "0", anchoredPosition + new Vector2(-69f, 2f), new Color32(34, 197, 94, 255));
        CreateStatChip(parent, $"{playerName}If", statIcons.IfPoint, "0", anchoredPosition + new Vector2(-23f, 2f), new Color32(234, 179, 8, 255));
        CreateStatChip(parent, $"{playerName}Mental", statIcons.Mental, "0", anchoredPosition + new Vector2(23f, 2f), new Color32(244, 63, 94, 255));
        CreateStatChip(parent, $"{playerName}Virtue", statIcons.Virtue, "0", anchoredPosition + new Vector2(69f, 2f), new Color32(147, 51, 234, 255));
        CreateProgressBar(parent, playerName, anchoredPosition + new Vector2(0f, -31f), accentColor, progressTrackSprite, progressFillSprite);
    }

    private static void CreateStatChip(Transform parent, string name, Sprite icon, string value, Vector2 anchoredPosition, Color32 color)
    {
        CreateImage(parent, $"{name}Icon", icon, anchoredPosition + new Vector2(-12f, 0f), new Vector2(18f, 18f), color);
        CreateText(parent, $"{name}Text", value, anchoredPosition + new Vector2(12f, 0f), new Vector2(28f, 20f), 12, new Color32(15, 23, 42, 255), TextAnchor.MiddleLeft);
    }

    private static void CreateProgressBar(
        Transform parent,
        string playerName,
        Vector2 anchoredPosition,
        Color32 fillColor,
        Sprite trackSprite,
        Sprite fillSprite)
    {
        var trackObject = new GameObject($"{playerName}ProgressTrack");
        trackObject.transform.SetParent(parent, false);
        var trackRect = trackObject.AddComponent<RectTransform>();
        trackRect.sizeDelta = new Vector2(160f, 8f);
        trackRect.anchoredPosition = anchoredPosition;
        var trackImage = trackObject.AddComponent<Image>();
        trackImage.sprite = trackSprite;
        trackImage.color = new Color32(203, 213, 225, 255);

        var fillObject = new GameObject($"{playerName}ProgressFill");
        fillObject.transform.SetParent(trackObject.transform, false);
        var fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = fillSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;
        fillImage.color = fillColor;
    }

    private static void CreateImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 anchoredPosition,
        Vector2 size,
        Color32 color)
    {
        var imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        var rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        var image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
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

    private sealed class StatIconSprites
    {
        public Sprite Money;
        public Sprite IfPoint;
        public Sprite Mental;
        public Sprite Virtue;
    }
}

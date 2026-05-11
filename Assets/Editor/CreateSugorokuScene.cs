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

        EnsureCharacterPortraits();
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
        var characterPortraits = new CharacterPortraitSprites
        {
            Hobby = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/CharacterPortraits/Grad_Hobby.png"),
            Serious = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/CharacterPortraits/Grad_Serious.png"),
            Athletic = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/CharacterPortraits/Grad_Athletic.png"),
            Rich = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/CharacterPortraits/Grad_Rich.png"),
            Genius = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/CharacterPortraits/Grad_Genius.png")
        };
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
        CreateBoard3DPreview();
        CreatePlayer("Player1", player1Sprite, playerWaypoints[0], 10);
        CreatePlayer("Player2", player2Sprite, playerWaypoints[1], 11);
        CreatePlayer("Player3", player3Sprite, playerWaypoints[2], 12);
        CreatePlayer("Player4", player4Sprite, playerWaypoints[3], 13);
        CreateDice(diceSprite, panelSprite);
        CreateGameControl();
        CreateUiSoundPlayer(uiClickClip, uiConfirmClip, uiErrorClip);
        CreateUi(panelSprite, buttonSprite, progressTrackSprite, progressFillSprite, statIcons, characterPortraits);
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

    private static void EnsureCharacterPortraits()
    {
        Directory.CreateDirectory("Assets/Resources");
        Directory.CreateDirectory("Assets/Resources/CharacterPortraits");

        CreateCharacterPortrait(
            "Assets/Resources/CharacterPortraits/Grad_Hobby.png",
            new Color32(219, 234, 254, 255),
            new Color32(30, 64, 175, 255),
            new Color32(16, 185, 129, 255),
            0);
        CreateCharacterPortrait(
            "Assets/Resources/CharacterPortraits/Grad_Serious.png",
            new Color32(226, 232, 240, 255),
            new Color32(15, 23, 42, 255),
            new Color32(51, 65, 85, 255),
            1);
        CreateCharacterPortrait(
            "Assets/Resources/CharacterPortraits/Grad_Athletic.png",
            new Color32(220, 252, 231, 255),
            new Color32(39, 39, 42, 255),
            new Color32(22, 163, 74, 255),
            2);
        CreateCharacterPortrait(
            "Assets/Resources/CharacterPortraits/Grad_Rich.png",
            new Color32(254, 243, 199, 255),
            new Color32(92, 64, 51, 255),
            new Color32(245, 158, 11, 255),
            3);
        CreateCharacterPortrait(
            "Assets/Resources/CharacterPortraits/Grad_Genius.png",
            new Color32(237, 233, 254, 255),
            new Color32(79, 70, 229, 255),
            new Color32(124, 58, 237, 255),
            4);
    }

    private static void CreateCharacterPortrait(string path, Color32 background, Color32 hair, Color32 shirt, int style)
    {
        var texture = new Texture2D(160, 160, TextureFormat.RGBA32, false);
        Fill(texture, background);

        FillCircle(texture, 80, 80, 70, new Color32(255, 255, 255, 118));
        FillCircle(texture, 80, 84, 44, new Color32(245, 208, 174, 255));
        FillCircle(texture, 80, 56, 41, shirt);
        FillRect(texture, 39, 30, 82, 42, shirt);
        FillCircle(texture, 61, 83, 7, new Color32(244, 191, 150, 255));
        FillCircle(texture, 99, 83, 7, new Color32(244, 191, 150, 255));

        FillCircle(texture, 80, 108, 34, hair);
        FillRect(texture, 46, 89, 68, 24, hair);
        FillCircle(texture, 58, 101, 13, hair);
        FillCircle(texture, 102, 101, 13, hair);

        FillCircle(texture, 66, 84, 3, new Color32(15, 23, 42, 255));
        FillCircle(texture, 94, 84, 3, new Color32(15, 23, 42, 255));
        FillRect(texture, 73, 73, 14, 3, new Color32(185, 104, 72, 255));
        FillRect(texture, 65, 64, 30, 3, new Color32(120, 53, 15, 255));

        switch (style)
        {
            case 0:
                DrawHeadphones(texture);
                FillRect(texture, 48, 38, 64, 6, new Color32(20, 184, 166, 255));
                break;
            case 1:
                DrawGlasses(texture);
                FillRect(texture, 58, 45, 44, 7, new Color32(148, 163, 184, 255));
                FillRect(texture, 72, 33, 16, 22, new Color32(255, 255, 255, 255));
                break;
            case 2:
                FillRect(texture, 47, 104, 66, 8, new Color32(239, 68, 68, 255));
                FillRect(texture, 50, 40, 60, 7, new Color32(187, 247, 208, 255));
                break;
            case 3:
                FillRect(texture, 64, 128, 32, 6, new Color32(234, 179, 8, 255));
                FillCircle(texture, 64, 134, 5, new Color32(234, 179, 8, 255));
                FillCircle(texture, 80, 138, 7, new Color32(234, 179, 8, 255));
                FillCircle(texture, 96, 134, 5, new Color32(234, 179, 8, 255));
                FillRect(texture, 57, 40, 46, 8, new Color32(253, 224, 71, 255));
                break;
            default:
                FillCircle(texture, 53, 120, 8, hair);
                FillCircle(texture, 108, 119, 9, hair);
                DrawSpark(texture, 121, 124, new Color32(250, 204, 21, 255));
                DrawGlasses(texture);
                break;
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        EnsureImportedSprite(path, 128);
    }

    private static void Fill(Texture2D texture, Color32 color)
    {
        var pixels = new Color32[texture.width * texture.height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels32(pixels);
    }

    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
    {
        for (var yy = y; yy < y + height; yy++)
        {
            for (var xx = x; xx < x + width; xx++)
            {
                SetPixel(texture, xx, yy, color);
            }
        }
    }

    private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color32 color)
    {
        var radiusSquared = radius * radius;
        for (var y = centerY - radius; y <= centerY + radius; y++)
        {
            for (var x = centerX - radius; x <= centerX + radius; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    SetPixel(texture, x, y, color);
                }
            }
        }
    }

    private static void DrawHeadphones(Texture2D texture)
    {
        FillRect(texture, 45, 91, 5, 26, new Color32(15, 23, 42, 255));
        FillRect(texture, 110, 91, 5, 26, new Color32(15, 23, 42, 255));
        FillCircle(texture, 48, 83, 9, new Color32(15, 23, 42, 255));
        FillCircle(texture, 112, 83, 9, new Color32(15, 23, 42, 255));
        FillRect(texture, 58, 125, 44, 5, new Color32(15, 23, 42, 255));
    }

    private static void DrawGlasses(Texture2D texture)
    {
        FillRect(texture, 55, 88, 23, 3, new Color32(15, 23, 42, 255));
        FillRect(texture, 82, 88, 23, 3, new Color32(15, 23, 42, 255));
        FillRect(texture, 55, 76, 23, 3, new Color32(15, 23, 42, 255));
        FillRect(texture, 82, 76, 23, 3, new Color32(15, 23, 42, 255));
        FillRect(texture, 55, 76, 3, 15, new Color32(15, 23, 42, 255));
        FillRect(texture, 75, 76, 3, 15, new Color32(15, 23, 42, 255));
        FillRect(texture, 82, 76, 3, 15, new Color32(15, 23, 42, 255));
        FillRect(texture, 102, 76, 3, 15, new Color32(15, 23, 42, 255));
        FillRect(texture, 78, 82, 4, 3, new Color32(15, 23, 42, 255));
    }

    private static void DrawSpark(Texture2D texture, int centerX, int centerY, Color32 color)
    {
        FillRect(texture, centerX - 1, centerY - 10, 3, 21, color);
        FillRect(texture, centerX - 10, centerY - 1, 21, 3, color);
        FillRect(texture, centerX - 5, centerY - 5, 11, 11, new Color32(color.r, color.g, color.b, 190));
    }

    private static void SetPixel(Texture2D texture, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
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

    private static void CreateBoard3DPreview()
    {
        var root = new GameObject("Board3DPreview");
        root.transform.position = new Vector3(0f, 0f, 1.8f);

        var baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObject.name = "Board3D_Base";
        baseObject.transform.SetParent(root.transform);
        baseObject.transform.position = new Vector3(0f, -0.08f, 0.28f);
        baseObject.transform.localScale = new Vector3(5.1f, 0.16f, 4.1f);
        SetMeshColor(baseObject, new Color32(226, 232, 240, 255));

        for (var i = 0; i < BoardCount; i++)
        {
            var column = i % 5;
            var row = i / 5;
            if (row % 2 == 1)
            {
                column = 4 - column;
            }

            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"Board3D_Tile_{i + 1:00}";
            tile.transform.SetParent(root.transform);
            tile.transform.position = new Vector3((column - 2f) * 0.86f, 0.05f, (1.5f - row) * 0.82f);
            tile.transform.localScale = new Vector3(0.7f, 0.12f, 0.66f);
            SetMeshColor(tile, GetTileColor(i));
        }

        var pawnColors = new[]
        {
            new Color32(37, 99, 235, 255),
            new Color32(220, 38, 38, 255),
            new Color32(22, 163, 74, 255),
            new Color32(147, 51, 234, 255)
        };

        for (var i = 0; i < pawnColors.Length; i++)
        {
            var pawn = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pawn.name = $"Board3D_Pawn_{i + 1}";
            pawn.transform.SetParent(root.transform);
            pawn.transform.position = new Vector3(-1.82f + i * 0.2f, 0.45f, 1.2f);
            pawn.transform.localScale = new Vector3(0.15f, 0.28f, 0.15f);
            SetMeshColor(pawn, pawnColors[i]);
        }

        var lightObject = new GameObject("Board3D_KeyLight");
        lightObject.transform.SetParent(root.transform);
        lightObject.transform.position = new Vector3(-2f, 4f, -3f);
        lightObject.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.9f;

        var cameraObject = new GameObject("Board3D_PreviewCamera");
        cameraObject.transform.SetParent(root.transform);
        cameraObject.transform.position = new Vector3(0f, 5.2f, -5.8f);
        cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.5f;
        camera.enabled = false;

        root.SetActive(false);
    }

    private static void SetMeshColor(GameObject target, Color32 color)
    {
        var renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = color;
        renderer.sharedMaterial = material;
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
        StatIconSprites statIcons,
        CharacterPortraitSprites characterPortraits)
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
        CreateText(canvas.transform, "TitleText", "大学院生すごろく", new Vector2(-350f, 236f), new Vector2(260f, 36f), 24, new Color32(255, 255, 255, 255), TextAnchor.MiddleLeft);
        CreateText(canvas.transform, "TurnText", "Player 1's turn", new Vector2(0f, 236f), new Vector2(300f, 36f), 29, new Color32(96, 165, 250, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "DiceResultText", "Ready", new Vector2(360f, 236f), new Vector2(210f, 32f), 18, new Color32(226, 232, 240, 255), TextAnchor.MiddleRight);
        CreateButton(canvas.transform, "PauseButton", "||", new Vector2(452f, 236f), new Vector2(46f, 36f), buttonSprite, new Color32(71, 85, 105, 255));
        CreateText(canvas.transform, "EventLogText", "研究生活、開始。", new Vector2(0f, 195f), new Vector2(520f, 28f), 17, new Color32(30, 41, 59, 255), TextAnchor.MiddleCenter);

        CreatePlayerCard(canvas.transform, "Player1", new Vector2(-315f, -187f), new Color32(37, 99, 235, 255), panelSprite, progressTrackSprite, progressFillSprite, statIcons);
        CreatePlayerCard(canvas.transform, "Player2", new Vector2(-105f, -187f), new Color32(220, 38, 38, 255), panelSprite, progressTrackSprite, progressFillSprite, statIcons);
        CreatePlayerCard(canvas.transform, "Player3", new Vector2(105f, -187f), new Color32(22, 163, 74, 255), panelSprite, progressTrackSprite, progressFillSprite, statIcons);
        CreatePlayerCard(canvas.transform, "Player4", new Vector2(315f, -187f), new Color32(147, 51, 234, 255), panelSprite, progressTrackSprite, progressFillSprite, statIcons);

        CreatePanel(canvas.transform, "DiceStatusPanel", panelSprite, new Vector2(0f, -252f), new Vector2(150f, 52f), new Color32(255, 255, 255, 240));
        CreateText(canvas.transform, "DiceLabelText", "DICE", new Vector2(-48f, -252f), new Vector2(60f, 20f), 12, new Color32(100, 116, 139, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "RollStateText", "READY", new Vector2(32f, -252f), new Vector2(80f, 28f), 23, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);
        CreateButton(canvas.transform, "SkillButton", "一旦逃避", new Vector2(355f, -252f), new Vector2(160f, 44f), buttonSprite, new Color32(99, 102, 241, 255));

        var winnerPanel = CreatePanel(canvas.transform, "WinnerPanel", panelSprite, new Vector2(0f, 42f), new Vector2(680f, 344f), new Color32(255, 255, 255, 248));
        CreateText(winnerPanel.transform, "WhoWinsText", "進路発表", new Vector2(0f, 132f), new Vector2(560f, 40f), 30, new Color32(15, 23, 42, 255), TextAnchor.MiddleCenter);
        CreateText(winnerPanel.transform, "ResultSummaryText", string.Empty, new Vector2(0f, -108f), new Vector2(600f, 70f), 12, new Color32(71, 85, 105, 255), TextAnchor.UpperLeft);
        CreateResultCard(winnerPanel.transform, "ResultP1", new Vector2(-170f, 56f), new Color32(37, 99, 235, 255), panelSprite);
        CreateResultCard(winnerPanel.transform, "ResultP2", new Vector2(170f, 56f), new Color32(220, 38, 38, 255), panelSprite);
        CreateResultCard(winnerPanel.transform, "ResultP3", new Vector2(-170f, -36f), new Color32(22, 163, 74, 255), panelSprite);
        CreateResultCard(winnerPanel.transform, "ResultP4", new Vector2(170f, -36f), new Color32(147, 51, 234, 255), panelSprite);

        CreateText(canvas.transform, "Player1MoveText", "Player 1", new Vector2(-78f, 196f), new Vector2(120f, 28f), 17, new Color32(96, 165, 250, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "Player2MoveText", "Player 2", new Vector2(78f, 196f), new Vector2(120f, 28f), 17, new Color32(248, 113, 113, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "Player3MoveText", "Player 3", new Vector2(234f, 196f), new Vector2(120f, 28f), 17, new Color32(74, 222, 128, 255), TextAnchor.MiddleCenter);
        CreateText(canvas.transform, "Player4MoveText", "Player 4", new Vector2(390f, 196f), new Vector2(120f, 28f), 17, new Color32(192, 132, 252, 255), TextAnchor.MiddleCenter);

        CreateEventModal(canvas.transform, panelSprite, buttonSprite);
        CreateMenuScreens(canvas.transform, panelSprite, buttonSprite, characterPortraits);
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
        var panel = CreatePanel(parent, "EventModalPanel", panelSprite, new Vector2(0f, 30f), new Vector2(600f, 300f), new Color32(255, 255, 255, 250));
        CreatePanel(panel.transform, "EventModalPortraitFrame", panelSprite, new Vector2(-236f, 54f), new Vector2(84f, 84f), new Color32(241, 245, 249, 255));
        CreateImage(panel.transform, "EventModalPortraitImage", null, new Vector2(-236f, 54f), new Vector2(72f, 72f), new Color32(255, 255, 255, 255));
        CreateText(panel.transform, "EventModalEyebrowText", "EVENT", new Vector2(-154f, 112f), new Vector2(72f, 20f), 13, new Color32(100, 116, 139, 255), TextAnchor.MiddleLeft);
        CreateText(panel.transform, "EventModalTitleText", "イベント", new Vector2(66f, 82f), new Vector2(420f, 34f), 24, new Color32(15, 23, 42, 255), TextAnchor.MiddleLeft);
        CreateText(panel.transform, "EventModalDescriptionText", "説明", new Vector2(66f, 36f), new Vector2(420f, 52f), 15, new Color32(51, 65, 85, 255), TextAnchor.MiddleLeft);

        CreateButton(panel.transform, "EventChoice1Button", "選択肢1", new Vector2(0f, -30f), new Vector2(520f, 42f), buttonSprite, new Color32(37, 99, 235, 255));
        CreateButton(panel.transform, "EventChoice2Button", "選択肢2", new Vector2(0f, -82f), new Vector2(520f, 42f), buttonSprite, new Color32(15, 23, 42, 255));
        CreateButton(panel.transform, "EventChoice3Button", "選択肢3", new Vector2(0f, -134f), new Vector2(520f, 42f), buttonSprite, new Color32(71, 85, 105, 255));
    }

    private static void CreateMenuScreens(Transform parent, Sprite panelSprite, Sprite buttonSprite, CharacterPortraitSprites characterPortraits)
    {
        var titlePanel = CreatePanel(parent, "TitlePanel", panelSprite, Vector2.zero, new Vector2(960f, 540f), new Color32(15, 23, 42, 248));
        CreateText(titlePanel.transform, "TitleScreenTitleText", "大学院生すごろく", new Vector2(0f, 124f), new Vector2(620f, 60f), 44, new Color32(255, 255, 255, 255), TextAnchor.MiddleCenter);
        CreateText(titlePanel.transform, "TitleScreenSubtitleText", "金、メンタル、IF、徳で生き残る研究生活", new Vector2(0f, 72f), new Vector2(540f, 28f), 15, new Color32(203, 213, 225, 255), TextAnchor.MiddleCenter);
        CreateTitlePortrait(titlePanel.transform, "TitlePortraitHobby", characterPortraits.Hobby, new Vector2(-192f, -160f), "多趣味");
        CreateTitlePortrait(titlePanel.transform, "TitlePortraitSerious", characterPortraits.Serious, new Vector2(-96f, -160f), "真面目");
        CreateTitlePortrait(titlePanel.transform, "TitlePortraitAthletic", characterPortraits.Athletic, new Vector2(0f, -160f), "体育会");
        CreateTitlePortrait(titlePanel.transform, "TitlePortraitRich", characterPortraits.Rich, new Vector2(96f, -160f), "金持ち");
        CreateTitlePortrait(titlePanel.transform, "TitlePortraitGenius", characterPortraits.Genius, new Vector2(192f, -160f), "天才肌");
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
        var text = CreateText(buttonObject.transform, textName, label, Vector2.zero, size - new Vector2(28f, 0f), 15, new Color32(255, 255, 255, 255), TextAnchor.MiddleLeft);
        text.fontStyle = FontStyle.Bold;
    }

    private static void CreateResultCard(Transform parent, string prefix, Vector2 anchoredPosition, Color32 accentColor, Sprite panelSprite)
    {
        var card = CreatePanel(parent, $"{prefix}Card", panelSprite, anchoredPosition, new Vector2(300f, 72f), new Color32(248, 250, 252, 245));
        CreatePanel(card.transform, $"{prefix}PortraitFrame", panelSprite, new Vector2(-120f, 0f), new Vector2(50f, 50f), new Color32(255, 255, 255, 245));
        CreateImage(card.transform, $"{prefix}PortraitImage", null, new Vector2(-120f, 0f), new Vector2(42f, 42f), new Color32(255, 255, 255, 255));
        CreateText(card.transform, $"{prefix}RankText", "1位  P1", new Vector2(-60f, 18f), new Vector2(86f, 22f), 17, accentColor, TextAnchor.MiddleLeft);
        CreateText(card.transform, $"{prefix}CareerText", "進路", new Vector2(74f, 18f), new Vector2(134f, 22f), 18, new Color32(15, 23, 42, 255), TextAnchor.MiddleLeft);
        CreateText(card.transform, $"{prefix}ScoreText", "Score 0", new Vector2(42f, -16f), new Vector2(214f, 24f), 12, new Color32(71, 85, 105, 255), TextAnchor.MiddleLeft);
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
        CreatePanel(parent, $"{playerName}Panel", panelSprite, anchoredPosition, new Vector2(198f, 100f), new Color32(255, 255, 255, 235));
        CreatePanel(parent, $"{playerName}PortraitFrame", panelSprite, anchoredPosition + new Vector2(-76f, 24f), new Vector2(38f, 38f), new Color32(241, 245, 249, 255));
        CreateImage(parent, $"{playerName}PortraitImage", null, anchoredPosition + new Vector2(-76f, 24f), new Vector2(32f, 32f), new Color32(255, 255, 255, 255));
        CreateText(parent, $"{playerName}StatusText", playerName.Replace("Player", "P"), anchoredPosition + new Vector2(1f, 28f), new Vector2(102f, 22f), 14, accentColor, TextAnchor.MiddleLeft);
        CreateText(parent, $"{playerName}PositionText", "1/20", anchoredPosition + new Vector2(63f, 28f), new Vector2(58f, 22f), 15, new Color32(15, 23, 42, 255), TextAnchor.MiddleRight);
        CreateStatChip(parent, $"{playerName}Money", statIcons.Money, "0", anchoredPosition + new Vector2(-69f, 2f), new Color32(34, 197, 94, 255));
        CreateStatChip(parent, $"{playerName}If", statIcons.IfPoint, "0", anchoredPosition + new Vector2(-23f, 2f), new Color32(234, 179, 8, 255));
        CreateStatChip(parent, $"{playerName}Mental", statIcons.Mental, "0", anchoredPosition + new Vector2(23f, 2f), new Color32(244, 63, 94, 255));
        CreateStatChip(parent, $"{playerName}Virtue", statIcons.Virtue, "0", anchoredPosition + new Vector2(69f, 2f), new Color32(147, 51, 234, 255));
        CreateProgressBar(parent, playerName, anchoredPosition + new Vector2(0f, -31f), accentColor, progressTrackSprite, progressFillSprite);
    }

    private static void CreateTitlePortrait(Transform parent, string name, Sprite sprite, Vector2 anchoredPosition, string label)
    {
        CreateImage(parent, $"{name}Image", sprite, anchoredPosition + new Vector2(0f, 14f), new Vector2(64f, 64f), new Color32(255, 255, 255, 255));
        CreateText(parent, $"{name}Text", label, anchoredPosition + new Vector2(0f, -30f), new Vector2(72f, 18f), 12, new Color32(226, 232, 240, 255), TextAnchor.MiddleCenter);
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

    private static Text CreateText(
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
        ApplyGeneratedTextStyle(name, text);
        return text;
    }

    private static void ApplyGeneratedTextStyle(string name, Text text)
    {
        text.lineSpacing = 0.92f;

        if (name.Contains("Title") || name.Contains("WhoWins") || name.Contains("TurnText") || name.Contains("RollState"))
        {
            text.fontStyle = FontStyle.Bold;
            return;
        }

        if (name.Contains("RankText") || name.Contains("CareerText") || name.Contains("StatusText") || name.Contains("PositionText"))
        {
            text.fontStyle = FontStyle.Bold;
            return;
        }

        if (name.Contains("MoneyText") || name.Contains("IfText") || name.Contains("MentalText") || name.Contains("VirtueText"))
        {
            text.fontStyle = FontStyle.Bold;
            return;
        }

        if (name.Contains("Eyebrow") || name.Contains("Label") || name.Contains("Subtitle") || name.Contains("ScoreText"))
        {
            text.fontStyle = FontStyle.Italic;
            return;
        }

        if (name.Contains("ButtonText") || name == "SkillButtonText")
        {
            text.fontStyle = FontStyle.Bold;
        }
    }

    private sealed class StatIconSprites
    {
        public Sprite Money;
        public Sprite IfPoint;
        public Sprite Mental;
        public Sprite Virtue;
    }

    private sealed class CharacterPortraitSprites
    {
        public Sprite Hobby;
        public Sprite Serious;
        public Sprite Athletic;
        public Sprite Rich;
        public Sprite Genius;
    }
}

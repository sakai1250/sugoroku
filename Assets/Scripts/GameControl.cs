using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// ゲーム全体の状態とターン管理を行う。
public class GameControl : MonoBehaviour
{
    private const int MaxPlayers = 4;
    private static int activePlayerCount = 4;
    private static int humanPlayerCount = 1;

    private static GameObject whoWinsText;
    private static GameObject winnerPanel;
    private static GameObject titlePanel;
    private static GameObject settingsPanel;
    private static GameObject achievementsPanel;
    private static GameObject pausePanel;
    private static GameObject reconnectPanel;
    private static Text turnText;
    private static Text diceResultText;
    private static Text rollStateText;
    private static Text eventLogText;
    private static Button skillButton;
    private static Text skillButtonText;
    private static Text resultSummaryText;
    private static Text settingsVolumeText;
    private static Text settingsPlayersText;
    private static Text settingsCpuText;
    private static Text achievementsText;
    private static GameObject eventModalPanel;
    private static Text eventModalTitleText;
    private static Text eventModalDescriptionText;
    private static readonly Button[] eventChoiceButtons = new Button[3];
    private static readonly Text[] eventChoiceTexts = new Text[3];
    private static readonly PlayerRuntime[] players = new PlayerRuntime[MaxPlayers];
    private static GameControl instance;
    private static EventDatabase eventDatabase;
    private static int currentPlayerIndex;
    private static int movingPlayerIndex = -1;
    private static bool gameStarted;
    private static bool isPaused;
    private static bool connectionInterrupted;
    private static float masterVolume = 0.8f;
    private static PlayerRuntime pendingEventPlayer;
    private static EventDefinition pendingEvent;
    private static int pendingEventSquare;
    private static bool waitingForEventChoice;

    public static int diceSideThrown = 0;
    public static bool gameOver = false;

    private void Start()
    {
        instance = this;
        diceSideThrown = 0;
        currentPlayerIndex = 0;
        movingPlayerIndex = -1;
        gameStarted = false;
        isPaused = false;
        connectionInterrupted = false;
        gameOver = false;
        waitingForEventChoice = false;
        pendingEventPlayer = null;
        pendingEvent = null;
        pendingEventSquare = 0;

        whoWinsText = GameObject.Find("WhoWinsText");
        winnerPanel = GameObject.Find("WinnerPanel");
        titlePanel = GameObject.Find("TitlePanel");
        settingsPanel = GameObject.Find("SettingsPanel");
        achievementsPanel = GameObject.Find("AchievementsPanel");
        pausePanel = GameObject.Find("PausePanel");
        reconnectPanel = GameObject.Find("ReconnectPanel");
        turnText = FindText("TurnText");
        diceResultText = FindText("DiceResultText");
        rollStateText = FindText("RollStateText");
        eventLogText = FindText("EventLogText");
        var skillButtonObject = GameObject.Find("SkillButton");
        skillButton = skillButtonObject == null ? null : skillButtonObject.GetComponent<Button>();
        skillButtonText = FindText("SkillButtonText");
        resultSummaryText = FindText("ResultSummaryText");
        settingsVolumeText = FindText("SettingsVolumeText");
        settingsPlayersText = FindText("SettingsPlayersText");
        settingsCpuText = FindText("SettingsCpuText");
        achievementsText = FindText("AchievementsText");
        eventModalPanel = GameObject.Find("EventModalPanel");
        eventModalTitleText = FindText("EventModalTitleText");
        eventModalDescriptionText = FindText("EventModalDescriptionText");

        if (skillButton != null)
        {
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(UseActiveSkill);
        }

        BindButton("StartGameButton", StartGame);
        BindButton("OpenSettingsButton", OpenSettings);
        BindButton("OpenAchievementsButton", OpenAchievements);
        BindButton("SettingsBackButton", ShowTitleScreen);
        BindButton("AchievementsBackButton", ShowTitleScreen);
        BindButton("VolumeDownButton", () => ChangeVolume(-0.1f));
        BindButton("VolumeUpButton", () => ChangeVolume(0.1f));
        BindButton("PlayersDownButton", () => ChangePlayerCount(-1));
        BindButton("PlayersUpButton", () => ChangePlayerCount(1));
        BindButton("CpuDownButton", () => ChangeCpuCount(-1));
        BindButton("CpuUpButton", () => ChangeCpuCount(1));
        BindButton("PauseButton", PauseGame);
        BindButton("ResumeButton", ResumeGame);
        BindButton("QuitToTitleButton", QuitToTitle);
        BindButton("GiveUpButton", GiveUpGame);
        BindButton("SimulateDisconnectButton", SimulateDisconnect);
        BindButton("ReconnectButton", ReconnectGame);

        for (var i = 0; i < eventChoiceButtons.Length; i++)
        {
            var choiceIndex = i;
            var buttonObject = GameObject.Find($"EventChoice{i + 1}Button");
            eventChoiceButtons[i] = buttonObject == null ? null : buttonObject.GetComponent<Button>();
            eventChoiceTexts[i] = FindText($"EventChoice{i + 1}Text");
            if (eventChoiceButtons[i] != null)
            {
                eventChoiceButtons[i].onClick.RemoveAllListeners();
                eventChoiceButtons[i].onClick.AddListener(() => SelectEventChoice(choiceIndex));
            }
        }

        if (whoWinsText != null)
        {
            whoWinsText.SetActive(false);
        }

        if (winnerPanel != null)
        {
            winnerPanel.SetActive(false);
        }

        HideEventModal();
        SetPanel(pausePanel, false);
        SetPanel(reconnectPanel, false);
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        activePlayerCount = Mathf.Clamp(PlayerPrefs.GetInt("ActivePlayerCount", 4), 1, MaxPlayers);
        var savedCpuCount = Mathf.Clamp(PlayerPrefs.GetInt("CpuPlayerCount", activePlayerCount - 1), 0, activePlayerCount - 1);
        humanPlayerCount = activePlayerCount - savedCpuCount;
        AudioListener.volume = masterVolume;
        UpdateSettingsText();
        LoadEventDatabase();

        for (var i = 0; i < MaxPlayers; i++)
        {
            var playerNumber = i + 1;
            var playerObject = GameObject.Find($"Player{playerNumber}");
            var follower = playerObject == null ? null : playerObject.GetComponent<FollowThePath>();
            if (follower != null)
            {
                follower.moveAllowed = false;
            }

            players[i] = new PlayerRuntime
            {
                Number = playerNumber,
                Object = playerObject,
                Follower = follower,
                State = GradStudentState.CreateRandom(),
                CpuControlled = i >= humanPlayerCount && i < activePlayerCount,
                StatusText = FindText($"Player{playerNumber}StatusText"),
                StatsText = FindText($"Player{playerNumber}StatsText"),
                MoneyText = FindText($"Player{playerNumber}MoneyText"),
                IfText = FindText($"Player{playerNumber}IfText"),
                MentalText = FindText($"Player{playerNumber}MentalText"),
                VirtueText = FindText($"Player{playerNumber}VirtueText"),
                PositionText = FindText($"Player{playerNumber}PositionText"),
                PanelImage = FindImage($"Player{playerNumber}Panel"),
                ProgressFill = FindImage($"Player{playerNumber}ProgressFill"),
                TurnTextObject = GameObject.Find($"Player{playerNumber}MoveText")
            };

            SetVisible(players[i], i < activePlayerCount);
        }

        WriteEvent(BuildCharacterSummary());
        UpdateHud("Ready");
        ShowTitleScreen();
    }

    private void Update()
    {
        if (gameOver || isPaused || connectionInterrupted || movingPlayerIndex < 0)
        {
            return;
        }

        var player = players[movingPlayerIndex];
        if (player.Follower == null)
        {
            CompleteTurn();
            return;
        }

        if (player.Follower.waypointIndex >= player.Follower.waypoints.Length ||
            player.Follower.waypointIndex > player.StartWaypoint + diceSideThrown)
        {
            player.Follower.moveAllowed = false;
            player.StartWaypoint = player.Follower.waypointIndex - 1;
            if (!ResolveSquare(player, player.StartWaypoint + 1))
            {
                return;
            }

            FinalizeTurnAfterSquare(player);
        }
    }

    public static bool CanRoll()
    {
        var currentPlayer = GetCurrentPlayer();
        return gameStarted && !isPaused && !connectionInterrupted && !gameOver && !waitingForEventChoice && movingPlayerIndex < 0 && currentPlayer != null && !currentPlayer.CpuControlled;
    }

    public static bool CanAutoRoll()
    {
        var currentPlayer = GetCurrentPlayer();
        return gameStarted && !isPaused && !connectionInterrupted && !gameOver && !waitingForEventChoice && movingPlayerIndex < 0 && currentPlayer != null && currentPlayer.CpuControlled;
    }

    public static int GetCurrentPlayerNumber()
    {
        var player = GetCurrentPlayer();
        return player == null ? 1 : player.Number;
    }

    public static bool ConsumeSkipTurn(int playerNumber)
    {
        var player = GetPlayer(playerNumber);
        if (player == null || player.State.SkipTurns <= 0)
        {
            return false;
        }

        player.State.SkipTurns -= 1;
        ShowRoll(playerNumber, "Skip");
        WriteEvent($"P{playerNumber}: 1回休み");
        AdvanceTurn();
        UpdateHud("Skip");
        return true;
    }

    public static void MovePlayer(int playerNumber)
    {
        var player = GetPlayer(playerNumber);
        if (player == null || player.Follower == null || player.Finished || player.Eliminated)
        {
            AdvanceTurn();
            return;
        }

        ShowRoll(playerNumber, diceSideThrown);
        movingPlayerIndex = player.Index;
        player.Follower.moveAllowed = true;
        UpdateHud(diceResultText == null ? string.Empty : diceResultText.text);
    }

    public static void ShowRoll(int playerNumber, int diceSide)
    {
        ShowRoll(playerNumber, diceSide.ToString());
        if (diceResultText != null)
        {
            diceResultText.text = $"P{playerNumber} rolled {diceSide}";
        }
    }

    private static void ShowRoll(int playerNumber, string value)
    {
        if (diceResultText != null)
        {
            diceResultText.text = $"P{playerNumber}: {value}";
        }

        if (rollStateText != null)
        {
            rollStateText.text = value;
            rollStateText.color = GetPlayerColor(playerNumber);
        }
    }

    private static void CompleteTurn()
    {
        movingPlayerIndex = -1;
        if (gameOver)
        {
            return;
        }

        if (ShouldFinishGame())
        {
            FinishGame();
            return;
        }

        AdvanceTurn();
        UpdateHud(diceResultText == null ? string.Empty : diceResultText.text);
    }

    private static void StartGame()
    {
        gameStarted = true;
        isPaused = false;
        connectionInterrupted = false;
        SetPanel(titlePanel, false);
        SetPanel(settingsPanel, false);
        SetPanel(achievementsPanel, false);
        SetPanel(pausePanel, false);
        SetPanel(reconnectPanel, false);
        WriteEvent(BuildCharacterSummary());
        UpdateHud("Ready");
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayConfirm();
        }
    }

    private static void ShowTitleScreen()
    {
        SetPanel(titlePanel, true);
        SetPanel(settingsPanel, false);
        SetPanel(achievementsPanel, false);
        UpdateAchievementsText();
    }

    private static void OpenSettings()
    {
        SetPanel(titlePanel, false);
        SetPanel(settingsPanel, true);
        SetPanel(achievementsPanel, false);
        UpdateSettingsText();
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayClick();
        }
    }

    private static void OpenAchievements()
    {
        SetPanel(titlePanel, false);
        SetPanel(settingsPanel, false);
        SetPanel(achievementsPanel, true);
        UpdateAchievementsText();
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayClick();
        }
    }

    private static void ChangeVolume(float delta)
    {
        masterVolume = Mathf.Clamp01(masterVolume + delta);
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
        UpdateSettingsText();
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayClick();
        }
    }

    private static void ChangePlayerCount(int delta)
    {
        activePlayerCount = Mathf.Clamp(activePlayerCount + delta, 1, MaxPlayers);
        humanPlayerCount = Mathf.Clamp(humanPlayerCount, 1, activePlayerCount);
        SavePlayerSettings();
        ApplyPlayerSettings();
        UpdateSettingsText();
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayClick();
        }
    }

    private static void ChangeCpuCount(int delta)
    {
        var cpuCount = Mathf.Clamp(activePlayerCount - humanPlayerCount + delta, 0, activePlayerCount - 1);
        humanPlayerCount = activePlayerCount - cpuCount;
        SavePlayerSettings();
        ApplyPlayerSettings();
        UpdateSettingsText();
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayClick();
        }
    }

    private static void SavePlayerSettings()
    {
        PlayerPrefs.SetInt("ActivePlayerCount", activePlayerCount);
        PlayerPrefs.SetInt("CpuPlayerCount", activePlayerCount - humanPlayerCount);
        PlayerPrefs.Save();
    }

    private static void ApplyPlayerSettings()
    {
        for (var i = 0; i < MaxPlayers; i++)
        {
            if (players[i] == null)
            {
                continue;
            }

            players[i].CpuControlled = i >= humanPlayerCount && i < activePlayerCount;
            SetVisible(players[i], i < activePlayerCount);
        }

        if (currentPlayerIndex >= activePlayerCount)
        {
            currentPlayerIndex = 0;
        }

        UpdateHud(diceResultText == null ? string.Empty : diceResultText.text);
    }

    private static void UpdateSettingsText()
    {
        if (settingsVolumeText != null)
        {
            settingsVolumeText.text = $"SE Volume {Mathf.RoundToInt(masterVolume * 100f)}%";
        }

        if (settingsPlayersText != null)
        {
            settingsPlayersText.text = $"Players {activePlayerCount}";
        }

        if (settingsCpuText != null)
        {
            settingsCpuText.text = $"CPU {activePlayerCount - humanPlayerCount}";
        }
    }

    private static void UpdateAchievementsText()
    {
        if (achievementsText != null)
        {
            achievementsText.text =
                $"修了回数 {PlayerPrefs.GetInt("CompletedGames", 0)}\n" +
                $"最高スコア {PlayerPrefs.GetInt("BestScore", 0)}\n" +
                $"最後の進路 {PlayerPrefs.GetString("LastCareer", "未記録")}";
        }
    }

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static void PauseGame()
    {
        if (!gameStarted || gameOver || connectionInterrupted)
        {
            return;
        }

        isPaused = true;
        SaveRecoveryState();
        SetPanel(pausePanel, true);
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayClick();
        }
    }

    private static void ResumeGame()
    {
        isPaused = false;
        SetPanel(pausePanel, false);
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayConfirm();
        }
    }

    private static void QuitToTitle()
    {
        isPaused = false;
        gameStarted = false;
        SetPanel(pausePanel, false);
        ShowTitleScreen();
    }

    private static void GiveUpGame()
    {
        var player = GetCurrentPlayer();
        if (player != null)
        {
            EliminatePlayer(player, "諦めた");
        }

        SetPanel(pausePanel, false);
        isPaused = false;
        if (ShouldFinishGame())
        {
            FinishGame();
            return;
        }

        AdvanceTurn();
        UpdateHud("Give up");
    }

    private static void SimulateDisconnect()
    {
        if (!gameStarted || gameOver)
        {
            return;
        }

        SaveRecoveryState();
        connectionInterrupted = true;
        SetPanel(reconnectPanel, true);
        WriteEvent("通信切断: 復帰待機");
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayError();
        }
    }

    private static void ReconnectGame()
    {
        RestoreRecoveryState();
        connectionInterrupted = false;
        SetPanel(reconnectPanel, false);
        UpdateHud("Reconnected");
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayConfirm();
        }
    }

    private static void SaveRecoveryState()
    {
        PlayerPrefs.SetInt("RecoveryExists", 1);
        PlayerPrefs.SetInt("RecoveryCurrentPlayer", currentPlayerIndex);
        PlayerPrefs.SetInt("RecoveryActivePlayers", activePlayerCount);
        PlayerPrefs.SetInt("RecoveryCpuPlayers", activePlayerCount - humanPlayerCount);
        for (var i = 0; i < activePlayerCount; i++)
        {
            var player = players[i];
            if (player == null)
            {
                continue;
            }

            PlayerPrefs.SetInt($"RecoveryP{i}Money", player.State.Money);
            PlayerPrefs.SetInt($"RecoveryP{i}Mental", player.State.Mental);
            PlayerPrefs.SetInt($"RecoveryP{i}If", player.State.IfScore);
            PlayerPrefs.SetInt($"RecoveryP{i}Virtue", player.State.Virtue);
            PlayerPrefs.SetInt($"RecoveryP{i}Position", player.StartWaypoint);
            PlayerPrefs.SetInt($"RecoveryP{i}Finished", player.Finished ? 1 : 0);
            PlayerPrefs.SetInt($"RecoveryP{i}Eliminated", player.Eliminated ? 1 : 0);
        }

        PlayerPrefs.Save();
    }

    private static void RestoreRecoveryState()
    {
        if (PlayerPrefs.GetInt("RecoveryExists", 0) == 0)
        {
            return;
        }

        activePlayerCount = Mathf.Clamp(PlayerPrefs.GetInt("RecoveryActivePlayers", activePlayerCount), 1, MaxPlayers);
        humanPlayerCount = activePlayerCount - Mathf.Clamp(PlayerPrefs.GetInt("RecoveryCpuPlayers", activePlayerCount - humanPlayerCount), 0, activePlayerCount - 1);
        currentPlayerIndex = Mathf.Clamp(PlayerPrefs.GetInt("RecoveryCurrentPlayer", 0), 0, activePlayerCount - 1);

        for (var i = 0; i < activePlayerCount; i++)
        {
            var player = players[i];
            if (player == null)
            {
                continue;
            }

            player.State.Money = PlayerPrefs.GetInt($"RecoveryP{i}Money", player.State.Money);
            player.State.Mental = PlayerPrefs.GetInt($"RecoveryP{i}Mental", player.State.Mental);
            player.State.IfScore = PlayerPrefs.GetInt($"RecoveryP{i}If", player.State.IfScore);
            player.State.Virtue = PlayerPrefs.GetInt($"RecoveryP{i}Virtue", player.State.Virtue);
            player.StartWaypoint = PlayerPrefs.GetInt($"RecoveryP{i}Position", player.StartWaypoint);
            player.Finished = PlayerPrefs.GetInt($"RecoveryP{i}Finished", player.Finished ? 1 : 0) == 1;
            player.Eliminated = PlayerPrefs.GetInt($"RecoveryP{i}Eliminated", player.Eliminated ? 1 : 0) == 1;
            if (player.Follower != null && player.Follower.waypoints.Length > 0)
            {
                player.Follower.waypointIndex = Mathf.Clamp(player.StartWaypoint, 0, player.Follower.waypoints.Length - 1);
                player.Object.transform.position = player.Follower.waypoints[player.Follower.waypointIndex].position;
            }
        }

        ApplyPlayerSettings();
    }

    private static void AdvanceTurn()
    {
        var previousPlayer = GetCurrentPlayer();
        if (previousPlayer != null)
        {
            previousPlayer.ActiveSkillUsedThisTurn = false;
        }

        for (var i = 0; i < activePlayerCount; i++)
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % activePlayerCount;
            var candidate = players[currentPlayerIndex];
            if (candidate != null && !candidate.Finished && !candidate.Eliminated)
            {
                return;
            }
        }
    }

    private static bool ResolveSquare(PlayerRuntime player, int square)
    {
        if (gameOver)
        {
            return true;
        }

        var eventDefinition = GetEventForSquare(square);
        if (eventDefinition != null && player.State.IgnoreNextEvents <= 0)
        {
            ShowEventModal(player, square, eventDefinition);
            return false;
        }

        var message = ApplySquareEvent(player.Number, player.State, square);
        WriteEvent(message);
        CheckPlayerFailure(player, square);
        return true;
    }

    private static void CheckPlayerFailure(PlayerRuntime player, int square)
    {
        if (square >= 10 && player.State.IfScore == 0)
        {
            EliminatePlayer(player, "業績ゼロで進級不可");
            return;
        }

        if (player.State.Money <= 0)
        {
            EliminatePlayer(player, "破産");
            return;
        }

        if (player.State.Mental <= 0)
        {
            EliminatePlayer(player, "失踪");
        }
    }

    private static void FinalizeTurnAfterSquare(PlayerRuntime player)
    {
        if (!gameOver && !player.Eliminated && player.Follower != null && player.Follower.waypointIndex >= player.Follower.waypoints.Length)
        {
            player.Finished = true;
            WriteEvent($"P{player.Number}: 修了");
        }

        UpdateHud(diceResultText == null ? string.Empty : diceResultText.text);
        SaveRecoveryState();
        CompleteTurn();
    }

    private static string ApplySquareEvent(int playerNumber, GradStudentState state, int square)
    {
        if (state.IgnoreNextEvents > 0)
        {
            state.IgnoreNextEvents -= 1;
            return $"P{playerNumber}: 一旦逃避中 イベント回避";
        }

        if (square == 5 || square == 10 || square == 15)
        {
            const int tuition = 25;
            state.Money -= tuition;
            return $"P{playerNumber}: 学費 -{tuition}万円";
        }

        switch (square)
        {
            case 2:
            case 8:
                state.Mental = Mathf.Min(state.MaxMental, state.Mental + 12);
                return $"P{playerNumber}: 趣味で回復 メンタル+12";
            case 3:
                state.IfScore += 1;
                state.Mental -= MentalDamage(state, 8);
                return $"P{playerNumber}: 国内発表 IF+1 / メンタル減";
            case 4:
            case 13:
                state.Virtue += 2;
                state.Mental -= MentalDamage(state, 6);
                return $"P{playerNumber}: 雑務対応 徳+2";
            case 6:
                return LectureEvent(playerNumber, state);
            case 7:
            case 16:
                return EmergencyEvent(playerNumber, state);
            case 9:
                state.Money += 18;
                state.Mental -= MentalDamage(state, 5);
                return $"P{playerNumber}: TA給与 +18万円";
            case 11:
                state.IfScore += JournalIfGain(state);
                state.Mental -= MentalDamage(state, 14);
                return $"P{playerNumber}: ジャーナル投稿";
            case 12:
                state.Money += state.Virtue >= 3 ? 20 : 8;
                return state.Virtue >= 3 ? $"P{playerNumber}: 徳で奢り +20万円" : $"P{playerNumber}: 先輩の差し入れ +8万円";
            case 14:
                return PcTroubleEvent(playerNumber, state);
            case 17:
                state.Money -= 10;
                state.Mental -= MentalDamage(state, 10);
                return $"P{playerNumber}: 就活移動費 -10万円 / メンタル減";
            case 18:
                state.IfScore += 2;
                state.Mental -= MentalDamage(state, 18);
                return $"P{playerNumber}: 国際会議 IF+2";
            case 19:
                state.Virtue += 4;
                state.Mental = Mathf.Min(state.MaxMental, state.Mental + 6);
                return $"P{playerNumber}: 後輩救済 徳+4 / メンタル+6";
            default:
                if (state.Kind == GradStudentKind.Hobby && state.IgnoreNextEvents == 0)
                {
                    state.IgnoreNextEvents = 2;
                    state.Mental = Mathf.Min(state.MaxMental, state.Mental + Mathf.CeilToInt(state.MaxMental * 0.1f));
                    return $"P{playerNumber}: 趣味に逃避 次2回回避 / メンタル回復";
                }

                return $"P{playerNumber}: 研究室で平常運転";
        }
    }

    private static string LectureEvent(int playerNumber, GradStudentState state)
    {
        if (state.Kind == GradStudentKind.Serious)
        {
            state.IfScore += 1;
            return $"P{playerNumber}: 講義を完璧に処理 IF+1";
        }

        state.Mental -= MentalDamage(state, 7);
        return $"P{playerNumber}: 講義課題 メンタル減";
    }

    private static string EmergencyEvent(int playerNumber, GradStudentState state)
    {
        var damage = state.Kind == GradStudentKind.Serious ? 18 : 15;
        state.Mental -= MentalDamage(state, damage);
        state.Virtue += 1;
        return $"P{playerNumber}: 緊急対応 徳+1 / メンタル減";
    }

    private static string PcTroubleEvent(int playerNumber, GradStudentState state)
    {
        if (state.Kind == GradStudentKind.Rich && state.Money >= 20)
        {
            state.Money -= 20;
            return $"P{playerNumber}: 課金でPC復旧 -20万円";
        }

        state.Money -= 12;
        state.Mental -= MentalDamage(state, 12);
        return $"P{playerNumber}: PC故障 -12万円 / メンタル減";
    }

    private static int MentalDamage(GradStudentState state, int damage)
    {
        if (state.Kind == GradStudentKind.Athletic)
        {
            return Mathf.CeilToInt(damage * 0.5f);
        }

        if (state.Kind == GradStudentKind.Hobby && damage >= 10 && state.SkipTurns == 0)
        {
            state.SkipTurns = 1;
            return 0;
        }

        return damage;
    }

    private static int JournalIfGain(GradStudentState state)
    {
        return state.Kind == GradStudentKind.Genius ? Random.Range(1, 7) : 3;
    }

    private static void UseActiveSkill()
    {
        var player = GetCurrentPlayer();
        if (player == null || !CanUseActiveSkill(player))
        {
            if (UiSoundPlayer.Instance != null)
            {
                UiSoundPlayer.Instance.PlayError();
            }

            return;
        }

        player.ActiveSkillUsedThisTurn = true;
        player.State.IgnoreNextEvents = 2;
        player.State.Mental = Mathf.Min(
            player.State.MaxMental,
            player.State.Mental + Mathf.CeilToInt(player.State.MaxMental * 0.1f));

        WriteEvent($"P{player.Number}: 一旦逃避 次2回イベント回避 / メンタル回復");
        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayConfirm();
        }

        UpdateHud(diceResultText == null ? string.Empty : diceResultText.text);
    }

    private static bool CanUseActiveSkill(PlayerRuntime player)
    {
        return player != null
            && !player.CpuControlled
            && !player.ActiveSkillUsedThisTurn
            && !waitingForEventChoice
            && movingPlayerIndex < 0
            && !gameOver
            && player.State.Kind == GradStudentKind.Hobby
            && player.State.IgnoreNextEvents == 0;
    }

    private static EventDefinition GetEventForSquare(int square)
    {
        switch (square)
        {
            case 3:
            case 18:
                return GetEventById("E001", EventDefinition.Conference);
            case 4:
            case 13:
                return GetEventById("E002", EventDefinition.LateNightReview);
            case 6:
                return GetEventById("E005", EventDefinition.TeachingAssistant);
            case 7:
            case 16:
                return GetEventById("E003", EventDefinition.PartTimeShift);
            case 14:
                return GetEventById("E004", EventDefinition.ExpenseTrouble);
            default:
                return null;
        }
    }

    private static void LoadEventDatabase()
    {
        var json = Resources.Load<TextAsset>("EventMasters/events");
        if (json == null)
        {
            eventDatabase = null;
            return;
        }

        eventDatabase = JsonUtility.FromJson<EventDatabase>(json.text);
    }

    private static EventDefinition GetEventById(string eventId, System.Func<EventDefinition> fallback)
    {
        if (eventDatabase?.Events != null)
        {
            for (var i = 0; i < eventDatabase.Events.Length; i++)
            {
                var eventDefinition = eventDatabase.Events[i];
                if (eventDefinition != null && eventDefinition.EventId == eventId)
                {
                    return eventDefinition;
                }
            }
        }

        return fallback();
    }

    private static void ShowEventModal(PlayerRuntime player, int square, EventDefinition eventDefinition)
    {
        pendingEventPlayer = player;
        pendingEvent = eventDefinition;
        pendingEventSquare = square;
        waitingForEventChoice = true;

        if (eventModalPanel != null)
        {
            eventModalPanel.SetActive(true);
        }

        if (eventModalTitleText != null)
        {
            eventModalTitleText.text = eventDefinition.Title;
        }

        if (eventModalDescriptionText != null)
        {
            eventModalDescriptionText.text = eventDefinition.Description;
        }

        for (var i = 0; i < eventChoiceButtons.Length; i++)
        {
            var choice = i < eventDefinition.Choices.Length ? eventDefinition.Choices[i] : null;
            if (eventChoiceButtons[i] != null)
            {
                eventChoiceButtons[i].gameObject.SetActive(choice != null);
                eventChoiceButtons[i].interactable = choice != null && choice.CanChoose(player.State);
            }

            if (eventChoiceTexts[i] != null)
            {
                eventChoiceTexts[i].text = choice == null ? string.Empty : choice.BuildLabel();
            }
        }

        WriteEvent($"P{player.Number}: {eventDefinition.Title}");
        if (!player.CpuControlled && UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayClick();
        }

        if (player.CpuControlled && instance != null)
        {
            instance.StartCoroutine(SelectCpuChoiceAfterDelay());
        }

        UpdateSkillButton();
    }

    private static IEnumerator SelectCpuChoiceAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        if (waitingForEventChoice && pendingEventPlayer != null && pendingEventPlayer.CpuControlled)
        {
            SelectEventChoice(GetCpuChoiceIndex(pendingEventPlayer, pendingEvent));
        }
    }

    private static int GetCpuChoiceIndex(PlayerRuntime player, EventDefinition eventDefinition)
    {
        var bestIndex = 0;
        var bestScore = int.MinValue;
        for (var i = 0; i < eventDefinition.Choices.Length; i++)
        {
            var choice = eventDefinition.Choices[i];
            if (!choice.CanChoose(player.State))
            {
                continue;
            }

            var score = choice.MoneyDelta + choice.IfDelta * 12 + choice.VirtueDelta * 3 + choice.MentalDelta * 2;
            if (choice.AddSkipTurn)
            {
                score -= 12;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static void SelectEventChoice(int choiceIndex)
    {
        if (!waitingForEventChoice || pendingEventPlayer == null || pendingEvent == null ||
            choiceIndex < 0 || choiceIndex >= pendingEvent.Choices.Length)
        {
            return;
        }

        var choice = pendingEvent.Choices[choiceIndex];
        if (!choice.CanChoose(pendingEventPlayer.State))
        {
            if (UiSoundPlayer.Instance != null)
            {
                UiSoundPlayer.Instance.PlayError();
            }

            return;
        }

        if (UiSoundPlayer.Instance != null)
        {
            UiSoundPlayer.Instance.PlayConfirm();
        }

        ApplyChoice(pendingEventPlayer, pendingEvent, choice);
        HideEventModal();
        CheckPlayerFailure(pendingEventPlayer, pendingEventSquare);

        var player = pendingEventPlayer;
        pendingEventPlayer = null;
        pendingEvent = null;
        pendingEventSquare = 0;
        waitingForEventChoice = false;
        FinalizeTurnAfterSquare(player);
    }

    private static void ApplyChoice(PlayerRuntime player, EventDefinition eventDefinition, EventChoice choice)
    {
        var state = player.State;
        state.Money += choice.MoneyDelta;
        state.IfScore += choice.IfDelta;
        state.Virtue += choice.VirtueDelta;

        if (choice.MentalDelta < 0)
        {
            state.Mental -= MentalDamage(state, -choice.MentalDelta);
        }
        else if (choice.MentalDelta > 0)
        {
            state.Mental = Mathf.Min(state.MaxMental, state.Mental + choice.MentalDelta);
        }

        if (choice.AddSkipTurn)
        {
            state.SkipTurns += 1;
        }

        WriteEvent($"P{player.Number}: {eventDefinition.Title} - {choice.Label}");
    }

    private static void HideEventModal()
    {
        if (eventModalPanel != null)
        {
            eventModalPanel.SetActive(false);
        }
    }

    private static void EliminatePlayer(PlayerRuntime player, string reason)
    {
        player.Eliminated = true;
        if (player.Follower != null)
        {
            player.Follower.moveAllowed = false;
        }

        WriteEvent($"P{player.Number}: {reason}");
        player.EliminationReason = reason;
    }

    private static bool ShouldFinishGame()
    {
        var activeCount = 0;
        var unfinishedCount = 0;

        for (var i = 0; i < activePlayerCount; i++)
        {
            var player = players[i];
            if (player == null || player.Eliminated)
            {
                continue;
            }

            activeCount += 1;
            if (!player.Finished)
            {
                unfinishedCount += 1;
            }
        }

        return activeCount <= 1 || unfinishedCount == 0;
    }

    private static void FinishGame()
    {
        gameOver = true;
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(true);
        }

        if (whoWinsText != null)
        {
            whoWinsText.SetActive(true);
        }

        var winner = GetWinner();
        var result = winner == null ? "Draw" : $"Player {winner.Number} wins";
        if (whoWinsText != null)
        {
            whoWinsText.GetComponent<Text>().text = winner == null ? "全員脱落" : "進路発表";
        }

        if (turnText != null)
        {
            turnText.text = result;
            turnText.color = winner == null ? new Color32(15, 23, 42, 255) : GetPlayerColor(winner.Number);
        }

        if (diceResultText != null)
        {
            diceResultText.text = BuildScoreSummary();
        }

        if (resultSummaryText != null)
        {
            resultSummaryText.text = string.Empty;
            if (instance != null)
            {
                instance.StartCoroutine(RevealResultSummary(BuildDetailedResultSummary()));
            }
            else
            {
                resultSummaryText.text = BuildDetailedResultSummary();
            }
        }

        if (rollStateText != null)
        {
            rollStateText.text = "DONE";
        }

        WriteEvent(BuildResultSummary());
        UpdateHud(BuildScoreSummary());
        SaveAchievements(winner);
    }

    private static IEnumerator RevealResultSummary(string summary)
    {
        var lines = summary.Split('\n');
        var builder = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            builder.AppendLine(lines[i]);
            if (resultSummaryText != null)
            {
                resultSummaryText.text = builder.ToString();
            }

            yield return new WaitForSeconds(0.12f);
        }
    }

    private static void SaveAchievements(PlayerRuntime winner)
    {
        PlayerPrefs.SetInt("CompletedGames", PlayerPrefs.GetInt("CompletedGames", 0) + 1);
        if (winner != null)
        {
            var score = CalculateScore(winner.State);
            PlayerPrefs.SetInt("BestScore", Mathf.Max(PlayerPrefs.GetInt("BestScore", 0), score));
            PlayerPrefs.SetString("LastCareer", GetCareerName(winner));
        }

        PlayerPrefs.Save();
    }

    private static PlayerRuntime GetWinner()
    {
        PlayerRuntime winner = null;
        var bestScore = int.MinValue;

        for (var i = 0; i < activePlayerCount; i++)
        {
            var player = players[i];
            if (player == null || player.Eliminated)
            {
                continue;
            }

            var score = CalculateScore(player.State);
            if (score > bestScore)
            {
                bestScore = score;
                winner = player;
            }
        }

        return winner;
    }

    private static void UpdateHud(string diceText)
    {
        var currentPlayer = GetCurrentPlayer();
        if (turnText != null)
        {
            turnText.text = currentPlayer == null
                ? "Game Over"
                : $"Player {currentPlayer.Number}'s turn{(currentPlayer.CpuControlled ? " CPU" : string.Empty)}";
            turnText.color = currentPlayer == null ? new Color32(15, 23, 42, 255) : GetPlayerColor(currentPlayer.Number);
        }

        if (diceResultText != null && !string.IsNullOrEmpty(diceText))
        {
            diceResultText.text = diceText;
        }

        if (rollStateText != null && diceSideThrown == 0)
        {
            rollStateText.text = "READY";
            rollStateText.color = new Color32(28, 38, 48, 255);
        }

        for (var i = 0; i < activePlayerCount; i++)
        {
            UpdatePlayerHud(players[i], currentPlayer);
        }

        UpdateSkillButton();
    }

    private static void UpdateSkillButton()
    {
        if (skillButton == null)
        {
            return;
        }

        var currentPlayer = GetCurrentPlayer();
        var showButton = currentPlayer != null && !currentPlayer.CpuControlled && !gameOver;
        skillButton.gameObject.SetActive(showButton);
        skillButton.interactable = showButton && CanUseActiveSkill(currentPlayer);

        if (skillButtonText != null)
        {
            skillButtonText.text = currentPlayer != null && currentPlayer.State.Kind == GradStudentKind.Hobby
                ? "一旦逃避"
                : "スキルなし";
        }
    }

    private static void UpdatePlayerHud(PlayerRuntime player, PlayerRuntime currentPlayer)
    {
        if (player == null)
        {
            return;
        }

        var length = GetWaypointLength(player);
        var position = Mathf.Min(player.StartWaypoint + 1, length);

        if (player.StatusText != null)
        {
            var suffix = player.Eliminated ? " 脱落" : player.Finished ? " 修了" : string.Empty;
            var controller = player.CpuControlled ? " CPU" : string.Empty;
            player.StatusText.text = $"P{player.Number}{controller} {player.State.TypeName}{suffix}";
        }

        if (player.StatsText != null)
        {
            player.StatsText.text = FormatStats(player.State);
        }

        if (player.MoneyText != null)
        {
            player.MoneyText.text = player.State.Money.ToString();
        }

        if (player.IfText != null)
        {
            player.IfText.text = player.State.IfScore.ToString();
        }

        if (player.MentalText != null)
        {
            player.MentalText.text = player.State.Mental.ToString();
        }

        if (player.VirtueText != null)
        {
            player.VirtueText.text = player.State.Virtue.ToString();
        }

        if (player.PositionText != null)
        {
            player.PositionText.text = $"{position}/{length}";
        }

        if (player.ProgressFill != null)
        {
            player.ProgressFill.fillAmount = GetProgress(position, length);
        }

        if (player.PanelImage != null)
        {
            player.PanelImage.color = currentPlayer == player
                ? GetActivePanelColor(player.Number)
                : new Color32(255, 255, 255, 220);
        }

        if (player.TurnTextObject != null)
        {
            player.TurnTextObject.SetActive(currentPlayer == player);
        }
    }

    private static int GetWaypointLength(PlayerRuntime player)
    {
        return player?.Follower == null ? 0 : player.Follower.waypoints.Length;
    }

    private static float GetProgress(int position, int length)
    {
        if (length <= 1)
        {
            return 0f;
        }

        return Mathf.Clamp01((position - 1f) / (length - 1f));
    }

    private static PlayerRuntime GetCurrentPlayer()
    {
        if (currentPlayerIndex < 0 || currentPlayerIndex >= activePlayerCount)
        {
            return null;
        }

        var player = players[currentPlayerIndex];
        return player != null && !player.Finished && !player.Eliminated ? player : null;
    }

    private static PlayerRuntime GetPlayer(int playerNumber)
    {
        var index = playerNumber - 1;
        return index < 0 || index >= activePlayerCount ? null : players[index];
    }

    private static string FormatStats(GradStudentState state)
    {
        return state == null ? string.Empty : $"金 {state.Money} / IF {state.IfScore} / 心 {state.Mental} / 徳 {state.Virtue}";
    }

    private static int CalculateScore(GradStudentState state)
    {
        if (state == null)
        {
            return 0;
        }

        var careerBonus = state.IfScore >= 6 ? 60 : state.IfScore >= 3 ? 35 : 20;
        return state.Money + state.Mental + state.Virtue * 8 + state.IfScore * 15 + careerBonus;
    }

    private static string BuildCharacterSummary()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < activePlayerCount; i++)
        {
            if (i > 0)
            {
                builder.Append(" / ");
            }

            var controller = players[i].CpuControlled ? " CPU" : string.Empty;
            builder.Append($"P{i + 1}{controller}: {players[i].State.TypeName}");
        }

        return builder.ToString();
    }

    private static string BuildScoreSummary()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < activePlayerCount; i++)
        {
            if (i > 0)
            {
                builder.Append(" / ");
            }

            var player = players[i];
            builder.Append($"P{player.Number} {CalculateScore(player.State)}");
        }

        return builder.ToString();
    }

    private static string BuildResultSummary()
    {
        var winner = GetWinner();
        return winner == null ? "全員脱落" : $"修了判定: {BuildScoreSummary()}";
    }

    private static string BuildDetailedResultSummary()
    {
        var builder = new StringBuilder();
        var ranked = new bool[activePlayerCount];

        for (var rank = 1; rank <= activePlayerCount; rank++)
        {
            var bestIndex = -1;
            var bestScore = int.MinValue;
            for (var i = 0; i < activePlayerCount; i++)
            {
                if (ranked[i] || players[i] == null)
                {
                    continue;
                }

                var score = players[i].Eliminated ? int.MinValue + i : CalculateScore(players[i].State);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                break;
            }

            ranked[bestIndex] = true;
            var player = players[bestIndex];
            builder.AppendLine($"{rank}位  P{player.Number} {player.State.TypeName}  {GetCareerName(player)}");
            builder.AppendLine($"Score {CalculateScore(player.State)}   金 {player.State.Money} / IF {player.State.IfScore} / 心 {player.State.Mental} / 徳 {player.State.Virtue}");
            if (!string.IsNullOrEmpty(player.EliminationReason))
            {
                builder.AppendLine($"理由: {player.EliminationReason}");
            }

            if (rank < activePlayerCount)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string GetCareerName(PlayerRuntime player)
    {
        if (player.Eliminated)
        {
            return player.EliminationReason == "失踪" ? "行方不明" : "中退";
        }

        var state = player.State;
        if (state.IfScore >= 8 && state.Mental >= 45)
        {
            return "博士進学";
        }

        if (state.IfScore >= 6 && state.Money >= 30)
        {
            return "大手メーカー研究職";
        }

        if (state.IfScore >= 4)
        {
            return "ポスドク候補";
        }

        if (state.Money >= 80)
        {
            return "実家手伝い";
        }

        return "修士就職";
    }

    private static void WriteEvent(string message)
    {
        if (eventLogText != null)
        {
            eventLogText.text = message;
        }

        Debug.Log(message);
    }

    private static void SetVisible(PlayerRuntime player, bool visible)
    {
        if (player?.Object != null)
        {
            player.Object.SetActive(visible);
        }

        if (player?.TurnTextObject != null)
        {
            player.TurnTextObject.SetActive(false);
        }
    }

    private static void BindButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = GameObject.Find(objectName);
        var button = buttonObject == null ? null : buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static Text FindText(string objectName)
    {
        var textObject = GameObject.Find(objectName);
        return textObject == null ? null : textObject.GetComponent<Text>();
    }

    private static Image FindImage(string objectName)
    {
        var imageObject = GameObject.Find(objectName);
        return imageObject == null ? null : imageObject.GetComponent<Image>();
    }

    private static Color32 GetPlayerColor(int playerNumber)
    {
        switch (playerNumber)
        {
            case 1:
                return new Color32(37, 99, 235, 255);
            case 2:
                return new Color32(220, 38, 38, 255);
            case 3:
                return new Color32(22, 163, 74, 255);
            default:
                return new Color32(147, 51, 234, 255);
        }
    }

    private static Color32 GetActivePanelColor(int playerNumber)
    {
        switch (playerNumber)
        {
            case 1:
                return new Color32(231, 242, 255, 245);
            case 2:
                return new Color32(255, 235, 235, 245);
            case 3:
                return new Color32(232, 252, 238, 245);
            default:
                return new Color32(245, 235, 255, 245);
        }
    }

    [System.Serializable]
    private sealed class EventDatabase
    {
        public EventDefinition[] Events;
    }

    [System.Serializable]
    private sealed class EventDefinition
    {
        public string EventId;
        public string Title;
        public string Description;
        public string[] Tags;
        public EventChoice[] Choices;

        public static EventDefinition Conference()
        {
            return new EventDefinition
            {
                EventId = "E001",
                Title = "国際学会の案内",
                Description = "ハワイでの国際学会。旅費は自腹だが、採択されれば研究実績は大きい。",
                Tags = new[] { "学会" },
                Choices = new[]
                {
                    new EventChoice { Label = "自腹で参加", MoneyDelta = -50, MentalDelta = -20, IfDelta = 5 },
                    new EventChoice { Label = "今回は見送る" },
                    new EventChoice { Label = "課金で遠征を整える", MoneyDelta = -25, IfDelta = 4, RequiresKind = true, RequiredKind = GradStudentKind.Rich, RequiredMoney = 25 }
                }
            };
        }

        public static EventDefinition LateNightReview()
        {
            return new EventDefinition
            {
                EventId = "E002",
                Title = "深夜の論文添削",
                Description = "指導教員から深夜に大量のコメントが届いた。明日の朝までに直せるだろうか。",
                Tags = new[] { "教授" },
                Choices = new[]
                {
                    new EventChoice { Label = "朝まで対応する", MentalDelta = -30, IfDelta = 2, VirtueDelta = 5 },
                    new EventChoice { Label = "寝る", MentalDelta = 10, VirtueDelta = -10 }
                }
            };
        }

        public static EventDefinition PartTimeShift()
        {
            return new EventDefinition
            {
                EventId = "E003",
                Title = "シフトの穴埋め",
                Description = "バイト先から急な連勤依頼。財布は助かるが、研究室に戻る気力は削られる。",
                Tags = new[] { "バイト" },
                Choices = new[]
                {
                    new EventChoice { Label = "連勤で稼ぐ", MoneyDelta = 40, MentalDelta = -20, AddSkipTurn = true },
                    new EventChoice { Label = "断る" }
                }
            };
        }

        public static EventDefinition ExpenseTrouble()
        {
            return new EventDefinition
            {
                EventId = "E004",
                Title = "経費精算トラブル",
                Description = "提出書類の不備で経費が止まった。締切まで時間がない。",
                Tags = new[] { "事務" },
                Choices = new[]
                {
                    new EventChoice { Label = "自力で徹夜で直す", MentalDelta = -15 },
                    new EventChoice { Label = "秘書さんに泣きつく", VirtueDelta = -15, RequiredVirtue = 15 },
                    new EventChoice { Label = "課金で丸ごと解決", MoneyDelta = -20, RequiresKind = true, RequiredKind = GradStudentKind.Rich, RequiredMoney = 20 }
                }
            };
        }

        public static EventDefinition TeachingAssistant()
        {
            return new EventDefinition
            {
                EventId = "E005",
                Title = "TA",
                Description = "学部生の演習担当。きちんと見るほど信頼は増えるが、時間と気力は削られる。",
                Tags = new[] { "講義" },
                Choices = new[]
                {
                    new EventChoice { Label = "真面目に教える", MoneyDelta = 10, MentalDelta = -10, VirtueDelta = 3 },
                    new EventChoice { Label = "適当に自習させる", MoneyDelta = 10, VirtueDelta = -5 }
                }
            };
        }
    }

    [System.Serializable]
    private sealed class EventChoice
    {
        public string Label;
        public int RequiredMoney;
        public int RequiredVirtue;
        public int MoneyDelta;
        public int MentalDelta;
        public int IfDelta;
        public int VirtueDelta;
        public bool AddSkipTurn;
        public bool RequiresKind;
        public GradStudentKind RequiredKind;

        public bool CanChoose(GradStudentState state)
        {
            return state != null
                && state.Money >= RequiredMoney
                && state.Virtue >= RequiredVirtue
                && (!RequiresKind || state.Kind == RequiredKind);
        }

        public string BuildLabel()
        {
            var builder = new StringBuilder(Label);
            var effectText = BuildEffectText();
            if (!string.IsNullOrEmpty(effectText))
            {
                builder.Append("  ");
                builder.Append(effectText);
            }

            if (RequiredVirtue > 0)
            {
                builder.Append($"  条件: 徳{RequiredVirtue}+");
            }

            if (RequiredMoney > 0)
            {
                builder.Append($"  条件: 金{RequiredMoney}+");
            }

            if (RequiresKind)
            {
                builder.Append($"  条件: {GetKindName(RequiredKind)}");
            }

            return builder.ToString();
        }

        private string BuildEffectText()
        {
            var builder = new StringBuilder();
            AppendEffect(builder, "金", MoneyDelta);
            AppendEffect(builder, "心", MentalDelta);
            AppendEffect(builder, "IF", IfDelta);
            AppendEffect(builder, "徳", VirtueDelta);
            if (AddSkipTurn)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" / ");
                }

                builder.Append("休み");
            }

            return builder.ToString();
        }

        private static void AppendEffect(StringBuilder builder, string label, int value)
        {
            if (value == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(" / ");
            }

            builder.Append($"{label}{(value > 0 ? "+" : string.Empty)}{value}");
        }

        private static string GetKindName(GradStudentKind kind)
        {
            switch (kind)
            {
                case GradStudentKind.Hobby:
                    return "多趣味";
                case GradStudentKind.Serious:
                    return "真面目";
                case GradStudentKind.Athletic:
                    return "体育会";
                case GradStudentKind.Rich:
                    return "金持ち";
                default:
                    return "天才肌";
            }
        }
    }

    private enum GradStudentKind
    {
        Hobby,
        Serious,
        Athletic,
        Rich,
        Genius
    }

    private sealed class PlayerRuntime
    {
        public int Index => Number - 1;
        public int Number;
        public GameObject Object;
        public FollowThePath Follower;
        public GradStudentState State;
        public bool CpuControlled;
        public int StartWaypoint;
        public bool Finished;
        public bool Eliminated;
        public string EliminationReason;
        public bool ActiveSkillUsedThisTurn;
        public Text StatusText;
        public Text StatsText;
        public Text MoneyText;
        public Text IfText;
        public Text MentalText;
        public Text VirtueText;
        public Text PositionText;
        public Image PanelImage;
        public Image ProgressFill;
        public GameObject TurnTextObject;
    }

    private sealed class GradStudentState
    {
        public GradStudentKind Kind;
        public string TypeName;
        public int Money;
        public int Mental;
        public int MaxMental;
        public int IfScore;
        public int Virtue;
        public int SkipTurns;
        public int IgnoreNextEvents;

        public static GradStudentState CreateRandom()
        {
            var kind = (GradStudentKind)Random.Range(0, 5);
            switch (kind)
            {
                case GradStudentKind.Hobby:
                    return new GradStudentState { Kind = kind, TypeName = "多趣味", Money = 55, Mental = 90, MaxMental = 90 };
                case GradStudentKind.Serious:
                    return new GradStudentState { Kind = kind, TypeName = "真面目", Money = 70, Mental = 70, MaxMental = 70 };
                case GradStudentKind.Athletic:
                    return new GradStudentState { Kind = kind, TypeName = "体育会", Money = 70, Mental = 100, MaxMental = 100 };
                case GradStudentKind.Rich:
                    return new GradStudentState { Kind = kind, TypeName = "金持ち", Money = 120, Mental = 80, MaxMental = 80 };
                default:
                    return new GradStudentState { Kind = kind, TypeName = "天才肌", Money = 55, Mental = 45, MaxMental = 45 };
            }
        }
    }
}

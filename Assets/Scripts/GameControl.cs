using UnityEngine;
using UnityEngine.UI;

// ゲーム全体の状態とターン管理を行う。
public class GameControl : MonoBehaviour
{
    private static GameObject whoWinsText;
    private static GameObject winnerPanel;
    private static GameObject player1MoveText;
    private static GameObject player2MoveText;
    private static Text turnText;
    private static Text diceResultText;
    private static Text rollStateText;
    private static Text player1StatusText;
    private static Text player2StatusText;
    private static Text player1PositionText;
    private static Text player2PositionText;
    private static Image player1PanelImage;
    private static Image player2PanelImage;
    private static Image player1ProgressFill;
    private static Image player2ProgressFill;
    private static GameObject player1;
    private static GameObject player2;

    // サイコロの出目
    public static int diceSideThrown = 0;
    // 各プレイヤの開始地点。ターンごとに更新して前回止まったマスを開始点とする
    public static int player1StartWaypoint = 0;
    public static int player2StartWaypoint = 0;
    // ゲーム終了フラグ
    public static bool gameOver = false;

    private void Start()
    {
        diceSideThrown = 0;
        player1StartWaypoint = 0;
        player2StartWaypoint = 0;
        gameOver = false;

        // UIテキストとキャラクタをシーンから取得
        whoWinsText = GameObject.Find("WhoWinsText");
        winnerPanel = GameObject.Find("WinnerPanel");
        player1MoveText = GameObject.Find("Player1MoveText");
        player2MoveText = GameObject.Find("Player2MoveText");
        turnText = FindText("TurnText");
        diceResultText = FindText("DiceResultText");
        rollStateText = FindText("RollStateText");
        player1StatusText = FindText("Player1StatusText");
        player2StatusText = FindText("Player2StatusText");
        player1PositionText = FindText("Player1PositionText");
        player2PositionText = FindText("Player2PositionText");
        player1PanelImage = FindImage("Player1Panel");
        player2PanelImage = FindImage("Player2Panel");
        player1ProgressFill = FindImage("Player1ProgressFill");
        player2ProgressFill = FindImage("Player2ProgressFill");
        player1 = GameObject.Find("Player1");
        player2 = GameObject.Find("Player2");

        // 開始時はどちらも移動不可
        player1.GetComponent<FollowThePath>().moveAllowed = false;
        player2.GetComponent<FollowThePath>().moveAllowed = false;

        // UI初期状態：勝敗表示は非表示、プレイヤ1の番
        whoWinsText.SetActive(false);
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(false);
        }

        player1MoveText.SetActive(true);
        player2MoveText.SetActive(false);
        UpdateHud(1, "Ready");
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

    // ダイスを振った後に誰を移動させるか指定する
    public static void MovePlayer(int playerToMove)
    {
        ShowRoll(playerToMove, diceSideThrown);

        switch (playerToMove)
        {
            case 1:
                player1.GetComponent<FollowThePath>().moveAllowed = true;
                break;
            case 2:
                player2.GetComponent<FollowThePath>().moveAllowed = true;
                break;
        }
    }

    public static void ShowRoll(int playerToMove, int diceSide)
    {
        if (diceResultText != null)
        {
            diceResultText.text = $"Player {playerToMove} rolled {diceSide}";
        }

        if (rollStateText != null)
        {
            rollStateText.text = diceSide.ToString();
            rollStateText.color = playerToMove == 1 ? new Color32(66, 135, 245, 255) : new Color32(237, 84, 86, 255);
        }
    }

    private void Update()
    {
        // プレイヤ1の移動が終わったら移動を止め、番を交代
        if (player1.GetComponent<FollowThePath>().waypointIndex > player1StartWaypoint + diceSideThrown)
        {
            player1.GetComponent<FollowThePath>().moveAllowed = false;
            player1MoveText.SetActive(false);
            player2MoveText.SetActive(true);
            player1StartWaypoint = player1.GetComponent<FollowThePath>().waypointIndex - 1;
            UpdateHud(2, diceResultText == null ? string.Empty : diceResultText.text);
        }

        // プレイヤ2の移動が終わったら移動を止め、番を交代
        if (player2.GetComponent<FollowThePath>().waypointIndex > player2StartWaypoint + diceSideThrown)
        {
            player2.GetComponent<FollowThePath>().moveAllowed = false;
            player2MoveText.SetActive(false);
            player1MoveText.SetActive(true);
            player2StartWaypoint = player2.GetComponent<FollowThePath>().waypointIndex - 1;
            UpdateHud(1, diceResultText == null ? string.Empty : diceResultText.text);
        }

        // プレイヤ1がゴールに到達した場合
        if (player1.GetComponent<FollowThePath>().waypointIndex == player1.GetComponent<FollowThePath>().waypoints.Length)
        {
            whoWinsText.SetActive(true);
            player1MoveText.SetActive(false);
            player2MoveText.SetActive(false);
            if (winnerPanel != null)
            {
                winnerPanel.SetActive(true);
            }

            whoWinsText.GetComponent<Text>().text = "Player 1 wins";
            gameOver = true;
            UpdateWinnerHud(1);
        }

        // プレイヤ2がゴールに到達した場合
        if (player2.GetComponent<FollowThePath>().waypointIndex == player2.GetComponent<FollowThePath>().waypoints.Length)
        {
            whoWinsText.SetActive(true);
            player1MoveText.SetActive(false);
            player2MoveText.SetActive(false);
            if (winnerPanel != null)
            {
                winnerPanel.SetActive(true);
            }

            whoWinsText.GetComponent<Text>().text = "Player 2 wins";
            gameOver = true;
            UpdateWinnerHud(2);
        }
    }

    private static void UpdateHud(int activePlayer, string diceText)
    {
        if (turnText != null)
        {
            turnText.text = $"Player {activePlayer}'s turn";
            turnText.color = activePlayer == 1 ? new Color32(66, 135, 245, 255) : new Color32(237, 84, 86, 255);
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

        var player1Length = GetWaypointLength(player1);
        var player2Length = GetWaypointLength(player2);
        var player1Position = Mathf.Min(player1StartWaypoint + 1, player1Length);
        var player2Position = Mathf.Min(player2StartWaypoint + 1, player2Length);

        if (player1StatusText != null)
        {
            player1StatusText.text = "Player 1";
        }

        if (player2StatusText != null)
        {
            player2StatusText.text = "Player 2";
        }

        if (player1PositionText != null)
        {
            player1PositionText.text = $"{player1Position}/{player1Length}";
        }

        if (player2PositionText != null)
        {
            player2PositionText.text = $"{player2Position}/{player2Length}";
        }

        if (player1ProgressFill != null)
        {
            player1ProgressFill.fillAmount = GetProgress(player1Position, player1Length);
        }

        if (player2ProgressFill != null)
        {
            player2ProgressFill.fillAmount = GetProgress(player2Position, player2Length);
        }

        if (player1PanelImage != null)
        {
            player1PanelImage.color = activePlayer == 1
                ? new Color32(231, 242, 255, 245)
                : new Color32(255, 255, 255, 220);
        }

        if (player2PanelImage != null)
        {
            player2PanelImage.color = activePlayer == 2
                ? new Color32(255, 235, 235, 245)
                : new Color32(255, 255, 255, 220);
        }
    }

    private static void UpdateWinnerHud(int winner)
    {
        UpdateHud(winner, "Game set");

        if (turnText != null)
        {
            turnText.text = $"Player {winner} wins";
            turnText.color = winner == 1 ? new Color32(66, 135, 245, 255) : new Color32(237, 84, 86, 255);
        }

        if (diceResultText != null)
        {
            diceResultText.text = "Game set";
        }

        if (rollStateText != null)
        {
            rollStateText.text = "DONE";
            rollStateText.color = winner == 1 ? new Color32(66, 135, 245, 255) : new Color32(237, 84, 86, 255);
        }
    }

    private static int GetWaypointLength(GameObject player)
    {
        if (player == null)
        {
            return 0;
        }

        return player.GetComponent<FollowThePath>().waypoints.Length;
    }

    private static float GetProgress(int position, int length)
    {
        if (length <= 1)
        {
            return 0f;
        }

        return Mathf.Clamp01((position - 1f) / (length - 1f));
    }
}

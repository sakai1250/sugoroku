using UnityEngine;
using UnityEngine.UI;

// ゲーム全体の状態とターン管理を行う。
public class GameControl : MonoBehaviour
{
    private static GameObject whoWinsText;
    private static GameObject player1MoveText;
    private static GameObject player2MoveText;
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
        // UIテキストとキャラクタをシーンから取得
        whoWinsText = GameObject.Find("WhoWinsText");
        player1MoveText = GameObject.Find("Player1MoveText");
        player2MoveText = GameObject.Find("Player2MoveText");
        player1 = GameObject.Find("Player1");
        player2 = GameObject.Find("Player2");

        // 開始時はどちらも移動不可
        player1.GetComponent<FollowThePath>().moveAllowed = false;
        player2.GetComponent<FollowThePath>().moveAllowed = false;

        // UI初期状態：勝敗表示は非表示、プレイヤ1の番
        whoWinsText.SetActive(false);
        player1MoveText.SetActive(true);
        player2MoveText.SetActive(false);
    }

    // ダイスを振った後に誰を移動させるか指定する
    public static void MovePlayer(int playerToMove)
    {
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

    private void Update()
    {
        // プレイヤ1の移動が終わったら移動を止め、番を交代
        if (player1.GetComponent<FollowThePath>().waypointIndex > player1StartWaypoint + diceSideThrown)
        {
            player1.GetComponent<FollowThePath>().moveAllowed = false;
            player1MoveText.SetActive(false);
            player2MoveText.SetActive(true);
            player1StartWaypoint = player1.GetComponent<FollowThePath>().waypointIndex - 1;
        }

        // プレイヤ2の移動が終わったら移動を止め、番を交代
        if (player2.GetComponent<FollowThePath>().waypointIndex > player2StartWaypoint + diceSideThrown)
        {
            player2.GetComponent<FollowThePath>().moveAllowed = false;
            player2MoveText.SetActive(false);
            player1MoveText.SetActive(true);
            player2StartWaypoint = player2.GetComponent<FollowThePath>().waypointIndex - 1;
        }

        // プレイヤ1がゴールに到達した場合
        if (player1.GetComponent<FollowThePath>().waypointIndex == player1.GetComponent<FollowThePath>().waypoints.Length)
        {
            whoWinsText.SetActive(true);
            player1MoveText.SetActive(false);
            player2MoveText.SetActive(false);
            whoWinsText.GetComponent<Text>().text = "Player 1 wins";
            gameOver = true;
        }

        // プレイヤ2がゴールに到達した場合
        if (player2.GetComponent<FollowThePath>().waypointIndex == player2.GetComponent<FollowThePath>().waypoints.Length)
        {
            whoWinsText.SetActive(true);
            player1MoveText.SetActive(false);
            player2MoveText.SetActive(false);
            whoWinsText.GetComponent<Text>().text = "Player 2 wins";
            gameOver = true;
        }
    }
}

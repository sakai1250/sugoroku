using UnityEngine;

// ボード上の道筋に沿ってキャラクタを動かす。
public class FollowThePath : MonoBehaviour
{
    // キャラクタが通過するWaypointのリスト
    public Transform[] waypoints;

    [SerializeField]
    private float moveSpeed = 1f;  // キャラクタの移動速度

    [HideInInspector]
    public int waypointIndex = 0;  // 現在のWaypointのインデックス

    public bool moveAllowed = false;  // trueの間キャラクタが移動する

    private void Start()
    {
        // 最初のWaypointにキャラクタを配置
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[waypointIndex].transform.position;
        }
    }

    private void Update()
    {
        // フラグが立っている間のみ移動
        if (moveAllowed)
        {
            Move();
        }
    }

    // 次のWaypointへ移動
    private void Move()
    {
        if (waypointIndex < waypoints.Length)
        {
            // 次のWaypointへ向かって補間移動
            transform.position = Vector2.MoveTowards(
                transform.position,
                waypoints[waypointIndex].transform.position,
                moveSpeed * Time.deltaTime);

            // 次のWaypointに到達したらインデックスを進める
            if (transform.position == waypoints[waypointIndex].transform.position)
            {
                waypointIndex += 1;
            }
        }
    }
}

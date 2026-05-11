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
    private Vector3 baseScale;
    private float hopTime;

    private void Start()
    {
        baseScale = transform.localScale;
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
            AnimateMovingPiece();
            return;
        }

        AnimateIdlePiece();
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
                hopTime = 0.16f;
                waypointIndex += 1;
            }
        }
    }

    private void AnimateMovingPiece()
    {
        var wave = Mathf.Sin(Time.time * 18f) * 0.045f;
        transform.localScale = baseScale * (1f + wave);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 14f) * 4f);
    }

    private void AnimateIdlePiece()
    {
        if (hopTime <= 0f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 12f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 12f);
            return;
        }

        hopTime -= Time.deltaTime;
        var t = Mathf.Clamp01(hopTime / 0.16f);
        var bounce = Mathf.Sin(t * Mathf.PI);
        transform.localScale = baseScale * (1f + bounce * 0.18f);
        transform.rotation = Quaternion.Euler(0f, 0f, bounce * 7f);
    }
}

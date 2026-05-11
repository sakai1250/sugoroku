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
    private float segmentProgress;
    private Vector3 segmentStart;

    private void Start()
    {
        baseScale = transform.localScale;
        // 最初のWaypointにキャラクタを配置
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[waypointIndex].transform.position;
        }
        segmentStart = transform.position;
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
            var target = waypoints[waypointIndex].transform.position;
            var totalDist = Vector3.Distance(segmentStart, target);

            // 次のWaypointへ向かって補間移動
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime);

            // Waypoint間の進捗を0→1で計算
            if (totalDist > 0.000001f)
            {
                segmentProgress = 1f - Vector3.Distance(transform.position, target) / totalDist;
            }

            // 次のWaypointに到達したらインデックスを進める
            if (Vector3.Distance(transform.position, target) < 0.001f)
            {
                hopTime = 0.16f;
                segmentStart = target;
                segmentProgress = 0f;
                waypointIndex += 1;
            }
        }
    }

    private void AnimateMovingPiece()
    {
        // Waypoint間を1往復するホップ（到達時に頂点）
        var hop = Mathf.Sin(segmentProgress * Mathf.PI);
        var tilt = Mathf.Sin(segmentProgress * Mathf.PI * 2f) * 18f;
        transform.localScale = baseScale * (1f + hop * 0.35f);
        transform.rotation = Quaternion.Euler(0f, 0f, tilt);
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

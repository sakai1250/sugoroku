using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// サイコロをクリックしてダイスロールを行う。
public class Dice : MonoBehaviour
{
    private Sprite[] diceSides;      // サイコロの各面のスプライト
    private SpriteRenderer rend;     // 表示用のSpriteRenderer
    private Vector3 baseScale;

    private bool coroutineAllowed = true; // 他のサイコロ処理中はクリック不能
    private bool cpuRollQueued;

    private void Start()
    {
        baseScale = transform.localScale;
        rend = GetComponent<SpriteRenderer>();
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }

        // Kenneyの無料CC0素材を優先し、無い場合は元のサイコロ画像を使う。
        diceSides = Resources.LoadAll<Sprite>("KenneyDice/");
        if (diceSides.Length == 0)
        {
            diceSides = Resources.LoadAll<Sprite>("DiceSides/");
        }
        // 初期表示として6の目を設定
        if (diceSides.Length > 5)
        {
            rend.sprite = diceSides[5];
        }
    }

    private void Update()
    {
        if (coroutineAllowed && !cpuRollQueued && GameControl.CanAutoRoll())
        {
            StartCoroutine(RollForCpu());
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning("Main Camera is missing.");
            return;
        }

        var worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        var hit = Physics2D.OverlapPoint(worldPoint);

        // ゲームオーバーでなく、サイコロ自身がクリックされた場合のみ処理
        if (hit != null && hit.gameObject == gameObject && coroutineAllowed && GameControl.CanRoll())
        {
            Debug.Log("Dice clicked");
            StartCoroutine(RollTheDice());
        }
    }

    private IEnumerator RollForCpu()
    {
        cpuRollQueued = true;
        yield return new WaitForSeconds(0.65f);

        if (coroutineAllowed && GameControl.CanAutoRoll())
        {
            yield return RollTheDice();
        }

        cpuRollQueued = false;
    }

    private IEnumerator RollTheDice()
    {
        coroutineAllowed = false;
        var currentPlayer = GameControl.GetCurrentPlayerNumber();
        if (GameControl.ConsumeSkipTurn(currentPlayer))
        {
            coroutineAllowed = true;
            yield break;
        }

        int randomDiceSide = 0;

        // サイコロの面を高速で切り替えてアニメーションを演出
        for (int i = 0; i <= 20; i++)
        {
            randomDiceSide = Random.Range(0, 6);
            rend.sprite = diceSides[randomDiceSide];
            var t = i / 20f;
            var pulse = Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseScale * (1f + pulse * 0.22f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-18f, 378f, t));
            yield return new WaitForSeconds(0.05f);
        }

        for (var i = 0; i < 8; i++)
        {
            var t = (i + 1f) / 8f;
            transform.localScale = Vector3.Lerp(baseScale * 1.12f, baseScale, t);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, t);
            yield return new WaitForSeconds(0.02f);
        }

        // 出目（0～5）は1～6に変換してGameControlへ渡す
        GameControl.diceSideThrown = randomDiceSide + 1;
        Debug.Log("Dice: " + GameControl.diceSideThrown);

        // 出目に基づいて現在のプレイヤを移動
        GameControl.MovePlayer(currentPlayer);

        coroutineAllowed = true;
    }
}

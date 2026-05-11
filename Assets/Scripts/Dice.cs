using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// サイコロをクリックしてダイスロールを行う。
public class Dice : MonoBehaviour
{
    private Sprite[] diceSides;      // サイコロの各面のスプライト
    private SpriteRenderer rend;     // 表示用のSpriteRenderer

    private int whosTurn = 1;         // 1はプレイヤー1のターン、-1はプレイヤー2のターン
    private bool coroutineAllowed = true; // 他のサイコロ処理中はクリック不能

    private void Start()
    {
        rend = GetComponent<SpriteRenderer>();
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }

        // Resources/DiceSides 配下に配置したサイコロ面の画像を読み込む。
        diceSides = Resources.LoadAll<Sprite>("DiceSides/");
        // 初期表示として6の目を設定
        if (diceSides.Length > 5)
        {
            rend.sprite = diceSides[5];
        }
    }

    private void Update()
    {
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
        if (hit != null && hit.gameObject == gameObject && !GameControl.gameOver && coroutineAllowed)
        {
            Debug.Log("Dice clicked");
            StartCoroutine(RollTheDice());
        }
    }

    private IEnumerator RollTheDice()
    {
        coroutineAllowed = false;
        int randomDiceSide = 0;

        // サイコロの面を高速で切り替えてアニメーションを演出
        for (int i = 0; i <= 20; i++)
        {
            randomDiceSide = Random.Range(0, 6);
            rend.sprite = diceSides[randomDiceSide];
            yield return new WaitForSeconds(0.05f);
        }

        // 出目（0～5）は1～6に変換してGameControlへ渡す
        GameControl.diceSideThrown = randomDiceSide + 1;
        Debug.Log("Dice: " + GameControl.diceSideThrown);

        // 出目に基づいて移動するプレイヤを決定
        if (whosTurn == 1)
        {
            GameControl.MovePlayer(1);
        }
        else if (whosTurn == -1)
        {
            GameControl.MovePlayer(2);
        }

        // 次の番を反転
        whosTurn *= -1;
        coroutineAllowed = true;
    }
}

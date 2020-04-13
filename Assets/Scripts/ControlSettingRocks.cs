using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// 플레이어가 선택한 쪽의 돌 놓는 것을 담당한다.
/// </summary>
public class ControlSettingRocks : MonoBehaviour
{
    private GameObject playerrocks; // 플레이어 돌 스프라이트
    private GameObject candidaterocks; // 두기 전 확인 스프라이트.
    private int inix, iniy;

    private Vector2 beforecoord; // 이전 후보군 벡터2

    private new Camera camera;

    // 한번 터치하면 후보군, 같은데 두번 터치하면 놓기.
    // 플레이어가 뭔돌인지는 gamemanager에 있음

    // Start is called before the first frame update
    void Start()
    {
        if (GameManager.instance.playercolor == GameManager.whatColor.black)
            playerrocks = GameManager.instance.Rocks[1];
        else
            playerrocks = GameManager.instance.Rocks[0];

        candidaterocks = GameManager.instance.Rocks[2];

        inix = GameManager.instance.inix;
        iniy = GameManager.instance.iniy;

        camera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.curplayerturn && GameManager.instance.timer > 0 && Input.GetMouseButtonDown(0))
        {

            Vector2 mouseposition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            mouseposition = camera.ScreenToWorldPoint(mouseposition);
            Vector2 curpos = new Vector2(Mathf.Round(mouseposition.x), Mathf.Round(mouseposition.y));

            // 좌표 정규화 (0,0)
            Vector2 normalcurpos = new Vector2(curpos.x - inix, curpos.y - iniy);


            // 먼저 정해진 좌표 안에 있는지 확인.
            if (normalcurpos.x < 0 || normalcurpos.x > GameManager.instance.mapsize || normalcurpos.y < 0 || normalcurpos.y > GameManager.instance.mapsize)
                return;
            // 해당 위치에 돌이 없어야함.
            if (GameManager.instance.curmap[(int)normalcurpos.x, (int)normalcurpos.y] != 0)
                return;


            // 후보군이 없었으면 그자리에 후보군 놓기.
            if (beforecoord == null)
            {
                Instantiate(candidaterocks, curpos, Quaternion.identity);
                beforecoord = curpos;
            }
            else
            {
                // 만약 이 전에 다른 후보군이 있었다면 지워버리고 현재 위치에 새로운 후보군 놓기

                Destroy(GameObject.FindGameObjectWithTag("Candidate"));

                if (beforecoord != curpos)
                {
                    Instantiate(candidaterocks, curpos, Quaternion.identity);
                    beforecoord = curpos;
                }
                // 같은 후보군이었으면 바로 돌 놓기
                else
                {
                    GameManager.instance.curmap[(int)normalcurpos.x, (int)normalcurpos.y] = 1;

                    Instantiate(playerrocks, curpos, Quaternion.identity);
                    // 턴 종료
                    GameManager.instance.IsActionEnded = true;
                }
            }
        }

        // 시간 다되도 actionended
        if (!GameManager.instance.IsActionEnded && GameManager.instance.curplayerturn && GameManager.instance.timer < 0)
            GameManager.instance.IsActionEnded = true;
    }
}

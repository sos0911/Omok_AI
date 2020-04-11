using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 싱글턴

    // 좌측아래 좌표 (-16,-9)..

    public int mapsize = 19; // 19 * 19
    // 기준좌표
    public int inix = -16;
    public int iniy = -9;

    // 흰돌, 검은돌, 후보돌
    public GameObject[] Rocks;
    
    public enum whatColor { white, black };

    // 누가 이겼는지를 나타낼 때 사용.
    public enum WhoWin { Player, AI, Unknown, Draw};
    public whatColor playercolor; // 플레이어 색깔

    public bool beplayerturn; // 바로 전 턴 플레이어 턴?
    public bool curplayerturn; // 이번 턴 플레이어 턴?

    private bool IsStart = true; // 게임이 막 시작했나? 
    public bool IsgameOver = false; // 게임이 끝났나?

    public bool IsActionEnded = false; // 돌 놓기가 끝났나?
    public bool IsAIActionEnded = false; // AI 돌 놓기가 끝났나?

    public float timer = 0; // 남은 시간.
    public float turntime = 60f; // 한 턴 시간.

    private ControlUI controlUI; // UI control 위임할 스크립트.

    public int[,] curmap; // 각 좌표에 바둑돌이 놓였나?
    // 0이면 not set, 1이면 player, 2면 AI

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        curmap = new int[mapsize + 1, mapsize + 1]; // 각 좌표에 바둑돌이 놓였나?

        // 초기화
        for (int i = 0; i <= mapsize; i++)
            for (int j = 0; j <= mapsize; j++)
                curmap[i, j] = 0;

        playercolor = PlayerPrefs.GetString("PlayerColor") == "Black" ? whatColor.black : whatColor.white;
    }


    // Update is called once per frame
    void Update()
    {
        // 시작 화면 말고 안으로 들어온 후 게임 시작됨.
        if (SceneManager.GetActiveScene().name == "Main" && !IsgameOver)
        {
            if (timer <= 0 || IsActionEnded || IsAIActionEnded)
            {
                WhoWin winside = DecideWhoWin();
              if(winside != WhoWin.Unknown)
                {
                    IsgameOver = true;
                    // 시간정지?
                    // scene reload로 restart 시 다시 되돌려줘야함
                    if (winside == WhoWin.AI)
                        controlUI.UIchangeGameoverText("AI");
                    else if (winside == WhoWin.Player)
                        controlUI.UIchangeGameoverText("Player");
                    else
                        controlUI.UIchangeGameoverText("Draw");

                    // 게임매니저 비활성화.
                    GameManager.instance.gameObject.SetActive(false);
                }

                ChangeTurn();
                if (!curplayerturn)
                    ControlAISettingRocks.instance.Search();
            }
            else
            {
                timer -= Time.deltaTime;
                controlUI.UIchangelefttime(timer);
            }
        }
    }

    /// <summary>
    /// 턴을 바꾸면서 정보 갱신하는 함수
    /// </summary>
    void ChangeTurn()
    {
        // turn bool flag 초기화
        if (curplayerturn)
            IsActionEnded = false;
        else
            IsAIActionEnded = false;

        timer = turntime;
        beplayerturn = curplayerturn;
        DecideWhosturn();
        if (curplayerturn)
            controlUI.UIchangePlayerturn("플레이어 턴");
        else
            controlUI.UIchangePlayerturn("AI 턴");
    }

    /// <summary>
    /// 이번 턴이 누구 턴인지 결정
    /// </summary>
    void DecideWhosturn()
    {
        if (IsStart)
        {
            if (playercolor == whatColor.black)
                beplayerturn = false;
            else
                beplayerturn = true;
            IsStart = false;
        }
        curplayerturn = !beplayerturn;
    }

    /// <summary>
    /// main 씬에서 UI 담당할 스크립트 가져오기
    /// </summary>
    public void GetcacheofUI()
    {
        controlUI = GameObject.FindWithTag("UIcontrol").GetComponent<ControlUI>();
    }

    /// <summary>
    /// 한 수를 둘 때마다 누가 이겼는지를 판정. 아무도 조건충족 안되면 continue
    /// </summary>
    public WhoWin DecideWhoWin()
    {
        bool Isfull = true; // 맵이 다 찼는가?

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                int rcolor = curmap[i, j];

                if (rcolor == 0)
                {
                    Isfull = false;
                    continue;
                }

                // 가로 세로 대각선 역대각선 다해봄

                int cnt = 1;
                while (j + cnt < mapsize + 1 && curmap[i, j + cnt] == rcolor)
                {
                    cnt++;
                    if (cnt == 5)
                        return rcolor == 1 ? WhoWin.Player : WhoWin.AI;
                }

                cnt = 1;
                while (i + cnt < mapsize + 1 && curmap[i + cnt, j] == rcolor)
                {
                    cnt++;
                    if (cnt == 5)
                        return rcolor == 1 ? WhoWin.Player : WhoWin.AI;
                }

                cnt = 1;
                while (i + cnt < mapsize + 1 && j + cnt < mapsize + 1 && curmap[i + cnt, j + cnt] == rcolor)
                {
                    cnt++;
                    if (cnt == 5)
                        return rcolor == 1 ? WhoWin.Player : WhoWin.AI;
                }

                cnt = 1;
                while (i + cnt < mapsize + 1 && j - cnt >= 0 && curmap[i + cnt, j - cnt] == rcolor)
                {
                    cnt++;
                    if (cnt == 5)
                        return rcolor == 1 ? WhoWin.Player : WhoWin.AI;
                }
            }

        // 만약 누구도 승리조건을 만족하지 않고 맵이 다 차버렸다면 draw.
        if (Isfull)
            return WhoWin.Draw;

        // 그게 다 아니라면 아직 승패는 모른다.
        return WhoWin.Unknown;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // 싱글턴

    public const int INF = (int)1e9;

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

    public int GameDepthLimit = INF; // 게임에서의 AI search 제한깊이

    public bool curplayerturn; // 이번 턴 플레이어 턴?

    public bool Isturnchanging = false; // 턴체인지 및 우승조건 작업중인가?

    public bool IsgameOver = false; // 게임이 끝났나?

    public bool IsActionEnded; // 돌 놓기가 끝났나?
    public bool IsAIActionEnded; // AI 돌 놓기가 끝났나?

    public bool Issamsam = false; // player가 3*3금수를 어겼나?
    public bool Istimeroverdefeat = false; // player가 시간을 넘겨서 패했나?

    public float timer = 0f; // 남은 시간.
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
        // 처음 턴 결정. changeturn으로 이 정보가 반대로 바뀐다.
        curplayerturn = playercolor == whatColor.black ? false : true;
        IsActionEnded = true;
        IsAIActionEnded = true;



        turntime = PlayerPrefs.GetInt("Turnlimit");

        if (PlayerPrefs.GetInt("Depthlimit") >= 1)
            GameDepthLimit = PlayerPrefs.GetInt("Depthlimit");

    }



    // Update is called once per frame
    // 아 이거 update 구성이 문제다..

    async void Update()
    {
        // 시작 화면 말고 안으로 들어온 후 게임 시작됨.
        // AI의 턴일때 action이 끝나야 함(timer와는 상관없이)
        // player의 턴일때는 timer < 0 or turnended
        if (!IsgameOver)
        {
            // 만약 플레이어가 입력을 시간 내에 안하면 패배
            if(timer < 0f && curplayerturn && !IsActionEnded)
            {
                Istimeroverdefeat = true;
                controlUI.UIchangeGameoverText("AI");
                GameManager.instance.gameObject.SetActive(false);
                return;
            }

            if (!Isturnchanging && (IsActionEnded || IsAIActionEnded))
            {
                Isturnchanging = true;

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
                    return;
                }

                ChangeTurn();

                Isturnchanging = false;

                if (!curplayerturn)
                {
                    
                    var task = Task.Run(() => ControlAISettingRocks.instance.Search());

                    await task;
                    
                    // Debug.Log("totalcoord : " + ControlAISettingRocks.instance.totalcoord.Key + " " + ControlAISettingRocks.instance.totalcoord.Value);

                    // setrock이 끝나면 isAIactionended = true
                    ControlAISettingRocks.instance.SetRocks();

                }
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
        IsActionEnded = false;
        IsAIActionEnded = false;

        Issamsam = false;
        timer = turntime;

        curplayerturn = !curplayerturn;

        //Debug.Log(" curplayerturn : " + curplayerturn);

        if (curplayerturn)
        {
            controlUI.UIchangePlayerturn("플레이어 턴");
            controlUI.ToggleThingkingAIText();
        }
        else
        {
            controlUI.UIchangePlayerturn("AI 턴");
            controlUI.ToggleThingkingAIText();
        }
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
        // 먼저, 누군가 33금수를 어겼는지 판단한다.
        // 근데, AI는 우선순위로 금수를 어기진 않을 것이다. 따라서 스킵한다.

        int[,] Psamsam = new int[mapsize + 1, mapsize + 1];

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (curmap[i, j] == 1)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && j + k < mapsize + 1)
                    {
                        if (curmap[i, j + k] == 0)
                            zerocnt++;
                        else if (curmap[i, j + k] == 2)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.x = j + k;
                        }

                        k++;
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((scoord.x - 1 >= 0 && curmap[scoord.y, scoord.x - 1] == 0) && (lcoord.x + 1 < mapsize + 1 && curmap[lcoord.y, lcoord.x + 1] == 0)))
                    {

                        if(Mathf.Abs(lcoord.x - scoord.x) != 2 || ((scoord.x - 2 >= 0 && curmap[scoord.y, scoord.x - 2] == 0) || (lcoord.x + 2 < mapsize + 1 && curmap[lcoord.y, lcoord.x + 2] == 0)))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            Psamsam[scoord.y, scoord.x]++;

                            do
                            {
                                scoord.x++;

                                if (curmap[scoord.y, scoord.x] == 1)
                                    Psamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 세로

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (curmap[i, j] == 1)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && i + k < mapsize + 1)
                    {
                        if (curmap[i + k, j] == 0)
                            zerocnt++;
                        else if (curmap[i + k, j] == 2)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                        }

                        k++;
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((scoord.y - 1 >= 0 && curmap[scoord.y - 1, scoord.x] == 0) && (lcoord.y + 1 < mapsize + 1 && curmap[lcoord.y + 1, lcoord.x] == 0)))
                    {

                        if(Mathf.Abs(lcoord.y - scoord.y) != 2 || ((scoord.y - 2 >= 0 && curmap[scoord.y-2, scoord.x] == 0) || (lcoord.y + 2 < mapsize + 1 && curmap[lcoord.y+2, lcoord.x] == 0)))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            Psamsam[scoord.y, scoord.x]++;

                            do
                            {
                                scoord.y++;

                                if (curmap[scoord.y, scoord.x] == 1)
                                    Psamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 대각선

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (curmap[i, j] == 1)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && ControlAISettingRocks.instance.twoIsinmap(new Pair(i + k, j + k)))
                    {
                        if (curmap[i + k, j + k] == 0)
                            zerocnt++;
                        else if (curmap[i + k, j + k] == 2)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                            lcoord.x = j + k;
                        }

                        k++;
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((ControlAISettingRocks.instance.twoIsinmap(new Pair(scoord.y - 1, scoord.x - 1)) && curmap[scoord.y - 1, scoord.x - 1] == 0) && (ControlAISettingRocks.instance.twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && curmap[lcoord.y + 1, lcoord.x + 1] == 0)))
                    {

                        if(Mathf.Abs(lcoord.y - scoord.y) != 2 || ((ControlAISettingRocks.instance.twoIsinmap(new Pair(scoord.y - 2, scoord.x - 2)) && (curmap[scoord.y-2, scoord.x-2] == 0)) ||
                            (ControlAISettingRocks.instance.twoIsinmap(new Pair(lcoord.y + 2 , lcoord.x + 2)) && (curmap[lcoord.y+2, lcoord.x+2] == 0))))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            Psamsam[scoord.y, scoord.x]++;

                            do
                            {
                                scoord.y++;
                                scoord.x++;

                                if (curmap[scoord.y, scoord.x] == 1)
                                    Psamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 역대각선

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (curmap[i, j] == 1)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && ControlAISettingRocks.instance.twoIsinmap(new Pair(i + k, j - k)))
                    {
                        if (curmap[i + k, j - k] == 0)
                            zerocnt++;
                        else if (curmap[i + k, j - k] == 2)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                            lcoord.x = j - k;
                        }

                        k++;
                    }


                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((ControlAISettingRocks.instance.twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && curmap[scoord.y - 1, scoord.x + 1] == 0) && (ControlAISettingRocks.instance.twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && curmap[lcoord.y + 1, lcoord.x - 1] == 0)))
                    {
                        if (Mathf.Abs(lcoord.y - scoord.y) != 2 || ((ControlAISettingRocks.instance.twoIsinmap(new Pair(scoord.y - 2, scoord.x + 2)) && (curmap[scoord.y - 2, scoord.x + 2] == 0)) ||
                           (ControlAISettingRocks.instance.twoIsinmap(new Pair(lcoord.y + 2, lcoord.x - 2)) && (curmap[lcoord.y + 2, lcoord.x - 2] == 0))))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            Psamsam[scoord.y, scoord.x]++;

                            do
                            {
                                scoord.y++;
                                scoord.x--;

                                if (curmap[scoord.y, scoord.x] == 1)
                                    Psamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // Psamsam을 보고 2 이상이면 금수처리. => ai 판정승

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (Psamsam[i, j] >= 2)
                {
                   // Debug.Log("samsam 발생!!");
                    Issamsam = true;
                    return WhoWin.AI;
                }
                /*
                else if(Psamsam[i, j] == 1)
                {
                    Debug.Log("sam 1개 발생!! : (" + i + "," + j + ")");
                }
                */


        // ========================================================================================
        // ========================================================================================
        // ========================================================================================


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

                // 해당 칸에 돌이 있다.
                // 가로 세로 대각선 역대각선 다해봄
                // 단, 오목의 경우 시작점이 구석탱이거나 시작점 전에 비어있던지 상대방 돌이 있어야 한다.
                // 아니면 장수가 되어 버리므로.
                // 즉, 시작점 전에 자신과 같은 돌이 있으면 안된다.

                int cnt = 1;
                while (j + cnt < mapsize + 1 && curmap[i, j + cnt] == rcolor)
                    cnt++;

                if (cnt == 5)
                {
                    if (!ControlAISettingRocks.instance.oneIsinmap(j - 1) || (ControlAISettingRocks.instance.oneIsinmap(j - 1) && curmap[i, j - 1] != rcolor))
                        return rcolor == 1 ? WhoWin.Player : WhoWin.AI;
                }

                cnt = 1;
                while (i + cnt < mapsize + 1 && curmap[i + cnt, j] == rcolor)
                    cnt++;

                if (cnt == 5)
                {
                    if (!ControlAISettingRocks.instance.oneIsinmap(i - 1) || (ControlAISettingRocks.instance.oneIsinmap(i - 1) && curmap[i-1, j] != rcolor))
                        return rcolor == 1 ? WhoWin.Player : WhoWin.AI;
                }

                cnt = 1;
                while (i + cnt < mapsize + 1 && j + cnt < mapsize + 1 && curmap[i + cnt, j + cnt] == rcolor)
                    cnt++;

                if (cnt == 5)
                {
                    if (!ControlAISettingRocks.instance.twoIsinmap(new Pair(i - 1, j-1)) || (ControlAISettingRocks.instance.twoIsinmap(new Pair(i - 1, j - 1)) && curmap[i-1, j-1] != rcolor))
                        return rcolor == 1 ? WhoWin.Player : WhoWin.AI;
                }

                cnt = 1;
                while (i + cnt < mapsize + 1 && j - cnt >= 0 && curmap[i + cnt, j - cnt] == rcolor)
                    cnt++;

                if (cnt == 5)
                {
                    if (!ControlAISettingRocks.instance.twoIsinmap(new Pair(i - 1, j + 1)) || (ControlAISettingRocks.instance.twoIsinmap(new Pair(i - 1, j + 1)) && curmap[i - 1, j + 1] != rcolor))
                        return rcolor == 1 ? WhoWin.Player : WhoWin.AI;
                }
            }

        // 만약 누구도 승리/패배(33)조건을 만족하지 않고 맵이 다 차버렸다면 draw.
        if (Isfull)
            return WhoWin.Draw;

        // 그게 다 아니라면 아직 승패는 모른다.
        return WhoWin.Unknown;
    }
}

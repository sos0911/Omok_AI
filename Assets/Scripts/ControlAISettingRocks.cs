using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// AI가 돌을 놓는 것을 컨트롤하는 스크립트.
/// </summary>
public class ControlAISettingRocks : MonoBehaviour
{
    // singleton
    public static ControlAISettingRocks instance;
    public KeyValuePair<int, int> totalcoord; // 최종적으로 돌을 놓을 좌표
    public const int INF = (int)1e9;
    public const int sINF = (int)1e8;

    // search 동안 그때마다의 맵으로 쓸 2차원 배열
    int[,] searchmap;
    // [연속수, 닫/열] score (공,방)
    // 방어하지 못할때 차감되는 걸 조금 높게 한다.
    int[,] Ascorearr, Dscorearr; 

    private GameObject AIrocks; // AI 돌 color sprite

    private int mapsize;

    public enum MaxMIn { max, min }; // node가 maxnode냐 minnode냐?

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        AIrocks = GameManager.instance.playercolor == GameManager.whatColor.black ? GameManager.instance.Rocks[0] : GameManager.instance.Rocks[1];
        mapsize = GameManager.instance.mapsize;

        searchmap = new int[mapsize + 1, mapsize + 1];
        // score배열 초기화
        // 6목 이상은 3000점이다.
        
        // 한쪽만 막힌거하고 두쪽 다막힌거하고 구분해야 한다(;;)
        // 두쪽 다막힌건 score=0으로 처리, 배열에서는 한쪽열림과 완전열림 구분.

        Ascorearr = new int[6, 2] { { 0, 0 }, {10,20},{ 1000, 2000 }, { 100000, 200000 }, { 10000000, 20000000 } , { sINF, sINF} };
        Dscorearr = new int[6, 2] { { 0, 0 }, { -30, -40 }, { -2000, -6000 }, { -200000, -600000 }, { -20000000, -60000000 }, { -2*sINF, -2*sINF } };
    }

    public void Search()
    {
        // 배열 초기화
        for (int i = 0; i < GameManager.instance.mapsize + 1; i++)
            for (int j = 0; j < GameManager.instance.mapsize + 1; j++)
                searchmap[i, j] = GameManager.instance.curmap[i, j];

        // iterative deepening

        int testdepthlimit = 1;


        for(int depth=1;depth <= testdepthlimit && GameManager.instance.timer > 0f; depth++)
        {
            int totalscore;
            Node initialnode = new Node(MaxMIn.max);
            initialnode.coord = new KeyValuePair<int, int>(-1, -1); // initial node 표시
            totalscore = alphabetaAlg(initialnode, depth, 2, -INF, INF);

            //Debug.Log("totalscore : " + totalscore);

            // initialnode에 저장된 bestvalue 갖는 child를 찾아 그 안 coord를 빼옴.
            foreach (Node child in initialnode.childlist)
            {

              //  Debug.Log("child : (" + child.coord.Key + "," + child.coord.Value + ") :: " + child.bestvalue);

                if (child.bestvalue == totalscore)
                {
                    totalcoord = new KeyValuePair<int, int>(child.coord.Key, child.coord.Value);
                    break;
                }
            }
        }

       // Debug.Log("totalcoord : " + totalcoord.Key + " " + totalcoord.Value);

        // 돌 놓기
        Vector2 curpos = new Vector2(totalcoord.Key + GameManager.instance.inix, totalcoord.Value + GameManager.instance.iniy);
        Instantiate(AIrocks, curpos, Quaternion.identity);
        GameManager.instance.curmap[totalcoord.Key, totalcoord.Value] = 2;
        GameManager.instance.IsAIActionEnded = true;
    }

    // alpha-beta pruning alg with iterative deepening


    int alphabetaAlg(Node node, int depth, int player, int alpha, int beta)
    {
        //Debug.Log("alphabetaalg : (" + node.coord.Key + "," + node.coord.Value + ") " + depth + " " + player);
        // alpha-beta pruning을 진행하는 함수
        // 자기가 찾은 bestscore을 return한다.
        // 앞으로 depth 수만큼 예상한다.

        // 만약 타이머가 다 되었거나 깊이가 다 되면 바로 status의 score을 매긴다.
        if(depth==0 || GameManager.instance.timer <= 0f)
        {
            // 현재 searchmap으로 결정.
            node.bestvalue = Newheuristic();
            return node.bestvalue;
        }

        if (player == 2)
        {
            // max node
            for (int i = 0; i < mapsize + 1; i++)
                for (int j = 0; j < mapsize + 1; j++)
                {
                    if (searchmap[i, j] == 0) 
                    {
                        // [i,j]에 놓기 가능
                        Node child = new Node(MaxMIn.min);
                        child.coord = new KeyValuePair<int, int>(i, j);
                        node.childlist.Add(child);
                        searchmap[i, j] = 2;

                        node.bestvalue = Math.Max(node.bestvalue, alphabetaAlg(child, depth - 1, 1, alpha, beta));
                        alpha = Math.Max(alpha, node.bestvalue);

                        searchmap[i, j] = 0;

                        if (alpha >= beta)
                            break;
                        if(GameManager.instance.timer <= 0f)
                            return node.bestvalue;
                    }
                }
        }
        else
        {
            // min node
            for (int i = 0; i < mapsize + 1; i++)
                for (int j = 0; j < mapsize + 1; j++)
                {
                    if (searchmap[i, j] == 0) 
                    {
                        // [i,j]에 놓기 가능
                        Node child = new Node(MaxMIn.max);
                        child.coord = new KeyValuePair<int, int>(i, j);
                        node.childlist.Add(child);
                        searchmap[i, j] = 1;

                        node.bestvalue = Math.Min(node.bestvalue, alphabetaAlg(child, depth - 1, 1, alpha, beta));
                        beta = Math.Min(beta, node.bestvalue);

                        searchmap[i, j] = 0;

                        if (alpha >= beta)
                            break;
                        if (GameManager.instance.timer <= 0f)
                            return node.bestvalue;
                    }
                }
        }

        return node.bestvalue;
    }

    /// <summary>
    /// 숫자 하나가 맵 안에 있는지 반환
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    bool oneIsinmap(int num)
    {
        return num >= 0 && num < mapsize + 1;
    }

    /// <summary>
    /// 좌표 (y,x)가 맵 안에 있는지 반환
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    bool twoIsinmap(Pair coord)
    {
        return coord.x >= 0 && coord.x < mapsize + 1 && coord.y >= 0 && coord.y < mapsize + 1;
    }

    int Newheuristic()
    {
        // 개량 휴리스틱 함수
        // 현재 맵 상태(searchmap)을 바탕으로 AI에게 얼마나 유리한지 점수 매겨서 반환
        int score = 0;
        // 지금 둔 것으로 AI가 오목이나 금수(33)이 되면 sINF, -sINF set.
        // 또, 6목은 둘 수는 있으나 승리 조건으로 인식되지 않는다.
        // mapsize : 20*20 (둘 수 있는 가짓수)

        // 열/닫 1~4목 판단한다. 5목은 sINF or -sINF, 6목 이상은 0점으로 둬야하나..?

        // samsam : "3"이 있는 좌표는 0 이상으로 표시된다.
        int[,] Asamsam = new int[mapsize + 1, mapsize + 1];
        int[,] Dsamsam = new int[mapsize + 1, mapsize + 1];

        // 공격 확인

        // 가로

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 2)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && j + k < mapsize + 1)
                    {
                        if (searchmap[i, j + k] == 0)
                            zerocnt++;
                        else if (searchmap[i, j + k] == 1)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.x = j + k;
                        }

                        k++;
                    }


                    // 열린/닫힌구간 점수 계산
                    // 장목은 점수 0점.

                    // 오목은 따로 계산한다.
                    // 아예 양쪽이 닫힌 오목이어도 점수를 얻는다.
                    // 단 5개가 연속으로 있어야 하고 양옆에 같은 돌이 없어야 한다.
                    if (cnt == 5)
                    {
                        // 연속이 끊기면 거기까지 점수로 인정한다.
                        Pair start = new Pair(scoord.y, scoord.x);
                        Pair end = new Pair(lcoord.y, lcoord.x);

                        bool Issequential = true; // 5개 연속인가?

                        do
                        {
                            start.x++;

                            if (searchmap[scoord.y, scoord.x] != 2)
                            {
                                Issequential = false;
                                break;
                            }

                        }
                        while (start != end);

                        if (Issequential && ((scoord.x - 1 >= 0 && searchmap[scoord.y, scoord.x - 1] != 2) && (lcoord.x + 1 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 1] != 2)))
                            score += Ascorearr[cnt,1];
                    }

                    if (cnt < 5)
                    {

                        if ((scoord.x - 1 >= 0 && searchmap[scoord.y, scoord.x - 1] == 0) && (lcoord.x + 1 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 1] == 0))
                            score += Ascorearr[cnt, 1];
                        else if ((scoord.x - 1 >= 0 && searchmap[scoord.y, scoord.x - 1] == 0) || (lcoord.x + 1 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 1] == 0))
                            score += Ascorearr[cnt, 0];
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((scoord.x - 1 >= 0 && searchmap[scoord.y, scoord.x - 1] == 0) && (lcoord.x + 1 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 1] == 0)))
                    {

                        if ((scoord.x - 2 >= 0 && searchmap[scoord.y, scoord.x - 2] == 0) && (lcoord.x + 2 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 2] == 0))
                        {
                            // 해당되는 좌표 모두 samsam처리
                            do
                            {
                                scoord.x++;

                                if (searchmap[scoord.y, scoord.x] == 2)
                                    Asamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 세로

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 2)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && i + k < mapsize + 1)
                    {
                        if (searchmap[i + k, j] == 0)
                            zerocnt++;
                        else if (searchmap[i + k, j] == 1)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                        }

                        k++;
                    }


                    // 열린/닫힌구간 점수 계산
                    // 장목은 점수 0점.

                    // 오목은 따로 계산한다.
                    // 아예 양쪽이 닫힌 오목이어도 점수를 얻는다.
                    // 단 5개가 연속으로 있어야 하고 양옆에 같은 돌이 없어야 한다.
                    if (cnt == 5)
                    {
                        // 연속이 끊기면 거기까지 점수로 인정한다.
                        Pair start = new Pair(scoord.y, scoord.x);
                        Pair end = new Pair(lcoord.y, lcoord.x);

                        bool Issequential = true; // 5개 연속인가?

                        do
                        {
                            start.y++;

                            if (searchmap[scoord.y, scoord.x] != 2)
                            {
                                Issequential = false;
                                break;
                            }

                        }
                        while (start != end);

                        if (Issequential && ((scoord.y - 1 >= 0 && searchmap[scoord.y - 1, scoord.x] != 2) && (lcoord.y + 1 < mapsize + 1 && searchmap[lcoord.y + 1, lcoord.x] != 2)))
                            score += Ascorearr[cnt,1];
                    }

                    if (cnt < 5)
                    {

                        if ((scoord.y - 1 >= 0 && searchmap[scoord.y - 1, scoord.x] == 0) && (lcoord.y + 1 < mapsize + 1 && searchmap[lcoord.y + 1, lcoord.x] == 0))
                            score += Ascorearr[cnt, 1];
                        else if ((scoord.y - 1 >= 0 && searchmap[scoord.y - 1, scoord.x] == 0) || (lcoord.y + 1 < mapsize + 1 && searchmap[lcoord.y + 1, lcoord.x] == 0))
                            score += Ascorearr[cnt, 0];
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((scoord.y - 1 >= 0 && searchmap[scoord.y - 1, scoord.x] == 0) && (lcoord.y + 1 < mapsize + 1 && searchmap[lcoord.y + 1, lcoord.x] == 0)))
                    {

                        if ((scoord.y - 2 >= 0 && searchmap[scoord.y - 2, scoord.x] == 0) && (lcoord.y + 2 < mapsize + 1 && searchmap[lcoord.y + 2, lcoord.x] == 0))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            do
                            {
                                scoord.y++;

                                if (searchmap[scoord.y, scoord.x] == 2)
                                    Asamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 대각선

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 2)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && twoIsinmap(new Pair(i + k, j + k)))
                    {
                        if (searchmap[i + k, j+k] == 0)
                            zerocnt++;
                        else if (searchmap[i + k, j+k] == 1)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                            lcoord.x = j + k;
                        }

                        k++;
                    }


                    // 열린/닫힌구간 점수 계산
                    // 장목은 점수 0점.

                    // 오목은 따로 계산한다.
                    // 아예 양쪽이 닫힌 오목이어도 점수를 얻는다.
                    // 단 5개가 연속으로 있어야 하고 양옆에 같은 돌이 없어야 한다.
                    if (cnt == 5)
                    {
                        // 연속이 끊기면 거기까지 점수로 인정한다.
                        Pair start = new Pair(scoord.y, scoord.x);
                        Pair end = new Pair(lcoord.y, lcoord.x);

                        bool Issequential = true; // 5개 연속인가?

                        do
                        {
                            start.y++;
                            start.x++;

                            if (searchmap[scoord.y, scoord.x] != 2)
                            {
                                Issequential = false;
                                break;
                            }

                        }
                        while (start != end);

                        if (Issequential && ((twoIsinmap(new Pair(scoord.y - 1, scoord.x - 1)) && searchmap[scoord.y - 1, scoord.x - 1] != 2) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && searchmap[lcoord.y + 1, lcoord.x + 1] != 2)))
                            score += Ascorearr[cnt,1];
                    }

                    if (cnt < 5)
                    {

                        if ((twoIsinmap(new Pair(scoord.y-1, scoord.x-1)) && searchmap[scoord.y - 1, scoord.x-1] == 0) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && searchmap[lcoord.y + 1, lcoord.x + 1] == 0))
                            score += Ascorearr[cnt, 1];
                        else if ((twoIsinmap(new Pair(scoord.y - 1, scoord.x - 1)) && searchmap[scoord.y - 1, scoord.x - 1] == 0) || (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && searchmap[lcoord.y + 1, lcoord.x + 1] == 0))
                            score += Ascorearr[cnt, 0];
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((twoIsinmap(new Pair(scoord.y - 1, scoord.x - 1)) && searchmap[scoord.y - 1, scoord.x - 1] == 0) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && searchmap[lcoord.y + 1, lcoord.x + 1] == 0)))
                    {

                        if ((twoIsinmap(new Pair(scoord.y - 2, scoord.x - 2)) && searchmap[scoord.y - 2, scoord.x - 2] == 0) && (twoIsinmap(new Pair(lcoord.y + 2, lcoord.x + 2)) && searchmap[lcoord.y + 2, lcoord.x + 2] == 0))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            do
                            {
                                scoord.y++;
                                scoord.x++;

                                if (searchmap[scoord.y, scoord.x] == 2)
                                    Asamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 역대각선

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 2)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && twoIsinmap(new Pair(i + k, j - k)))
                    {
                        if (searchmap[i + k, j - k] == 0)
                            zerocnt++;
                        else if (searchmap[i + k, j - k] == 1)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                            lcoord.x = j - k;
                        }

                        k++;
                    }


                    // 열린/닫힌구간 점수 계산
                    // 장목은 점수 0점.


                    // 오목은 따로 계산한다.
                    // 아예 양쪽이 닫힌 오목이어도 점수를 얻는다.
                    // 단 5개가 연속으로 있어야 하고 양옆에 같은 돌이 없어야 한다.
                    if (cnt == 5)
                    {
                        // 연속이 끊기면 거기까지 점수로 인정한다.
                        Pair start = new Pair(scoord.y, scoord.x);
                        Pair end = new Pair(lcoord.y, lcoord.x);

                        bool Issequential = true; // 5개 연속인가?

                        do
                        {
                            start.y++;
                            start.x--;

                            if (searchmap[scoord.y, scoord.x] != 2)
                            {
                                Issequential = false;
                                break;
                            }

                        }
                        while (start != end);

                        if (Issequential && ((twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && searchmap[scoord.y - 1, scoord.x + 1] != 2) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && searchmap[lcoord.y + 1, lcoord.x - 1] != 2)))
                            score += Ascorearr[cnt,1];
                    }

                    if (cnt < 5)
                    {

                        if ((twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && searchmap[scoord.y - 1, scoord.x + 1] == 0) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && searchmap[lcoord.y + 1, lcoord.x - 1] == 0))
                            score += Ascorearr[cnt, 1];
                        else if ((twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && searchmap[scoord.y - 1, scoord.x + 1] == 0) || (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && searchmap[lcoord.y + 1, lcoord.x - 1] == 0))
                            score += Ascorearr[cnt, 0];
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && searchmap[scoord.y - 1, scoord.x + 1] == 0) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && searchmap[lcoord.y + 1, lcoord.x - 1] == 0)))
                    {

                        if ((twoIsinmap(new Pair(scoord.y - 2, scoord.x + 2)) && searchmap[scoord.y - 2, scoord.x + 2] == 0) && (twoIsinmap(new Pair(lcoord.y + 2, lcoord.x - 2)) && searchmap[lcoord.y + 2, lcoord.x - 2] == 0))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            do
                            {
                                scoord.y++;
                                scoord.x--;

                                if (searchmap[scoord.y, scoord.x] == 2)
                                    Asamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 3*3 금수처리

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (Asamsam[i, j] >= 2)
                    score -= Ascorearr[5, 1];


        // ================================================================================================
        // ================================================================================================
        // ================================================================================================
        // ================================================================================================
        // ================================================================================================
        // ================================================================================================



        // 방어 확인

        // 가로

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 1)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && j + k < mapsize + 1)
                    {
                        if (searchmap[i, j + k] == 0)
                            zerocnt++;
                        else if (searchmap[i, j + k] == 2)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.x = j + k;
                        }

                        k++;
                    }


                    // 열린/닫힌구간 점수 계산
                    // 장목은 점수 0점.

                    // 오목은 따로 계산한다.
                    // 아예 양쪽이 닫힌 오목이어도 점수를 얻는다.
                    // 단 5개가 연속으로 있어야 하고 양옆에 같은 돌이 없어야 한다.
                    if (cnt == 5)
                    {
                        // 연속이 끊기면 거기까지 점수로 인정한다.
                        Pair start = new Pair(scoord.y, scoord.x);
                        Pair end = new Pair(lcoord.y, lcoord.x);

                        bool Issequential = true; // 5개 연속인가?

                        do
                        {
                            start.x++;

                            if (searchmap[scoord.y, scoord.x] != 1)
                            {
                                Issequential = false;
                                break;
                            }

                        }
                        while (start != end);

                        if (Issequential && ((scoord.x - 1 >= 0 && searchmap[scoord.y, scoord.x - 1] != 1) && (lcoord.x + 1 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 1] != 1)))
                            score += Dscorearr[cnt,1];
                    }

                    if (cnt < 5)
                    {

                        if ((scoord.x - 1 >= 0 && searchmap[scoord.y, scoord.x - 1] == 0) && (lcoord.x + 1 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 1] == 0))
                            score += Dscorearr[cnt, 1];
                        else if ((scoord.x - 1 >= 0 && searchmap[scoord.y, scoord.x - 1] == 0) || (lcoord.x + 1 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 1] == 0))
                            score += Dscorearr[cnt, 0];
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((scoord.x - 1 >= 0 && searchmap[scoord.y, scoord.x - 1] == 0) && (lcoord.x + 1 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 1] == 0)))
                    {

                        if ((scoord.x - 2 >= 0 && searchmap[scoord.y, scoord.x - 2] == 0) && (lcoord.x + 2 < mapsize + 1 && searchmap[lcoord.y, lcoord.x + 2] == 0))
                        {
                            // 해당되는 좌표 모두 samsam처리
                            do
                            {
                                scoord.x++;

                                if (searchmap[scoord.y, scoord.x] == 1)
                                    Dsamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 세로

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 1)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && i + k < mapsize + 1)
                    {
                        if (searchmap[i + k, j] == 0)
                            zerocnt++;
                        else if (searchmap[i + k, j] == 2)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                        }

                        k++;
                    }


                    // 열린/닫힌구간 점수 계산
                    // 장목은 점수 0점.

                    // 오목은 따로 계산한다.
                    // 아예 양쪽이 닫힌 오목이어도 점수를 얻는다.
                    // 단 5개가 연속으로 있어야 하고 양옆에 같은 돌이 없어야 한다.
                    if (cnt == 5)
                    {
                        // 연속이 끊기면 거기까지 점수로 인정한다.
                        Pair start = new Pair(scoord.y, scoord.x);
                        Pair end = new Pair(lcoord.y, lcoord.x);

                        bool Issequential = true; // 5개 연속인가?

                        do
                        {
                            start.y++;

                            if (searchmap[scoord.y, scoord.x] != 1)
                            {
                                Issequential = false;
                                break;
                            }

                        }
                        while (start != end);

                        if (Issequential && ((scoord.y - 1 >= 0 && searchmap[scoord.y - 1, scoord.x] != 1) && (lcoord.y + 1 < mapsize + 1 && searchmap[lcoord.y + 1, lcoord.x] != 1)))
                            score += Dscorearr[cnt,1];
                    }

                    if (cnt < 5)
                    {

                        if ((scoord.y - 1 >= 0 && searchmap[scoord.y - 1, scoord.x] == 0) && (lcoord.y + 1 < mapsize + 1 && searchmap[lcoord.y + 1, lcoord.x] == 0))
                            score += Dscorearr[cnt, 1];
                        else if ((scoord.y - 1 >= 0 && searchmap[scoord.y - 1, scoord.x] == 0) || (lcoord.y + 1 < mapsize + 1 && searchmap[lcoord.y + 1, lcoord.x] == 0))
                            score += Dscorearr[cnt, 0];
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((scoord.y - 1 >= 0 && searchmap[scoord.y - 1, scoord.x] == 0) && (lcoord.y + 1 < mapsize + 1 && searchmap[lcoord.y + 1, lcoord.x] == 0)))
                    {

                        if ((scoord.y - 2 >= 0 && searchmap[scoord.y - 2, scoord.x] == 0) && (lcoord.y + 2 < mapsize + 1 && searchmap[lcoord.y + 2, lcoord.x] == 0))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            do
                            {
                                scoord.y++;

                                if (searchmap[scoord.y, scoord.x] == 1)
                                    Dsamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 대각선

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 1)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && twoIsinmap(new Pair(i + k, j + k)))
                    {
                        if (searchmap[i + k, j + k] == 0)
                            zerocnt++;
                        else if (searchmap[i + k, j + k] == 2)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                            lcoord.x = j + k;
                        }

                        k++;
                    }


                    // 열린/닫힌구간 점수 계산
                    // 장목은 점수 0점.

                    // 오목은 따로 계산한다.
                    // 아예 양쪽이 닫힌 오목이어도 점수를 얻는다.
                    // 단 5개가 연속으로 있어야 하고 양옆에 같은 돌이 없어야 한다.
                    if (cnt == 5)
                    {
                        // 연속이 끊기면 거기까지 점수로 인정한다.
                        Pair start = new Pair(scoord.y, scoord.x);
                        Pair end = new Pair(lcoord.y, lcoord.x);

                        bool Issequential = true; // 5개 연속인가?

                        do
                        {
                            start.y++;
                            start.x++;

                            if (searchmap[scoord.y, scoord.x] != 1)
                            {
                                Issequential = false;
                                break;
                            }

                        }
                        while (start != end);

                        if (Issequential && ((twoIsinmap(new Pair(scoord.y - 1, scoord.x - 1)) && searchmap[scoord.y - 1, scoord.x - 1] != 1) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && searchmap[lcoord.y + 1, lcoord.x + 1] != 1)))
                            score += Dscorearr[cnt,1];
                    }

                    if (cnt < 5)
                    {

                        if ((twoIsinmap(new Pair(scoord.y - 1, scoord.x - 1)) && searchmap[scoord.y - 1, scoord.x - 1] == 0) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && searchmap[lcoord.y + 1, lcoord.x + 1] == 0))
                            score += Dscorearr[cnt, 1];
                        else if ((twoIsinmap(new Pair(scoord.y - 1, scoord.x - 1)) && searchmap[scoord.y - 1, scoord.x - 1] == 0) || (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && searchmap[lcoord.y + 1, lcoord.x + 1] == 0))
                            score += Dscorearr[cnt, 0];
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((twoIsinmap(new Pair(scoord.y - 1, scoord.x - 1)) && searchmap[scoord.y - 1, scoord.x - 1] == 0) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x + 1)) && searchmap[lcoord.y + 1, lcoord.x + 1] == 0)))
                    {

                        if ((twoIsinmap(new Pair(scoord.y - 2, scoord.x - 2)) && searchmap[scoord.y - 2, scoord.x - 2] == 0) && (twoIsinmap(new Pair(lcoord.y + 2, lcoord.x + 2)) && searchmap[lcoord.y + 2, lcoord.x + 2] == 0))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            do
                            {
                                scoord.y++;
                                scoord.x++;

                                if (searchmap[scoord.y, scoord.x] == 1)
                                    Dsamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 역대각선

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 1)
                {
                    int cnt = 1, zerocnt = 0;
                    int k = 1;

                    // 구간의 시작과 끝 좌표.
                    Pair scoord = new Pair(i, j);
                    Pair lcoord = new Pair(i, j);

                    while (zerocnt < 2 && twoIsinmap(new Pair(i + k, j - k)))
                    {
                        if (searchmap[i + k, j - k] == 0)
                            zerocnt++;
                        else if (searchmap[i + k, j - k] == 2)
                            break;
                        else
                        {
                            cnt++;
                            lcoord.y = i + k;
                            lcoord.x = j - k;
                        }

                        k++;
                    }


                    // 열린/닫힌구간 점수 계산
                    // 장목은 점수 0점.

                    // 오목은 따로 계산한다.
                    // 아예 양쪽이 닫힌 오목이어도 점수를 얻는다.
                    // 단 5개가 연속으로 있어야 하고 양옆에 같은 돌이 없어야 한다.
                    if (cnt == 5)
                    {
                        // 연속이 끊기면 거기까지 점수로 인정한다.
                        Pair start = new Pair(scoord.y, scoord.x);
                        Pair end = new Pair(lcoord.y, lcoord.x);

                        bool Issequential = true; // 5개 연속인가?

                        do
                        {
                            start.y++;
                            start.x--;

                            if (searchmap[scoord.y, scoord.x] != 1)
                            {
                                Issequential = false;
                                break;
                            }

                        }
                        while (start != end);

                        if (Issequential && ((twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && searchmap[scoord.y - 1, scoord.x + 1] != 1) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && searchmap[lcoord.y + 1, lcoord.x - 1] != 1)))
                            score += Dscorearr[cnt,1];
                    }

                    if (cnt < 5)
                    {

                        if ((twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && searchmap[scoord.y - 1, scoord.x + 1] == 0) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && searchmap[lcoord.y + 1, lcoord.x - 1] == 0))
                            score += Dscorearr[cnt, 1];
                        else if ((twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && searchmap[scoord.y - 1, scoord.x + 1] == 0) || (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && searchmap[lcoord.y + 1, lcoord.x - 1] == 0))
                            score += Dscorearr[cnt, 0];
                    }

                    // samsam 계산
                    // 양 끝에만 돌을 추가로 둬 봐서 열린 4가 되면 된다.
                    // 이게 만족되면 samsam 배열의 해당되는 좌표에 int++

                    if (cnt == 3 && ((twoIsinmap(new Pair(scoord.y - 1, scoord.x + 1)) && searchmap[scoord.y - 1, scoord.x + 1] == 0) && (twoIsinmap(new Pair(lcoord.y + 1, lcoord.x - 1)) && searchmap[lcoord.y + 1, lcoord.x - 1] == 0)))
                    {

                        if ((twoIsinmap(new Pair(scoord.y - 2, scoord.x + 2)) && searchmap[scoord.y - 2, scoord.x + 2] == 0) && (twoIsinmap(new Pair(lcoord.y + 2, lcoord.x - 2)) && searchmap[lcoord.y + 2, lcoord.x - 2] == 0))
                        {
                            // 해당되는 좌표 모두 samsam처리

                            do
                            {
                                scoord.y++;
                                scoord.x--;

                                if (searchmap[scoord.y, scoord.x] == 1)
                                    Dsamsam[scoord.y, scoord.x]++;

                            }
                            while (scoord != lcoord);
                        }

                    }
                }

        // 3*3 금수처리

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (Dsamsam[i, j] >= 2)
                    score += Ascorearr[5,1];

        return score;

    }
    int heuristic()
    {
        // 현재 맵 상태(searchmap)을 바탕으로 AI에게 얼마나 유리한지 점수 매겨서 반환
        int score = 0;
        // 지금 둔 것으로 AI가 오목이나 금수(33)이 되면 INF, -INF set.
        // 또, 6목은 둘 수는 있으나 승리 조건으로 인식되지 않는다.
        // mapsize : 20*20 (둘 수 있는 가짓수)

        // opencache[i][j][4]
        // (i,j)에서 특정 방향(0,1,2,3 = 가로,세로,대각선,역대각선)으로 몇수만큼 최대로 열린 연속인지 적기.
        // 공격, 방어 구할때마다 초기화해야함!
        int[,,] Aopencache = new int[mapsize + 1, mapsize + 1, 4];
        int[,,] Dopencache = new int[mapsize + 1, mapsize + 1, 4];
        

        // cross : 3*3, 3*4, 4*4 .. 의 재료가 있는지 검사한다.
        // 있는데는 모두 int++ 해서
        // 나중에 취합시 2 이상이면 그곳이 cross되는 곳!
        // 3*3은 금지이다.. -INF
        // 0으로 초기화

        // (y,x)이다..주의..

        int[,] Across = new int[mapsize + 1, mapsize + 1];
        int[,] Dcross = new int[mapsize + 1, mapsize + 1];

        // 공격(가로)
        for (int i=0;i<mapsize+1;i++)
            for(int j = 0; j < mapsize + 1; j++)
                if(searchmap[i,j] == 2)
                {
                    int k = j+1;
                    while(k < mapsize+1 && searchmap[i,k] == 2 && k-j <= 5)
                    {
                        k++;
                    }

                    // 6목 이상..
                    if (k - j > 5)
                        score -= 3000;
                    else
                    {
                        // 열린 애
                        if ((j - 1 >= 0 && searchmap[i, j - 1] == 0) && (j + 1 < mapsize + 1 && searchmap[i, j + 1] == 0))
                        {
                            score += Ascorearr[k - j, 1];
                            Aopencache[i, j, 0] = k - j;
                        }
                        else if((j - 1 >= 0 && searchmap[i, j - 1] == 0) || (j + 1 < mapsize + 1 && searchmap[i, j + 1] == 0))
                            score += Ascorearr[k - j, 0];
                    }
                }

        // 공격(세로)
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[j, i] == 2)
                {
                    int k = j + 1;
                    while (k < mapsize + 1 && searchmap[k,i] == 2 && k - j <= 5)
                    {
                        k++;
                    }

                    // 6목 이상..
                    if (k - j > 5)
                        score -= 3000;
                    else
                    {
                        // 열린 애
                        if ((j - 1 >= 0 && searchmap[j-1, i] == 0) && (j + 1 < mapsize + 1 && searchmap[j+1, i] == 0))
                        {
                            score += Ascorearr[k - j, 1];
                            Aopencache[i, j, 1] = k - j;
                        }
                        else if ((j - 1 >= 0 && searchmap[j - 1, i] == 0) || (j + 1 < mapsize + 1 && searchmap[j + 1, i] == 0))
                            score += Ascorearr[k - j, 0];
                    }
                }

        // 공격(대각선)
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 2)
                {
                    int k = 1;
                    while (i+k < mapsize + 1 && j+k < mapsize+1 && searchmap[i+k, j+k] == 2 && k <= 5)
                    {
                        k++;
                    }

                    // 6목 이상..
                    if (k > 5)
                        score -= 3000;
                    else
                    {
                        // 열린 애
                        if ((j - 1 >= 0 && i-1 >= 0 && searchmap[i-1, j - 1] == 0) && (j + 1 < mapsize + 1 && i+1 < mapsize+1 && searchmap[i+1, j + 1] == 0))
                        {
                            score += Ascorearr[k, 1];
                            Aopencache[i, j, 2] = k;
                        }
                        else if ((j - 1 >= 0 && i - 1 >= 0 && searchmap[i - 1, j - 1] == 0) || (j + 1 < mapsize + 1 && i + 1 < mapsize + 1 && searchmap[i + 1, j + 1] == 0))
                            score += Ascorearr[k, 0];
                    }
                }

        // 공격(역대각선) : / 방향
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 2)
                {
                    int k = 1;
                    while (i + k < mapsize+1 && j - k >= 0 && searchmap[i + k, j - k] == 2 && k <= 5)
                    {
                        k++;
                    }

                    // 6목 이상..
                    if (k > 5)
                        score -= 3000;
                    else
                    {
                        // 열린 애
                        if ((i - 1 >= 0 &&  j + 1 < mapsize+1 && searchmap[i - 1, j + 1] == 0) && (i + 1 < mapsize+1 &&  j-1 >= 0 && searchmap[i + 1, j - 1] == 0))
                        {
                            score += Ascorearr[k, 1];
                            Aopencache[i, j, 3] = k;
                        }
                        else if ((i - 1 >= 0 &&  j + 1 < mapsize+1 && searchmap[i - 1, j + 1] == 0) || (i + 1 < mapsize+1 &&  j-1 >= 0 && searchmap[i + 1, j - 1] == 0))
                            score += Ascorearr[k, 0];
                    }
                }

        // 공격(cross) 조사
        // Aopencache 이용
        // (i,j)에서 특정 방향(0,1,2,3 = 가로,세로,대각선,역대각선)으로 몇수만큼 최대로 열린 연속인지 적기.
        // across에 결과 int++

        // Aopencache는 범위 검사 필요 - aopencache[i][j+3] 이런것때문

        // 3수
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    switch (dir)
                    {
                        case 0:

                            // 가로

                            for(int add=0; add<4; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0) {
                                    if (Aopencache[i, j, dir] == 3)
                                    {
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            Across[i, j + k]++;
                                    }
                                }
                                else if(Aopencache[i,j,dir] == add-1 && j+add < mapsize+1 && Aopencache[i,j+add,dir] == 4-add)
                                {
                                    for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                        Across[i, j + k]++;
                                    for (int k = 0; k < Aopencache[i, j + add, dir]; k++)
                                        Across[i, j + add + k]++;
                                }
                            }
                         
                            break;

                        case 1:

                            // 세로

                            for (int add = 0; add < 4; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Aopencache[i, j, dir] == 3)
                                    {
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            Across[i+k, j]++;
                                    }
                                }
                                else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && Aopencache[i+add, j, dir] == 4 - add)
                                {
                                    for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                        Across[i+k, j]++;
                                    for (int k = 0; k < Aopencache[i+add, j, dir]; k++)
                                        Across[i+add+k, j]++;
                                }
                            }

                            break;

                        case 2:

                            // 대각선

                            for (int add = 0; add < 4; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Aopencache[i, j, dir] == 3)
                                    {
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            Across[i+k, j + k]++;
                                    }
                                }
                                else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j + add < mapsize + 1 && Aopencache[i+add, j + add, dir] == 4 - add)
                                {
                                    for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                        Across[i+k, j + k]++;
                                    for (int k = 0; k < Aopencache[i+add, j + add, dir]; k++)
                                        Across[i+add+k, j + add + k]++;
                                }
                            }

                            break;

                        case 3:

                            // 역대각선

                            for (int add = 0; add < 4; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Aopencache[i, j, dir] == 3)
                                    {
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            Across[i + k, j - k]++;
                                    }
                                }
                                else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j - add >= 0 && Aopencache[i + add, j - add, dir] == 4 - add)
                                {
                                    for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                        Across[i + k, j - k]++;
                                    for (int k = 0; k < Aopencache[i + add, j - add, dir]; k++)
                                        Across[i + add + k, j - add - k]++;
                                }
                            }

                            break;
                    }
                   
                }
            }

        // 3수 계산하기
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                
                if (Across[i, j] == 1)
                {
                    score += Ascorearr[3, 0];

                    // 이미 체크한 3수 제거
                    // 단, 3수가 다른 수와 겹쳐지지 않아야 한다.
                    // 즉 Across를 다 더한게 3이어야 한다.

                    for (int dir = 0; dir < 4; dir++)
                    {
                        switch (dir)
                        {
                            case 0:

                                // 가로

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Aopencache[i, j, dir] == 3)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                cnt += Across[i, j + k];
                                            if(cnt==3)
                                                for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                    Across[i, j + k]--;
                                        }
                                    }
                                    else if (Aopencache[i, j, dir] == add - 1 && j + add < mapsize + 1 && Aopencache[i, j + add, dir] == 4 - add)
                                    {
                                        int cnt = 0;
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            cnt += Across[i, j + k];


                                        for (int k = 0; k < Aopencache[i, j + add, dir]; k++)
                                            cnt += Across[i, j + add + k];

                                        if (cnt == 3)
                                        {
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                Across[i, j + k]--;
                                            for (int k = 0; k < Aopencache[i, j + add, dir]; k++)
                                                Across[i, j + add + k]--;
                                        }
                                    }
                                }

                                break;

                            case 1:

                                // 세로

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Aopencache[i, j, dir] == 3)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                cnt+=Across[i + k, j];
                                            if(cnt==3)
                                                for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                    Across[i + k, j]--;
                                        }
                                    }
                                    else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && Aopencache[i + add, j, dir] == 4 - add)
                                    {
                                        int cnt = 0;

                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            cnt+=Across[i + k, j];
                                        for (int k = 0; k < Aopencache[i + add, j, dir]; k++)
                                            cnt+=Across[i + add + k, j];

                                        if (cnt == 3)
                                        {
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                Across[i + k, j]--;
                                            for (int k = 0; k < Aopencache[i + add, j, dir]; k++)
                                                Across[i + add + k, j]--;
                                        }
                                    }
                                }

                                break;

                            case 2:

                                // 대각선

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Aopencache[i, j, dir] == 3)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                cnt+=Across[i + k, j + k];
                                            if (cnt == 3)
                                                for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                   Across[i + k, j + k]--;
                                        }
                                    }
                                    else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j + add < mapsize + 1 && Aopencache[i + add, j + add, dir] == 4 - add)
                                    {
                                        int cnt = 0;

                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            cnt+=Across[i + k, j + k];
                                        for (int k = 0; k < Aopencache[i + add, j + add, dir]; k++)
                                            cnt+=Across[i + add + k, j + add + k];

                                        if (cnt == 3)
                                        {
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                               Across[i + k, j + k]--;
                                            for (int k = 0; k < Aopencache[i + add, j + add, dir]; k++)
                                                Across[i + add + k, j + add + k]--;
                                        }
                                    }
                                }

                                break;

                            case 3:

                                // 역대각선

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Aopencache[i, j, dir] == 3)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                cnt+=Across[i + k, j - k];
                                            if (cnt == 3)
                                            {
                                                for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                    Across[i + k, j - k]--;
                                            }
                                        }
                                    }
                                    else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j - add >= 0 && Aopencache[i + add, j - add, dir] == 4 - add)
                                    {
                                        int cnt = 0;
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            cnt+=Across[i + k, j - k];
                                        for (int k = 0; k < Aopencache[i + add, j - add, dir]; k++)
                                            cnt+=Across[i + add + k, j - add - k];
                                        if (cnt == 3)
                                        {
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                Across[i + k, j - k]--;
                                            for (int k = 0; k < Aopencache[i + add, j - add, dir]; k++)
                                                Across[i + add + k, j - add - k]--;
                                        }
                                    }
                                }

                                break;
                        }

                    }
                }
                // 금수 3*3
                else if (Across[i, j] >= 2)
                    score -= sINF;
            }

        // Across 초기화
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                Across[i, j] = 0;

        // 4수

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    switch (dir)
                    {
                        case 0:

                            // 가로

                            for (int add = 0; add < 5; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Aopencache[i, j, dir] == 4)
                                    {
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            Across[i, j + k]++;
                                    }
                                }
                                else if (Aopencache[i, j, dir] == add - 1 && j + add < mapsize + 1 && Aopencache[i, j + add, dir] == 5 - add)
                                {
                                    for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                        Across[i, j + k]++;
                                    for (int k = 0; k < Aopencache[i, j + add, dir]; k++)
                                        Across[i, j + add + k]++;
                                }
                            }

                            break;

                        case 1:

                            // 세로

                            for (int add = 0; add < 5; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Aopencache[i, j, dir] == 4)
                                    {
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            Across[i + k, j]++;
                                    }
                                }
                                else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && Aopencache[i + add, j, dir] == 5 - add)
                                {
                                    for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                        Across[i + k, j]++;
                                    for (int k = 0; k < Aopencache[i + add, j, dir]; k++)
                                        Across[i + add + k, j]++;
                                }
                            }

                            break;

                        case 2:

                            // 대각선

                            for (int add = 0; add < 5; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Aopencache[i, j, dir] == 4)
                                    {
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            Across[i + k, j + k]++;
                                    }
                                }
                                else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j + add < mapsize + 1 && Aopencache[i + add, j + add, dir] == 5 - add)
                                {
                                    for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                        Across[i + k, j + k]++;
                                    for (int k = 0; k < Aopencache[i + add, j + add, dir]; k++)
                                        Across[i + add + k, j + add + k]++;
                                }
                            }

                            break;

                        case 3:

                            // 역대각선

                            for (int add = 0; add < 5; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Aopencache[i, j, dir] == 4)
                                    {
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            Across[i + k, j - k]++;
                                    }
                                }
                                else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j - add >= 0 && Aopencache[i + add, j - add, dir] == 5 - add)
                                {
                                    for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                        Across[i + k, j - k]++;
                                    for (int k = 0; k < Aopencache[i + add, j - add, dir]; k++)
                                        Across[i + add + k, j - add - k]++;
                                }
                            }

                            break;
                    }

                }
            }

        // 4수 계산하기

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                if (Across[i, j] == 1)
                {
                    score += Ascorearr[4, 0];

                    // 이미 체크한 4수 제거
                    // 단, 4수가 다른 수와 겹쳐지지 않아야 한다.
                    // 즉 Across를 다 더한게 4이어야 한다.

                    for (int dir = 0; dir < 4; dir++)
                    {
                        switch (dir)
                        {
                            case 0:

                                // 가로

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Aopencache[i, j, dir] == 4)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                cnt += Across[i, j + k];
                                            if (cnt == 4)
                                                for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                    Across[i, j + k]--;
                                        }
                                    }
                                    else if (Aopencache[i, j, dir] == add - 1 && j + add < mapsize + 1 && Aopencache[i, j + add, dir] == 5 - add)
                                    {
                                        int cnt = 0;
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            cnt += Across[i, j + k];


                                        for (int k = 0; k < Aopencache[i, j + add, dir]; k++)
                                            cnt += Across[i, j + add + k];

                                        if (cnt == 4)
                                        {
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                Across[i, j + k]--;
                                            for (int k = 0; k < Aopencache[i, j + add, dir]; k++)
                                                Across[i, j + add + k]--;
                                        }
                                    }
                                }

                                break;

                            case 1:

                                // 세로

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Aopencache[i, j, dir] == 4)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                cnt += Across[i + k, j];
                                            if (cnt == 4)
                                                for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                    Across[i + k, j]--;
                                        }
                                    }
                                    else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && Aopencache[i + add, j, dir] == 5 - add)
                                    {
                                        int cnt = 0;

                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            cnt += Across[i + k, j];
                                        for (int k = 0; k < Aopencache[i + add, j, dir]; k++)
                                            cnt += Across[i + add + k, j];

                                        if (cnt == 4)
                                        {
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                Across[i + k, j]--;
                                            for (int k = 0; k < Aopencache[i + add, j, dir]; k++)
                                                Across[i + add + k, j]--;
                                        }
                                    }
                                }

                                break;

                            case 2:

                                // 대각선

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Aopencache[i, j, dir] == 4)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                cnt += Across[i + k, j + k];
                                            if (cnt == 4)
                                                for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                    Across[i + k, j + k]--;
                                        }
                                    }
                                    else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j + add < mapsize + 1 && Aopencache[i + add, j + add, dir] == 5 - add)
                                    {
                                        int cnt = 0;

                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            cnt += Across[i + k, j + k];
                                        for (int k = 0; k < Aopencache[i + add, j + add, dir]; k++)
                                            cnt += Across[i + add + k, j + add + k];

                                        if (cnt == 4)
                                        {
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                Across[i + k, j + k]--;
                                            for (int k = 0; k < Aopencache[i + add, j + add, dir]; k++)
                                                Across[i + add + k, j + add + k]--;
                                        }
                                    }
                                }

                                break;

                            case 3:

                                // 역대각선

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Aopencache[i, j, dir] == 4)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                cnt += Across[i + k, j - k];
                                            if (cnt == 4)
                                            {
                                                for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                    Across[i + k, j - k]--;
                                            }
                                        }
                                    }
                                    else if (Aopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j - add >= 0 && Aopencache[i + add, j - add, dir] == 5 - add)
                                    {
                                        int cnt = 0;
                                        for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                            cnt += Across[i + k, j - k];
                                        for (int k = 0; k < Aopencache[i + add, j - add, dir]; k++)
                                            cnt += Across[i + add + k, j - add - k];
                                        if (cnt == 4)
                                        {
                                            for (int k = 0; k < Aopencache[i, j, dir]; k++)
                                                Across[i + k, j - k]--;
                                            for (int k = 0; k < Aopencache[i + add, j - add, dir]; k++)
                                                Across[i + add + k, j - add - k]--;
                                        }
                                    }
                                }

                                break;
                        }

                    }
                }
                // 4*4
                else if (Across[i, j] >= 2)
                    score += 20000;
            }

        // =====================================================================================================================
        // =====================================================================================================================
        // =====================================================================================================================
        // =====================================================================================================================
        // =====================================================================================================================

        // 방어 조사

        // 방어(가로)
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 1)
                {
                    int k = j + 1;
                    while (k < mapsize + 1 && searchmap[i, k] == 1 && k - j <= 5)
                    {
                        k++;
                    }

                    // 6목 이상..
                    if (k - j > 5)
                        score += 3000;
                    else
                    {
                        // 열린 애
                        if ((j - 1 >= 0 && searchmap[i, j - 1] == 0) && (j + 1 < mapsize + 1 && searchmap[i, j + 1] == 0))
                        {
                            score += Dscorearr[k - j, 1];
                            Dopencache[i, j, 0] = k - j;
                        }
                        else
                            score += Dscorearr[k - j, 0];
                    }
                }

        // 방어(세로)
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[j, i] == 1)
                {
                    int k = j + 1;
                    while (k < mapsize + 1 && searchmap[k, i] == 1 && k - j <= 5)
                    {
                        k++;
                    }

                    // 6목 이상..
                    if (k - j > 5)
                        score += 3000;
                    else
                    {
                        // 열린 애
                        if ((j - 1 >= 0 && searchmap[j - 1, i] == 0) && (j + 1 < mapsize + 1 && searchmap[j + 1, i] == 0))
                        {
                            score += Dscorearr[k - j, 1];
                            Dopencache[i, j, 1] = k - j;
                        }
                        else
                            score += Dscorearr[k - j, 0];
                    }
                }

        // 방어(대각선)
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 1)
                {
                    int k = 1;
                    while (i + k < mapsize + 1 && j + k < mapsize + 1 && searchmap[i + k, j + k] == 1 && k <= 5)
                    {
                        k++;
                    }

                    // 6목 이상..
                    if (k > 5)
                        score += 3000;
                    else
                    {
                        // 열린 애
                        if ((j - 1 >= 0 && i - 1 >= 0 && searchmap[i - 1, j - 1] == 0) && (j + 1 < mapsize + 1 && i + 1 < mapsize + 1 && searchmap[i + 1, j + 1] == 0))
                        {
                            score += Dscorearr[k, 1];
                            Dopencache[i, j, 2] = k;
                        }
                        else
                            score += Dscorearr[k, 0];
                    }
                }

        // 방어(역대각선) : / 방향
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                if (searchmap[i, j] == 1)
                {
                    int k = 1;
                    while (i + k < mapsize+1 && j - k >= 0 && searchmap[i + k, j - k] == 1 && k <= 5)
                    {
                        k++;
                    }

                    // 6목 이상..
                    if (k > 5)
                        score += 3000;
                    else
                    {
                        // 열린 애
                        if ((i - 1 >= 0 && j + 1 < mapsize+1 && searchmap[i - 1, j + 1] == 0) && (i + 1 < mapsize + 1 && j - 1 >= 0 && searchmap[i + 1, j - 1] == 0))
                        {
                            score += Dscorearr[k, 1];
                            Dopencache[i, j, 3] = k;
                        }
                        else
                            score += Dscorearr[k, 0];
                    }
                }

        // 방어(cross) 조사
        // Dopencache 이용
        // (i,j)에서 특정 방향(0,1,2,3 = 가로,세로,대각선,역대각선)으로 몇수만큼 최대로 열린 연속인지 적기.
        // Dcross에 결과 int++

        // Dopencache는 범위 검사 필요 - aopencache[i][j+3] 이런것때문

        // 3수
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    switch (dir)
                    {
                        case 0:

                            // 가로

                            for (int add = 0; add < 4; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Dopencache[i, j, dir] == 3)
                                    {
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            Dcross[i, j + k]++;
                                    }
                                }
                                else if (Dopencache[i, j, dir] == add - 1 && j + add < mapsize + 1 && Dopencache[i, j + add, dir] == 4 - add)
                                {
                                    for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                        Dcross[i, j + k]++;
                                    for (int k = 0; k < Dopencache[i, j + add, dir]; k++)
                                        Dcross[i, j + add + k]++;
                                }
                            }

                            break;

                        case 1:

                            // 세로

                            for (int add = 0; add < 4; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Dopencache[i, j, dir] == 3)
                                    {
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            Dcross[i + k, j]++;
                                    }
                                }
                                else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && Dopencache[i + add, j, dir] == 4 - add)
                                {
                                    for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                        Dcross[i + k, j]++;
                                    for (int k = 0; k < Dopencache[i + add, j, dir]; k++)
                                        Dcross[i + add + k, j]++;
                                }
                            }

                            break;

                        case 2:

                            // 대각선

                            for (int add = 0; add < 4; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Dopencache[i, j, dir] == 3)
                                    {
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            Dcross[i + k, j + k]++;
                                    }
                                }
                                else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j + add < mapsize + 1 && Dopencache[i + add, j + add, dir] == 4 - add)
                                {
                                    for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                        Dcross[i + k, j + k]++;
                                    for (int k = 0; k < Dopencache[i + add, j + add, dir]; k++)
                                        Dcross[i + add + k, j + add + k]++;
                                }
                            }

                            break;

                        case 3:

                            // 역대각선

                            for (int add = 0; add < 4; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Dopencache[i, j, dir] == 3)
                                    {
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            Dcross[i + k, j - k]++;
                                    }
                                }
                                else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j - add >= 0 && Dopencache[i + add, j - add, dir] == 4 - add)
                                {
                                    for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                        Dcross[i + k, j - k]++;
                                    for (int k = 0; k < Dopencache[i + add, j - add, dir]; k++)
                                        Dcross[i + add + k, j - add - k]++;
                                }
                            }

                            break;
                    }

                }
            }

        // 3수 계산하기
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                if (Dcross[i, j] == 1)
                {
                    score += Dscorearr[3, 0];

                    // 이미 체크한 3수 제거
                    // 단, 3수가 다른 수와 겹쳐지지 않아야 한다.
                    // 즉 Dcross를 다 더한게 3이어야 한다.

                    for (int dir = 0; dir < 4; dir++)
                    {
                        switch (dir)
                        {
                            case 0:

                                // 가로

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Dopencache[i, j, dir] == 3)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                cnt += Dcross[i, j + k];
                                            if (cnt == 3)
                                                for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                    Dcross[i, j + k]--;
                                        }
                                    }
                                    else if (Dopencache[i, j, dir] == add - 1 && j + add < mapsize + 1 && Dopencache[i, j + add, dir] == 4 - add)
                                    {
                                        int cnt = 0;
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            cnt += Dcross[i, j + k];


                                        for (int k = 0; k < Dopencache[i, j + add, dir]; k++)
                                            cnt += Dcross[i, j + add + k];

                                        if (cnt == 3)
                                        {
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                Dcross[i, j + k]--;
                                            for (int k = 0; k < Dopencache[i, j + add, dir]; k++)
                                                Dcross[i, j + add + k]--;
                                        }
                                    }
                                }

                                break;

                            case 1:

                                // 세로

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Dopencache[i, j, dir] == 3)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                cnt += Dcross[i + k, j];
                                            if (cnt == 3)
                                                for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                    Dcross[i + k, j]--;
                                        }
                                    }
                                    else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && Dopencache[i + add, j, dir] == 4 - add)
                                    {
                                        int cnt = 0;

                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            cnt += Dcross[i + k, j];
                                        for (int k = 0; k < Dopencache[i + add, j, dir]; k++)
                                            cnt += Dcross[i + add + k, j];

                                        if (cnt == 3)
                                        {
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                Dcross[i + k, j]--;
                                            for (int k = 0; k < Dopencache[i + add, j, dir]; k++)
                                                Dcross[i + add + k, j]--;
                                        }
                                    }
                                }

                                break;

                            case 2:

                                // 대각선

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Dopencache[i, j, dir] == 3)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                cnt += Dcross[i + k, j + k];
                                            if (cnt == 3)
                                                for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                    Dcross[i + k, j + k]--;
                                        }
                                    }
                                    else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j + add < mapsize + 1 && Dopencache[i + add, j + add, dir] == 4 - add)
                                    {
                                        int cnt = 0;

                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            cnt += Dcross[i + k, j + k];
                                        for (int k = 0; k < Dopencache[i + add, j + add, dir]; k++)
                                            cnt += Dcross[i + add + k, j + add + k];

                                        if (cnt == 3)
                                        {
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                Dcross[i + k, j + k]--;
                                            for (int k = 0; k < Dopencache[i + add, j + add, dir]; k++)
                                                Dcross[i + add + k, j + add + k]--;
                                        }
                                    }
                                }

                                break;

                            case 3:

                                // 역대각선

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Dopencache[i, j, dir] == 3)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                cnt += Dcross[i + k, j - k];
                                            if (cnt == 3)
                                            {
                                                for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                    Dcross[i + k, j - k]--;
                                            }
                                        }
                                    }
                                    else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j - add >= 0 && Dopencache[i + add, j - add, dir] == 4 - add)
                                    {
                                        int cnt = 0;
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            cnt += Dcross[i + k, j - k];
                                        for (int k = 0; k < Dopencache[i + add, j - add, dir]; k++)
                                            cnt += Dcross[i + add + k, j - add - k];
                                        if (cnt == 3)
                                        {
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                Dcross[i + k, j - k]--;
                                            for (int k = 0; k < Dopencache[i + add, j - add, dir]; k++)
                                                Dcross[i + add + k, j - add - k]--;
                                        }
                                    }
                                }

                                break;
                        }

                    }
                }
                // 금수 3*3
                else if (Dcross[i, j] >= 2)
                    score += sINF;
            }

        // Dcross 초기화
        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
                Dcross[i, j] = 0;

        // 4수

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    switch (dir)
                    {
                        case 0:

                            // 가로

                            for (int add = 0; add < 5; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Dopencache[i, j, dir] == 4)
                                    {
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            Dcross[i, j + k]++;
                                    }
                                }
                                else if (Dopencache[i, j, dir] == add - 1 && j + add < mapsize + 1 && Dopencache[i, j + add, dir] == 5 - add)
                                {
                                    for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                        Dcross[i, j + k]++;
                                    for (int k = 0; k < Dopencache[i, j + add, dir]; k++)
                                        Dcross[i, j + add + k]++;
                                }
                            }

                            break;

                        case 1:

                            // 세로

                            for (int add = 0; add < 5; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Dopencache[i, j, dir] == 4)
                                    {
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            Dcross[i + k, j]++;
                                    }
                                }
                                else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && Dopencache[i + add, j, dir] == 5 - add)
                                {
                                    for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                        Dcross[i + k, j]++;
                                    for (int k = 0; k < Dopencache[i + add, j, dir]; k++)
                                        Dcross[i + add + k, j]++;
                                }
                            }

                            break;

                        case 2:

                            // 대각선

                            for (int add = 0; add < 5; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Dopencache[i, j, dir] == 4)
                                    {
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            Dcross[i + k, j + k]++;
                                    }
                                }
                                else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j + add < mapsize + 1 && Dopencache[i + add, j + add, dir] == 5 - add)
                                {
                                    for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                        Dcross[i + k, j + k]++;
                                    for (int k = 0; k < Dopencache[i + add, j + add, dir]; k++)
                                        Dcross[i + add + k, j + add + k]++;
                                }
                            }

                            break;

                        case 3:

                            // 역대각선

                            for (int add = 0; add < 5; add++)
                            {
                                if (add == 1)
                                    continue;
                                if (add == 0)
                                {
                                    if (Dopencache[i, j, dir] == 4)
                                    {
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            Dcross[i + k, j - k]++;
                                    }
                                }
                                else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j - add >= 0 && Dopencache[i + add, j - add, dir] == 5 - add)
                                {
                                    for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                        Dcross[i + k, j - k]++;
                                    for (int k = 0; k < Dopencache[i + add, j - add, dir]; k++)
                                        Dcross[i + add + k, j - add - k]++;
                                }
                            }

                            break;
                    }

                }
            }

        // 4수 계산하기

        for (int i = 0; i < mapsize + 1; i++)
            for (int j = 0; j < mapsize + 1; j++)
            {
                if (Dcross[i, j] == 1)
                {
                    score += Dscorearr[4, 0];

                    // 이미 체크한 4수 제거
                    // 단, 4수가 다른 수와 겹쳐지지 않아야 한다.
                    // 즉 Dcross를 다 더한게 4이어야 한다.

                    for (int dir = 0; dir < 4; dir++)
                    {
                        switch (dir)
                        {
                            case 0:

                                // 가로

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Dopencache[i, j, dir] == 4)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                cnt += Dcross[i, j + k];
                                            if (cnt == 4)
                                                for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                    Dcross[i, j + k]--;
                                        }
                                    }
                                    else if (Dopencache[i, j, dir] == add - 1 && j + add < mapsize + 1 && Dopencache[i, j + add, dir] == 5 - add)
                                    {
                                        int cnt = 0;
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            cnt += Dcross[i, j + k];


                                        for (int k = 0; k < Dopencache[i, j + add, dir]; k++)
                                            cnt += Dcross[i, j + add + k];

                                        if (cnt == 4)
                                        {
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                Dcross[i, j + k]--;
                                            for (int k = 0; k < Dopencache[i, j + add, dir]; k++)
                                                Dcross[i, j + add + k]--;
                                        }
                                    }
                                }

                                break;

                            case 1:

                                // 세로

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Dopencache[i, j, dir] == 4)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                cnt += Dcross[i + k, j];
                                            if (cnt == 4)
                                                for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                    Dcross[i + k, j]--;
                                        }
                                    }
                                    else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && Dopencache[i + add, j, dir] == 5 - add)
                                    {
                                        int cnt = 0;

                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            cnt += Dcross[i + k, j];
                                        for (int k = 0; k < Dopencache[i + add, j, dir]; k++)
                                            cnt += Dcross[i + add + k, j];

                                        if (cnt == 4)
                                        {
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                Dcross[i + k, j]--;
                                            for (int k = 0; k < Dopencache[i + add, j, dir]; k++)
                                                Dcross[i + add + k, j]--;
                                        }
                                    }
                                }

                                break;

                            case 2:

                                // 대각선

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Dopencache[i, j, dir] == 4)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                cnt += Dcross[i + k, j + k];
                                            if (cnt == 4)
                                                for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                    Dcross[i + k, j + k]--;
                                        }
                                    }
                                    else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j + add < mapsize + 1 && Dopencache[i + add, j + add, dir] == 5 - add)
                                    {
                                        int cnt = 0;

                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            cnt += Dcross[i + k, j + k];
                                        for (int k = 0; k < Dopencache[i + add, j + add, dir]; k++)
                                            cnt += Dcross[i + add + k, j + add + k];

                                        if (cnt == 4)
                                        {
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                Dcross[i + k, j + k]--;
                                            for (int k = 0; k < Dopencache[i + add, j + add, dir]; k++)
                                                Dcross[i + add + k, j + add + k]--;
                                        }
                                    }
                                }

                                break;

                            case 3:

                                // 역대각선

                                for (int add = 0; add < 4; add++)
                                {
                                    if (add == 1)
                                        continue;
                                    if (add == 0)
                                    {
                                        if (Dopencache[i, j, dir] == 4)
                                        {
                                            int cnt = 0;
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                cnt += Dcross[i + k, j - k];
                                            if (cnt == 4)
                                            {
                                                for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                    Dcross[i + k, j - k]--;
                                            }
                                        }
                                    }
                                    else if (Dopencache[i, j, dir] == add - 1 && i + add < mapsize + 1 && j - add >= 0 && Dopencache[i + add, j - add, dir] == 5 - add)
                                    {
                                        int cnt = 0;
                                        for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                            cnt += Dcross[i + k, j - k];
                                        for (int k = 0; k < Dopencache[i + add, j - add, dir]; k++)
                                            cnt += Dcross[i + add + k, j - add - k];
                                        if (cnt == 4)
                                        {
                                            for (int k = 0; k < Dopencache[i, j, dir]; k++)
                                                Dcross[i + k, j - k]--;
                                            for (int k = 0; k < Dopencache[i + add, j - add, dir]; k++)
                                                Dcross[i + add + k, j - add - k]--;
                                        }
                                    }
                                }

                                break;
                        }

                    }
                }
                // 4*4 
                else if (Dcross[i, j] >= 2)
                    score -= 20000;
            }

        return score;
    }


}

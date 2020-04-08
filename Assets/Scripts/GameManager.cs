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

    public enum whatColor { white, black };
    public whatColor playercolor; // 플레이어 색깔

    public bool beplayerturn; // 바로 전 턴 플레이어 턴?
    public bool curplayerturn; // 이번 턴 플레이어 턴?

    private bool IsStart = true; // 게임이 막 시작했나? 
    public bool IsgameOver = false; // 게임이 끝났나?
    public bool IsActionEnded = false; // 돌 놓기가 끝났나?

    public float timer = 0; // 남은 시간.
    public float turntime = 60f; // 한 턴 시간.

    private ControlUI controlUI; // UI control 위임할 스크립트.

    public bool[,] IsSet; // 각 좌표에 바둑돌이 놓였나?

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        IsSet = new bool[mapsize+1, mapsize+1];

        // 초기화
        for (int i = 0; i <= mapsize; i++)
            for (int j = 0; j <= mapsize; j++)
                IsSet[i, j] = false;

    }

    // Update is called once per frame
    void Update()
    {
        // 시작 화면 말고 안으로 들어온 후 게임 시작됨.
        if (SceneManager.GetActiveScene().name == "Main" && !IsgameOver)
        {
            if (timer <= 0 || IsActionEnded)
                ChangeTurn();
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
        IsActionEnded = false;
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
}

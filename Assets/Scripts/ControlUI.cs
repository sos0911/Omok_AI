﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ControlUI : MonoBehaviour
{
    // UI 부분
    public Text playerturntext;
    public Text lefttimetext;
    public GameObject GameoverPanel;

    private Text WhowinsText;

    private void Start()
    {
        // caching
        // getcomponent 함수는 특정 오브젝트의 editor에 표시되는 것들 중 일부를 가져오는 거다.
        // 자식을 가져오는 게 아님!
        WhowinsText = GameoverPanel.gameObject.transform.GetChild(0).gameObject.GetComponent<Text>();
        GameManager.instance.GetcacheofUI();
    }

    public void UIchangePlayerturn(string str)
    {
        playerturntext.text = str;
    }

    public void UIchangelefttime(float time)
    {
        lefttimetext.text = "남은 시간 : " + (int)time + "초";
    }

    public void UIchangeGameoverText(string str)
    {
        GameoverPanel.SetActive(true);
        if (str == "Draw")
            WhowinsText.text = "Draw!";
        else
            WhowinsText.text = str + " wins!";
    }


    public void RestartGame()
    {
        GameManager.instance.gameObject.SetActive(true);
        SceneManager.LoadScene("Main");
    }
}

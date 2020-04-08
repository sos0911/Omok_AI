using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControlUI : MonoBehaviour
{
    // UI 부분
    public Text playerturntext;
    public Text lefttimetext;

    public void UIchangePlayerturn(string str)
    {
        playerturntext.text = str;
    }

    public void UIchangelefttime(float time)
    {
        lefttimetext.text = "남은 시간 : " + (int)time + "초";
    }

    private void OnEnable()
    {
        GameManager.instance.GetcacheofUI();
    }
}

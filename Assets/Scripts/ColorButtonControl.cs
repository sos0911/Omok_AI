using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ColorButtonControl : MonoBehaviour
{
    public TextMeshProUGUI warningtext;
    // 플레이어가 색을 집었나?
    public bool Iscolorpicked = false;

    /// <summary>
    /// 어떤 색을 선택했는지에 따라 반영
    /// </summary>
    /// <param name="Isblack"></param>
    public void Onclick_Button(string color)
    {
        if (color == "black")
            PlayerPrefs.SetString("PlayerColor", "Black");
        else
            PlayerPrefs.SetString("PlayerColor", "White");
        // bool set
        Iscolorpicked = true;
    }

    /// <summary>
    /// 플레이어가 색지정을 완료할 시에만 다음 씬으로 넘어감. 아니면 경고창
    /// </summary>
    public void IsplayerColorpicked()
    {
        // 이전에 이미 경고문이 있었다면 지우기
        if (warningtext.gameObject.activeInHierarchy)
            warningtext.gameObject.SetActive(false);
        if (Iscolorpicked)
            SceneManager.LoadScene("Main");
        else
        {
            warningtext.gameObject.SetActive(true);
        }
    }
}

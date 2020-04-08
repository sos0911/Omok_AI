using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NotColorSelectedWarn : MonoBehaviour
{
    public TextMeshProUGUI warningtext;
    public const float initialEffectTime = 5f; // 처음 정한 효과시간.
    public float EffectTime; // 현재 효과시간
    public float delayTime = 0.5f;

    private void OnEnable()
    {
        EffectTime = initialEffectTime;
        // 1초간 색 점등
        while(EffectTime > 0f)
        {
            StartCoroutine(Changecolor());
            EffectTime -= delayTime;
        }
    }
    IEnumerator Changecolor()
    {
        if (warningtext.color == Color.white)
            warningtext.color = Color.red;
        else
            warningtext.color = Color.white;
        yield return new WaitForSeconds(delayTime);
    }
}

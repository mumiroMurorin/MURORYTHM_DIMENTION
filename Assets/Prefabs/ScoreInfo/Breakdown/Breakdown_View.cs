using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Breakdown_View : MonoBehaviour
{
    [SerializeField] GameObject perfectItem;
    [SerializeField] TextMeshProUGUI perfectCount_tmp;
    [SerializeField] DoScalePulseBuilder scalePulseBuilderPerfect;

    [Space(15)]
    [SerializeField] GameObject greatItem;
    [SerializeField] TextMeshProUGUI greatCount_tmp;
    [SerializeField] DoScalePulseBuilder scalePulseBuilderGreat;
    
    [Space(15)]
    [SerializeField] GameObject goodItem;
    [SerializeField] TextMeshProUGUI goodCount_tmp;
    [SerializeField] DoScalePulseBuilder scalePulseBuilderGood;

    [Space(15)]
    [SerializeField] GameObject missItem;
    [SerializeField] TextMeshProUGUI missCount_tmp;
    [SerializeField] DoScalePulseBuilder scalePulseBuilderMiss;

    public void OnChangePerfectCount(int perfectCount)
    {
        if (perfectCount_tmp == null) { return; }

        perfectItem?.SetActive(perfectCount > 0);
        perfectCount_tmp.text = perfectCount.ToString();

        scalePulseBuilderPerfect?.ApplyScalePulse(perfectCount_tmp.transform);
    }

    public void OnChangeGreatCount(int greatCount)
    {
        if (greatCount_tmp == null) { return; }

        greatItem?.SetActive(greatCount > 0);
        greatCount_tmp.text = greatCount.ToString();

        scalePulseBuilderGreat?.ApplyScalePulse(greatCount_tmp.transform);
    }

    public void OnChangeGoodCount(int goodCount)
    {
        if (goodCount_tmp == null) { return; }

        goodItem?.SetActive(goodCount > 0);
        goodCount_tmp.text = goodCount.ToString();

        scalePulseBuilderGood?.ApplyScalePulse(goodCount_tmp.transform);
    }

    public void OnChangeMissCount(int missCount)
    {
        if (missCount_tmp == null) { return; }

        missItem.SetActive(missCount > 0);
        missCount_tmp.text = missCount.ToString();

        scalePulseBuilderMiss?.ApplyScalePulse(missCount_tmp.transform);
    }
}

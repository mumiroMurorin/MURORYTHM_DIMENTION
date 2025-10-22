using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Breakdown_View : MonoBehaviour
{
    [SerializeField] GameObject perfectItem;
    [SerializeField] TextMeshProUGUI perfectCount_tmp;

    [SerializeField] GameObject greatItem;
    [SerializeField] TextMeshProUGUI greatCount_tmp;

    [SerializeField] GameObject goodItem;
    [SerializeField] TextMeshProUGUI goodCount_tmp;

    [SerializeField] GameObject missItem;
    [SerializeField] TextMeshProUGUI missCount_tmp;

    public void OnChangePerfectCount(int perfectCount)
    {
        if (perfectCount_tmp == null) { return; }

        perfectItem?.SetActive(perfectCount > 0);
        perfectCount_tmp.text = perfectCount.ToString();
    }

    public void OnChangeGreatCount(int greatCount)
    {
        if (greatCount_tmp == null) { return; }

        greatItem?.SetActive(greatCount > 0);
        greatCount_tmp.text = greatCount.ToString();
    }

    public void OnChangeGoodCount(int goodCount)
    {
        if (goodCount_tmp == null) { return; }

        goodItem?.SetActive(goodCount > 0);
        goodCount_tmp.text = goodCount.ToString();
    }

    public void OnChangeMissCount(int missCount)
    {
        if (missCount_tmp == null) { return; }

        missItem.SetActive(missCount > 0);
        missCount_tmp.text = missCount.ToString();
    }
}

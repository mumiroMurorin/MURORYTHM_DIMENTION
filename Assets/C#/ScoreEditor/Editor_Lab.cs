using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Editor_Lab : MonoBehaviour
{
    RectTransform this_r;

    void Start()
    {
        this_r = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(this_r.anchoredPosition);
        //this_r.anchorMin = this_r.anchorMax = new Vector2();
    }
}

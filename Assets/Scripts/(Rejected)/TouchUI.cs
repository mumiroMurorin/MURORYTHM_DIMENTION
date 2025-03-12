using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchUI : MonoBehaviour
{
    [Header("0～16の順にオブジェクト")]
    [SerializeField] private GameObject[] flag_obj;
    [Header("空")]
    [SerializeField] private Color32 null_color;
    [Header("決定とかメイン色")]
    [SerializeField] private Color32 main_color;
    [Header("上昇色と下降色")]
    [SerializeField] private Color32 up_color;
    [SerializeField] private Color32 down_color;
    [Header("左右色")]
    [SerializeField] private Color32 horizon_color;
    [Header("サブカラー達")]
    [SerializeField] private Color32[] sub_color;

    [Header("タッチアウトライン色")]
    [SerializeField] private Color32 touch_color;
    [Header("フリーアウトライン色")]
    [SerializeField] private Color32 free_color;

    private Image[] flag_ima;
    private Outline[] flag_out;

    void Start()
    {
        flag_ima = new Image[16];
        flag_out = new Outline[16];
        int i = 0;
        foreach(GameObject g in flag_obj)
        {
            flag_ima[i] = g.GetComponent<Image>();
            flag_out[i++] = g.GetComponent<Outline>();
        }
    }

    void Update()
    {
        
    }

    //タッチされた場所を光らせる
    public void TouchFlag(int[] nums)
    {
        foreach(int i in nums)
        {
            StartCoroutine(SwitchFlag(i));
        }
    }

    //開放された場所を元に戻す
    public void FreeFlag(int[] nums)
    {
        foreach (int i in nums)
        {
            flag_out[i].effectColor = free_color;
        }
    }

    //指定された場所達を指定された色達に
    public void SetFlagColors(int[] nums, string str)
    {
        Color32 color = Color.black;
        switch (str)
        {
            case "null":
                color = null_color;
                break;
            case "main":
                color = main_color;
                break;
            case "up":
                color = up_color;
                break;
            case "down":
                color = down_color;
                break;
            case "horizon":
                color = horizon_color;
                break;
            default:
                if (str.Contains("sub"))
                {
                    string num_str = str.Split('_')[1];
                    int num;
                    if (int.TryParse(num_str, out num)) { color = sub_color[num]; }
                    else {
                        Debug.LogError("数字に変換できませんでした: " + num_str);
                        return;
                    }
                }
                else
                {
                    Debug.LogError("予期せぬ引数です: " + str);
                    return;
                }
                break;
        }
        
        foreach (int i in nums)
        {
            SetFlagColor(i, color);
        }
    }

    //指定された場所を指定された色に
    private void SetFlagColor(int num, Color32 color32)
    {
        flag_ima[num].color = color32;
    }

    private IEnumerator SwitchFlag(int num)
    {
        flag_out[num].effectColor = touch_color;
        yield return new WaitForSeconds(0.1f);
        flag_out[num].effectColor = free_color;
    }
}
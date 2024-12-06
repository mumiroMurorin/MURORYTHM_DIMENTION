using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchUI : MonoBehaviour
{
    [Header("0�`16�̏��ɃI�u�W�F�N�g")]
    [SerializeField] private GameObject[] flag_obj;
    [Header("��")]
    [SerializeField] private Color32 null_color;
    [Header("����Ƃ����C���F")]
    [SerializeField] private Color32 main_color;
    [Header("�㏸�F�Ɖ��~�F")]
    [SerializeField] private Color32 up_color;
    [SerializeField] private Color32 down_color;
    [Header("���E�F")]
    [SerializeField] private Color32 horizon_color;
    [Header("�T�u�J���[�B")]
    [SerializeField] private Color32[] sub_color;

    [Header("�^�b�`�A�E�g���C���F")]
    [SerializeField] private Color32 touch_color;
    [Header("�t���[�A�E�g���C���F")]
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

    //�^�b�`���ꂽ�ꏊ�����点��
    public void TouchFlag(int[] nums)
    {
        foreach(int i in nums)
        {
            StartCoroutine(SwitchFlag(i));
        }
    }

    //�J�����ꂽ�ꏊ�����ɖ߂�
    public void FreeFlag(int[] nums)
    {
        foreach (int i in nums)
        {
            flag_out[i].effectColor = free_color;
        }
    }

    //�w�肳�ꂽ�ꏊ�B���w�肳�ꂽ�F�B��
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
                        Debug.LogError("�����ɕϊ��ł��܂���ł���: " + num_str);
                        return;
                    }
                }
                else
                {
                    Debug.LogError("�\�����ʈ����ł�: " + str);
                    return;
                }
                break;
        }
        
        foreach (int i in nums)
        {
            SetFlagColor(i, color);
        }
    }

    //�w�肳�ꂽ�ꏊ���w�肳�ꂽ�F��
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
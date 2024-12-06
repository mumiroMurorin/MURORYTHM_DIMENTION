using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RootOption{
    public bool isUseSlider;
    public bool isUseKinect;
    public string slider_com_str;
    public int slider_speed;
    public float space_sensitivity;
}

public class RootUI : MonoBehaviour
{
    [Header("管理者画面")]
    [SerializeField] private GameObject rootCanvas_obj;
    [Header("COMInput")]
    [SerializeField] private TMP_InputField com_input;
    [Header("SpeedInput")]
    [SerializeField] private TMP_InputField speed_input;
    [Header("isKinectToggle")]
    [SerializeField] private Toggle kinect_tog;
    [Header("isSliderToggle")]
    [SerializeField] private Toggle slider_tog;
    [Header("SpaceSensitivity")]
    [SerializeField] private TMP_InputField spaceSensitivity_input;

    //画面の表示
    public void DisRootScreen(bool isDis)
    {
        rootCanvas_obj.SetActive(isDis);
    }

    //-------------------代入系-------------------

    //初期化
    public void Init(RootOption r)
    {
        SetComString(r.slider_com_str);
        SetSpeedNum(r.slider_speed);
        SetKinectBool(r.isUseKinect);
        SetSliderBool(r.isUseSlider);
        SetSpaceSensitivity(r.space_sensitivity);
        rootCanvas_obj.SetActive(false);
    }

    //COMInputに文字を代入
    private void SetComString(string str) { com_input.text = str; }

    //SpeedInputに文字を代入
    private void SetSpeedNum(int speed) { speed_input.text = speed.ToString(); }

    //SpaceSensitivityに値を代入
    private void SetSpaceSensitivity(float sensitivity) { spaceSensitivity_input.text = sensitivity.ToString(); }

    //KinectToggleにboolを代入
    private void SetKinectBool(bool b) { kinect_tog.isOn = b; }

    //SliderToggleにboolを代入
    private void SetSliderBool(bool b) { slider_tog.isOn = b; }

    //-------------------リターン系-------------------

    //ルートオプションがセットされているか返す
    public bool IsReturnSetRootOption()
    {
        if (slider_tog.isOn && (com_input.text == "" || speed_input.text == "")) { return false; }
        return true;
    }

    //ルートオプションを返す
    public RootOption ReturnRootOption()
    {
        RootOption r = new RootOption();
        r.isUseSlider = slider_tog.isOn;
        r.isUseKinect = kinect_tog.isOn;
        r.slider_com_str = com_input.text;
        r.space_sensitivity = float.Parse(spaceSensitivity_input.text);
        if (r.isUseSlider) { r.slider_speed = int.Parse(speed_input.text); }
        return r;
    }
}

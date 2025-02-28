using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

public class SliderInput : MonoBehaviour
{
    //対応するキー
    KeyCode[] KEY_CODES_16K = new KeyCode[16]
    {
        KeyCode.X,KeyCode.W,KeyCode.V,KeyCode.U,
        KeyCode.M,KeyCode.N,KeyCode.O, KeyCode.P,
        KeyCode.L, KeyCode.K, KeyCode.J,KeyCode.I,
        KeyCode.A,KeyCode.B,KeyCode.C,KeyCode.D
    };

    //対応するキー(3K)
    KeyCode[] KEY_CODES_3K = new KeyCode[16]
    {
        KeyCode.D,KeyCode.D,KeyCode.D,KeyCode.D,KeyCode.D,
        KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,
        KeyCode.K, KeyCode.K,KeyCode.K,KeyCode.K,KeyCode.K
    };

    //対応するキー(4K)
    KeyCode[] KEY_CODES_4K = new KeyCode[16]
    {
        KeyCode.D,KeyCode.D,KeyCode.D,KeyCode.D,KeyCode.D,
        KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,
        KeyCode.K, KeyCode.K,KeyCode.K,KeyCode.K,KeyCode.K
    };

    //対応するセンサ番号
    int[] SENSOR_INDEX = new int[16]
    { 0,1,2,3,
      11,10,9,8,
      12,13,14,15,
      23,22,21,20
    };

    [Header("専コン？")]
    [SerializeField] private bool isSerialMode;
    [SerializeField] private SerialHandler serial;

    ReactiveProperty<bool>[] inputs;
    public IReadOnlyReactiveProperty<bool>[] InputsReactiveProperty { get { return inputs; } }

    private void Awake()
    {
        inputs = new ReactiveProperty<bool>[16];
        for(int i = 0; i < 16; i++)
        {
            inputs[i] = new ReactiveProperty<bool>();
        }
    }

    void Update()
    {
        //FixedUpdateで扱えるよう変換
        ConvertInputData();
    }

    //Updateで得た入力情報をFixedUpdateで扱えるようにクラスに代入
    private void ConvertInputData()
    {
        //デバイスが接続されているとき
        if (isSerialMode && serial.IsReturnConnectDevice())
        {
            for (int i = 0; i < 16; i++)
            {
                if (inputs[i].Value == IsReturnTouchDevice(SENSOR_INDEX[i])) { continue; }
                inputs[i].Value = IsReturnTouchDevice(SENSOR_INDEX[i]);
            }
        }
        //キーボード入力モード
        else
        {
            for (int i = 0; i < 16; i++)
            {
                if (inputs[i].Value == Input.GetKey(KEY_CODES_16K[i])) { continue; }
                inputs[i].Value = Input.GetKey(KEY_CODES_16K[i]);
            }
        }
    }

    //管理者設定を反映
    public void SetRootOption(RootOption r)
    {
        //Debug.Log($"【ここ】:{r.isUseKinect},{r.isUseSlider},{r.slider_com_str},{r.slider_speed},{r.space_sensitivity}");
        isSerialMode = r.isUseSlider;
        serial.SetOption(r.slider_com_str, r.slider_speed);
        serial.StartSerial();
    }

    //デバイスからの入力を返す
    private bool IsReturnTouchDevice(int i)
    {
        string str = serial.ReturnTouchData();
        if (str == null) { return false; }
        return str[i] == '1' ? true : false;
    }

    //スライドがタッチされた瞬間か返す(配列)
    public IReadOnlyReactiveProperty<bool> GetSliderReactiveProperty(int index)
    {
        return InputsReactiveProperty[index];
    }

    //スライドがタッチされている最中か返す(配列)
    public bool IsReturnSlidersTouching(int[] nums)
    {
        foreach (int i in nums)
        {
            if (IsReturnSliderTouching(i)) { return true; }
        }
        return false;
    }

    //スライドがタッチされている最中か返す
    public bool IsReturnSliderTouching(int num)
    {
        return inputs[num].Value;
    }

    //専コン使用モードかどうか返す
    public bool IsReturnSerialMode()
    {
        return isSerialMode;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SliderInput : MonoBehaviour
{
    //入力状態
    public class InputCondition
    {
        public bool isFirst;
        public bool isTouching;
    }

    //対応するキー
    KeyCode[] KEY_CODES_16K = new KeyCode[16]
    {
        KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.F,
        KeyCode.G,KeyCode.H,KeyCode.J, KeyCode.K,
        KeyCode.L, KeyCode.Semicolon, KeyCode.Colon,KeyCode.RightBracket,
        KeyCode.Z,KeyCode.X,KeyCode.C,KeyCode.V
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

    List<InputCondition> inputList;

    void Start()
    {
        inputList = new List<InputCondition>();
        for (int i = 0; i < 16; i++) { inputList.Add(new InputCondition()); }
    }

    void Update()
    {
        //FixedUpdateで扱えるよう変換
        ConvertInputData();
    }

    private void FixedUpdate()
    {
        ResetInputData();   //入力データ(GetKeyDown)の初期化
    }

    //Updateで得た入力情報をFixedUpdateで扱えるようにクラスに代入
    private void ConvertInputData()
    {
        //デバイスが接続されているとき
        if (isSerialMode && serial.IsReturnConnectDevice())
        {
            for (int i = 0; i < 16; i++)
            {
                //入力された直後？
                if (!inputList[i].isTouching && IsReturnTouchDevice(SENSOR_INDEX[i]))
                {
                    inputList[i].isFirst = true;
                    inputList[i].isTouching = true;
                }
                //入力されている最中？
                else if (IsReturnTouchDevice(SENSOR_INDEX[i]))
                {
                    inputList[i].isTouching = true;
                }
                //なにもねぇ
                else
                {
                    inputList[i].isTouching = false;
                }
            }
        }
        //キーボード入力モード
        else
        {
            for (int i = 0; i < 16; i++)
            {
                //入力された直後？
                if (Input.GetKeyDown(KEY_CODES_16K[i]))
                {
                    inputList[i].isFirst = true;
                    inputList[i].isTouching = true;
                }
                //入力されている最中？
                else if (Input.GetKey(KEY_CODES_16K[i])) { inputList[i].isTouching = true; }
                //なにもねぇ
                else { inputList[i].isTouching = false; }
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

    //FixedUpdateの最後で、入力情報をリセット
    private void ResetInputData()
    {
        for (int i = 0; i < 16; i++)
        {
            inputList[i].isFirst = false;
            //inputList[i].isTouching = false;
        }
    }

    //スライドがタッチされた瞬間か返す(配列)
    public bool IsReturnSlidersFirstTouch(int[] nums)
    {
        foreach (int i in nums)
        {
            if (IsReturnSliderFirstTouch(i)) { return true; }
        }
        return false;
    }

    //スライドがタッチされた瞬間か返す
    public bool IsReturnSliderFirstTouch(int num)
    {
        return inputList[num].isFirst;
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
        return inputList[num].isTouching;
    }

    //専コン使用モードかどうか返す
    public bool IsReturnSerialMode()
    {
        return isSerialMode;
    }
}

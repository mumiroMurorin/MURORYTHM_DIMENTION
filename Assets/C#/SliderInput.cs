using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SliderInput : MonoBehaviour
{
    //���͏��
    public class InputCondition
    {
        public bool isFirst;
        public bool isTouching;
    }

    //�Ή�����L�[
    KeyCode[] KEY_CODES_16K = new KeyCode[16]
    {
        KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.F,
        KeyCode.G,KeyCode.H,KeyCode.J, KeyCode.K,
        KeyCode.L, KeyCode.Semicolon, KeyCode.Colon,KeyCode.RightBracket,
        KeyCode.Z,KeyCode.X,KeyCode.C,KeyCode.V
    };

    //�Ή�����L�[(3K)
    KeyCode[] KEY_CODES_3K = new KeyCode[16]
    {
        KeyCode.D,KeyCode.D,KeyCode.D,KeyCode.D,KeyCode.D,
        KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,
        KeyCode.K, KeyCode.K,KeyCode.K,KeyCode.K,KeyCode.K
    };

    //�Ή�����L�[(4K)
    KeyCode[] KEY_CODES_4K = new KeyCode[16]
    {
        KeyCode.D,KeyCode.D,KeyCode.D,KeyCode.D,KeyCode.D,
        KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,KeyCode.Space,
        KeyCode.K, KeyCode.K,KeyCode.K,KeyCode.K,KeyCode.K
    };

    //�Ή�����Z���T�ԍ�
    int[] SENSOR_INDEX = new int[16]
    { 0,1,2,3,
      11,10,9,8,
      12,13,14,15,
      23,22,21,20
    };

    [Header("��R���H")]
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
        //FixedUpdate�ň�����悤�ϊ�
        ConvertInputData();
    }

    private void FixedUpdate()
    {
        ResetInputData();   //���̓f�[�^(GetKeyDown)�̏�����
    }

    //Update�œ������͏���FixedUpdate�ň�����悤�ɃN���X�ɑ��
    private void ConvertInputData()
    {
        //�f�o�C�X���ڑ�����Ă���Ƃ�
        if (isSerialMode && serial.IsReturnConnectDevice())
        {
            for (int i = 0; i < 16; i++)
            {
                //���͂��ꂽ����H
                if (!inputList[i].isTouching && IsReturnTouchDevice(SENSOR_INDEX[i]))
                {
                    inputList[i].isFirst = true;
                    inputList[i].isTouching = true;
                }
                //���͂���Ă���Œ��H
                else if (IsReturnTouchDevice(SENSOR_INDEX[i]))
                {
                    inputList[i].isTouching = true;
                }
                //�Ȃɂ��˂�
                else
                {
                    inputList[i].isTouching = false;
                }
            }
        }
        //�L�[�{�[�h���̓��[�h
        else
        {
            for (int i = 0; i < 16; i++)
            {
                //���͂��ꂽ����H
                if (Input.GetKeyDown(KEY_CODES_16K[i]))
                {
                    inputList[i].isFirst = true;
                    inputList[i].isTouching = true;
                }
                //���͂���Ă���Œ��H
                else if (Input.GetKey(KEY_CODES_16K[i])) { inputList[i].isTouching = true; }
                //�Ȃɂ��˂�
                else { inputList[i].isTouching = false; }
            }
        }
    }

    //�Ǘ��Ґݒ�𔽉f
    public void SetRootOption(RootOption r)
    {
        //Debug.Log($"�y�����z:{r.isUseKinect},{r.isUseSlider},{r.slider_com_str},{r.slider_speed},{r.space_sensitivity}");
        isSerialMode = r.isUseSlider;
        serial.SetOption(r.slider_com_str, r.slider_speed);
        serial.StartSerial();
    }

    //�f�o�C�X����̓��͂�Ԃ�
    private bool IsReturnTouchDevice(int i)
    {
        string str = serial.ReturnTouchData();
        if (str == null) { return false; }
        return str[i] == '1' ? true : false;
    }

    //FixedUpdate�̍Ō�ŁA���͏������Z�b�g
    private void ResetInputData()
    {
        for (int i = 0; i < 16; i++)
        {
            inputList[i].isFirst = false;
            //inputList[i].isTouching = false;
        }
    }

    //�X���C�h���^�b�`���ꂽ�u�Ԃ��Ԃ�(�z��)
    public bool IsReturnSlidersFirstTouch(int[] nums)
    {
        foreach (int i in nums)
        {
            if (IsReturnSliderFirstTouch(i)) { return true; }
        }
        return false;
    }

    //�X���C�h���^�b�`���ꂽ�u�Ԃ��Ԃ�
    public bool IsReturnSliderFirstTouch(int num)
    {
        return inputList[num].isFirst;
    }

    //�X���C�h���^�b�`����Ă���Œ����Ԃ�(�z��)
    public bool IsReturnSlidersTouching(int[] nums)
    {
        foreach (int i in nums)
        {
            if (IsReturnSliderTouching(i)) { return true; }
        }
        return false;
    }

    //�X���C�h���^�b�`����Ă���Œ����Ԃ�
    public bool IsReturnSliderTouching(int num)
    {
        return inputList[num].isTouching;
    }

    //��R���g�p���[�h���ǂ����Ԃ�
    public bool IsReturnSerialMode()
    {
        return isSerialMode;
    }
}

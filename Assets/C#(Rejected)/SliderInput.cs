using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

public class SliderInput : MonoBehaviour
{
    //�Ή�����L�[
    KeyCode[] KEY_CODES_16K = new KeyCode[16]
    {
        KeyCode.X,KeyCode.W,KeyCode.V,KeyCode.U,
        KeyCode.M,KeyCode.N,KeyCode.O, KeyCode.P,
        KeyCode.L, KeyCode.K, KeyCode.J,KeyCode.I,
        KeyCode.A,KeyCode.B,KeyCode.C,KeyCode.D
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
        //FixedUpdate�ň�����悤�ϊ�
        ConvertInputData();
    }

    //Update�œ������͏���FixedUpdate�ň�����悤�ɃN���X�ɑ��
    private void ConvertInputData()
    {
        //�f�o�C�X���ڑ�����Ă���Ƃ�
        if (isSerialMode && serial.IsReturnConnectDevice())
        {
            for (int i = 0; i < 16; i++)
            {
                if (inputs[i].Value == IsReturnTouchDevice(SENSOR_INDEX[i])) { continue; }
                inputs[i].Value = IsReturnTouchDevice(SENSOR_INDEX[i]);
            }
        }
        //�L�[�{�[�h���̓��[�h
        else
        {
            for (int i = 0; i < 16; i++)
            {
                if (inputs[i].Value == Input.GetKey(KEY_CODES_16K[i])) { continue; }
                inputs[i].Value = Input.GetKey(KEY_CODES_16K[i]);
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

    //�X���C�h���^�b�`���ꂽ�u�Ԃ��Ԃ�(�z��)
    public IReadOnlyReactiveProperty<bool> GetSliderReactiveProperty(int index)
    {
        return InputsReactiveProperty[index];
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
        return inputs[num].Value;
    }

    //��R���g�p���[�h���ǂ����Ԃ�
    public bool IsReturnSerialMode()
    {
        return isSerialMode;
    }
}

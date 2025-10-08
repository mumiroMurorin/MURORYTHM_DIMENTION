using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using VContainer;

public class SliderInputSetter : MonoBehaviour
{
    [Header("対応する入力(必ず長さ16に)")]
    [SerializeField] KeyCodeConfig[] configs;

    ISliderInputSetter sliderInputSetter;

    // スライダー(キーボード) → ゲーム内入力
    List<Dictionary<KeyCode, int>> keyCodeToSliderIndexList = new List<Dictionary<KeyCode, int>>();

    [Inject]
    public void Inject(ISliderInputSetter inputSetter)
    {
        sliderInputSetter = inputSetter;
    }

    private void Awake()
    {
        sliderInputSetter?.Initialize();
    }

    private void Start()
    {
        CheckSliderKeyCodes();
    }

    void Update()
    {
        // 全てのキー入力を監視
        foreach(var keyCodeToSliderIndex in keyCodeToSliderIndexList)
        {
            foreach (var pair in keyCodeToSliderIndex)
            {
                sliderInputSetter?.SetSliderInput(pair.Value, Input.GetKey(pair.Key));
            }
        }
    }

    private void CheckSliderKeyCodes()
    {
        if (configs == null) { return; }

        keyCodeToSliderIndexList.Clear();
        keyCodeToSliderIndexList = new List<Dictionary<KeyCode, int>>();

        foreach (var config in configs)
        {
            if (config.KeyCodes.Length != 16) 
            {
                Debug.LogError("【入力】対応するキーの数が16でありません");
                continue;
            }

            if (!config.IsActive) { continue; }

            var dictionary = new Dictionary<KeyCode, int>();
            keyCodeToSliderIndexList.Add(dictionary);

            for (int i = 0; i < 16; i++)
            {
                dictionary.Add(config.KeyCodes[i], i);
            }
        }
    }

    [System.Serializable]
    public class KeyCodeConfig
    {
        [SerializeField] string configName;

        [Header("対応する入力(必ず長さ16に)")]
        [SerializeField] KeyCode[] keyCodes;

        [SerializeField] bool isActive;

        public KeyCode[] KeyCodes { get { return keyCodes; } }

        public bool IsActive { get { return isActive; } }
    }
}


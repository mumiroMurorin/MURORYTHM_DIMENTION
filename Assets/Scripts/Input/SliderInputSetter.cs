using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class SliderInputSetter : MonoBehaviour
{
    private const int SliderMaxCount = 16;

    [Header("Key bindings")]
    [SerializeField] private KeyCodeConfig[] configs;

    private ISliderInputSetter sliderInputSetter;
    private readonly List<KeyBinding> keyBindings = new List<KeyBinding>();
    private readonly bool[] sliderSwitches = new bool[SliderMaxCount];

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
        RebuildKeyBindings();
    }

    private void Update()
    {
        Array.Clear(sliderSwitches, 0, sliderSwitches.Length);

        foreach (var binding in keyBindings)
        {
            if (!binding.IsValid) { continue; }

            sliderSwitches[binding.SliderIndex] |= Input.GetKey(binding.KeyCode);
        }

        for (var i = 0; i < sliderSwitches.Length; i++)
        {
            sliderInputSetter?.SetSliderInput(i, sliderSwitches[i]);
        }

    }

    private void RebuildKeyBindings()
    {
        keyBindings.Clear();

        if (configs == null) { return; }

        foreach (var config in configs)
        {
            if (config == null || !config.IsActive) { continue; }

            config.AddBindings(keyBindings);
        }
    }

    public readonly struct KeyBinding
    {
        public readonly KeyCode KeyCode;
        public readonly int SliderIndex;

        public KeyBinding(KeyCode keyCode, int sliderIndex)
        {
            KeyCode = keyCode;
            SliderIndex = sliderIndex;
        }

        public bool IsValid => KeyCode != KeyCode.None && 0 <= SliderIndex && SliderIndex < SliderMaxCount;
    }

    [Serializable]
    public class KeyCodeConfig
    {
        [SerializeField] private string configName;

        [Header("Multiple keys per lane")]
        [SerializeField] private LaneKeyCodeConfig[] laneKeyCodes;

        [SerializeField] private bool isActive;

        public bool IsActive => isActive;

        public void AddBindings(List<KeyBinding> bindings)
        {
            if (laneKeyCodes == null || laneKeyCodes.Length == 0) { return; }

            if (laneKeyCodes.Length != SliderMaxCount)
            {
                Debug.LogWarning($"[Input] {configName}: Lane key setting count should be 16. Current: {laneKeyCodes.Length}");
            }

            var count = Mathf.Min(laneKeyCodes.Length, SliderMaxCount);
            for (var i = 0; i < count; i++)
            {
                laneKeyCodes[i]?.AddBindings(bindings, i);
            }
        }
    }

    [Serializable]
    public class LaneKeyCodeConfig
    {
        [SerializeField] private KeyCode[] keyCodes;

        public void AddBindings(List<KeyBinding> bindings, int sliderIndex)
        {
            if (keyCodes == null) { return; }

            foreach (var keyCode in keyCodes)
            {
                if (keyCode == KeyCode.None) { continue; }

                bindings.Add(new KeyBinding(keyCode, sliderIndex));
            }
        }
    }
}

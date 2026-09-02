using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class SliderInputSetter : MonoBehaviour
{
    private const int SliderMaxCount = 16;
    private const int PhysicalKeyMaxCount = 32;

    [Header("Key bindings")]
    [SerializeField] private KeyCodeConfig[] configs;

    [Header("Hot plug")]
    [SerializeField] private bool watchJoystickConnection = true;
    [SerializeField, Min(0.1f)] private float joystickPollInterval = 1f;
    [SerializeField, Min(0f)] private float joystickReconnectDelay = 0.5f;

    private ISliderInputSetter sliderInputSetter;
    private readonly List<KeyBinding> keyBindings = new List<KeyBinding>();
    private readonly bool[] sliderSwitches = new bool[SliderMaxCount];
    private string[] joystickNames = Array.Empty<string>();
    private float nextJoystickPollTime;
    private float pendingJoystickRefreshTime = -1f;

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
        joystickNames = Input.GetJoystickNames();
        RebuildKeyBindings();
    }

    private void Update()
    {
        UpdateJoystickConnection();

        Array.Clear(sliderSwitches, 0, sliderSwitches.Length);

        foreach (var binding in keyBindings)
        {
            if (!binding.IsValid) { continue; }

            sliderSwitches[binding.SliderIndex] |= Input.GetKey(binding.KeyCode);

            if (Input.GetKeyDown(binding.KeyCode))
            {
                sliderInputSetter?.NotifySliderTouchDown(binding.SliderIndex);
            }
        }

        for (var i = 0; i < sliderSwitches.Length; i++)
        {
            sliderInputSetter?.SetSliderInput(i, sliderSwitches[i]);
        }

    }

    private void UpdateJoystickConnection()
    {
        if (!watchJoystickConnection) { return; }

        var now = Time.unscaledTime;
        if (now >= nextJoystickPollTime)
        {
            nextJoystickPollTime = now + joystickPollInterval;
            var currentNames = Input.GetJoystickNames();

            if (!AreJoystickNamesEqual(joystickNames, currentNames))
            {
                joystickNames = currentNames;
                pendingJoystickRefreshTime = now + joystickReconnectDelay;
            }
        }

        if (pendingJoystickRefreshTime < 0f || now < pendingJoystickRefreshTime) { return; }

        pendingJoystickRefreshTime = -1f;
        ResetSliderInputs();
        Input.ResetInputAxes();
        RebuildKeyBindings();
        Debug.Log($"[Input] Joystick connection changed. Rebuilt slider bindings: {string.Join(", ", joystickNames)}");
    }

    private void ResetSliderInputs()
    {
        Array.Clear(sliderSwitches, 0, sliderSwitches.Length);

        for (var i = 0; i < SliderMaxCount; i++)
        {
            sliderInputSetter?.SetSliderInput(i, false);
        }
    }

    private static bool AreJoystickNamesEqual(string[] a, string[] b)
    {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null) { return false; }
        if (a.Length != b.Length) { return false; }

        for (var i = 0; i < a.Length; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) { return false; }
        }

        return true;
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

        [Header("Physical keys mapped to 16 lanes")]
        [SerializeField] private KeyCode[] physicalKeyCodes = new KeyCode[PhysicalKeyMaxCount];

        [HideInInspector]
        [SerializeField] private LaneKeyCodeConfig[] laneKeyCodes;

        [SerializeField] private bool isActive;

        public bool IsActive => isActive;

        public void AddBindings(List<KeyBinding> bindings)
        {
            if (TryAddPhysicalKeyBindings(bindings)) { return; }

            AddLegacyLaneKeyBindings(bindings);
        }

        private bool TryAddPhysicalKeyBindings(List<KeyBinding> bindings)
        {
            if (physicalKeyCodes == null || physicalKeyCodes.Length == 0) { return false; }

            if (physicalKeyCodes.Length != PhysicalKeyMaxCount)
            {
                Debug.LogWarning($"[Input] {configName}: Physical key setting count should be 32. Current: {physicalKeyCodes.Length}");
            }

            var count = Mathf.Min(physicalKeyCodes.Length, PhysicalKeyMaxCount);
            var hasBinding = false;
            for (var i = 0; i < count; i++)
            {
                var keyCode = physicalKeyCodes[i];
                if (keyCode == KeyCode.None) { continue; }

                bindings.Add(new KeyBinding(keyCode, i / 2));
                hasBinding = true;
            }

            return hasBinding;
        }

        private void AddLegacyLaneKeyBindings(List<KeyBinding> bindings)
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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using VContainer;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ConductusHidInputSetter : MonoBehaviour
{
    private const int SliderMaxCount = 16;
    private const int PhysicalKeyMaxCount = 32;

    [Header("Device")]
    [SerializeField] private bool acceptAllConductusDevices = true;
    [SerializeField] private bool resetInputWhenDeviceChanges = true;
    [SerializeField] private bool logDeviceChanges = true;

    private ISliderInputSetter sliderInputSetter;
    private readonly List<ConductusHidDevice> devices = new List<ConductusHidDevice>();
    private readonly bool[] sliderInputs = new bool[SliderMaxCount];
    private readonly bool[] previousSliderInputs = new bool[SliderMaxCount];

    [Inject]
    public void Inject(ISliderInputSetter inputSetter)
    {
        sliderInputSetter = inputSetter;
    }

    private void Awake()
    {
        ConductusHidDevice.RegisterLayout();
        sliderInputSetter?.Initialize();
    }

    private void OnEnable()
    {
        RefreshDevices();
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        ResetSliderInputs();
    }

    private void Update()
    {
        Array.Copy(sliderInputs, previousSliderInputs, SliderMaxCount);
        Array.Clear(sliderInputs, 0, sliderInputs.Length);

        foreach (var device in devices)
        {
            if (device == null || !device.added) { continue; }
            if (!acceptAllConductusDevices && device != ConductusHidDevice.current) { continue; }

            ApplyDeviceInput(device);
        }

        for (var i = 0; i < SliderMaxCount; i++)
        {
            sliderInputSetter?.SetSliderInput(i, sliderInputs[i]);

            if (sliderInputs[i] && !previousSliderInputs[i])
            {
                sliderInputSetter?.NotifySliderTouchDown(i);
            }
        }
    }

    private void ApplyDeviceInput(ConductusHidDevice device)
    {
        for (var physicalIndex = 0; physicalIndex < PhysicalKeyMaxCount; physicalIndex++)
        {
            var button = device.GetPhysicalButton(physicalIndex);
            if (button == null || !button.isPressed) { continue; }

            sliderInputs[physicalIndex / 2] = true;
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        var conductusDevice = device as ConductusHidDevice;
        if (conductusDevice == null) { return; }

        if (logDeviceChanges)
        {
            Debug.Log($"[Input] Conductus HID device {change}: {conductusDevice.displayName}");
        }

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
                if (!devices.Contains(conductusDevice))
                {
                    devices.Add(conductusDevice);
                }
                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                devices.Remove(conductusDevice);
                break;
        }

        if (resetInputWhenDeviceChanges)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Reconnected:
                    ResetSliderInputs();
                    break;
            }
        }
    }

    private void RefreshDevices()
    {
        devices.Clear();

        foreach (var device in InputSystem.devices)
        {
            if (device is ConductusHidDevice conductusDevice)
            {
                devices.Add(conductusDevice);
            }
        }
    }

    private void ResetSliderInputs()
    {
        Array.Clear(sliderInputs, 0, sliderInputs.Length);
        Array.Clear(previousSliderInputs, 0, previousSliderInputs.Length);

        for (var i = 0; i < SliderMaxCount; i++)
        {
            sliderInputSetter?.SetSliderInput(i, false);
        }
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[InputControlLayout(stateType = typeof(ConductusHidInputState), displayName = "Conductus Controller")]
public class ConductusHidDevice : InputDevice
{
    private static bool isLayoutRegistered;

    public static ConductusHidDevice current { get; private set; }

    public ButtonControl[] physicalButtons { get; private set; }

#if UNITY_EDITOR
    static ConductusHidDevice()
    {
        RegisterLayout();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void RegisterLayout()
    {
        if (isLayoutRegistered) { return; }

        InputSystem.RegisterLayout<ConductusHidDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("Conductus")
                .WithProduct("Conductus.*"));

        InputSystem.RegisterLayout<ConductusHidDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithCapability("vendorId", 0x239A)
                .WithCapability("productId", 0x80F4));

        isLayoutRegistered = true;
    }

    protected override void FinishSetup()
    {
        base.FinishSetup();

        physicalButtons = new ButtonControl[32];
        for (var i = 0; i < physicalButtons.Length; i++)
        {
            physicalButtons[i] = GetChildControl<ButtonControl>($"physical{i}");
        }
    }

    public override void MakeCurrent()
    {
        base.MakeCurrent();
        current = this;
    }

    protected override void OnRemoved()
    {
        base.OnRemoved();

        if (current == this)
        {
            current = null;
        }
    }

    public ButtonControl GetPhysicalButton(int physicalIndex)
    {
        if (physicalButtons == null || physicalIndex < 0 || physicalIndex >= physicalButtons.Length)
        {
            return null;
        }

        return physicalButtons[physicalIndex];
    }
}

[StructLayout(LayoutKind.Explicit, Size = 5)]
public struct ConductusHidInputState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('H', 'I', 'D');

    [FieldOffset(0)]
    public byte reportId;

    [InputControl(name = "physical0", layout = "Button", bit = 0)]
    [InputControl(name = "physical1", layout = "Button", bit = 1)]
    [InputControl(name = "physical2", layout = "Button", bit = 2)]
    [InputControl(name = "physical3", layout = "Button", bit = 3)]
    [InputControl(name = "physical4", layout = "Button", bit = 4)]
    [InputControl(name = "physical5", layout = "Button", bit = 5)]
    [InputControl(name = "physical6", layout = "Button", bit = 6)]
    [InputControl(name = "physical7", layout = "Button", bit = 7)]
    [InputControl(name = "physical8", layout = "Button", bit = 8)]
    [InputControl(name = "physical9", layout = "Button", bit = 9)]
    [InputControl(name = "physical10", layout = "Button", bit = 10)]
    [InputControl(name = "physical11", layout = "Button", bit = 11)]
    [InputControl(name = "physical12", layout = "Button", bit = 12)]
    [InputControl(name = "physical13", layout = "Button", bit = 13)]
    [InputControl(name = "physical14", layout = "Button", bit = 14)]
    [InputControl(name = "physical15", layout = "Button", bit = 15)]
    [InputControl(name = "physical16", layout = "Button", bit = 16)]
    [InputControl(name = "physical17", layout = "Button", bit = 17)]
    [InputControl(name = "physical18", layout = "Button", bit = 18)]
    [InputControl(name = "physical19", layout = "Button", bit = 19)]
    [InputControl(name = "physical20", layout = "Button", bit = 20)]
    [InputControl(name = "physical21", layout = "Button", bit = 21)]
    [InputControl(name = "physical22", layout = "Button", bit = 22)]
    [InputControl(name = "physical23", layout = "Button", bit = 23)]
    [InputControl(name = "physical24", layout = "Button", bit = 24)]
    [InputControl(name = "physical25", layout = "Button", bit = 25)]
    [InputControl(name = "physical26", layout = "Button", bit = 26)]
    [InputControl(name = "physical27", layout = "Button", bit = 27)]
    [InputControl(name = "physical28", layout = "Button", bit = 28)]
    [InputControl(name = "physical29", layout = "Button", bit = 29)]
    [InputControl(name = "physical30", layout = "Button", bit = 30)]
    [InputControl(name = "physical31", layout = "Button", bit = 31)]
    [FieldOffset(1)]
    public uint buttons;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class ControllerPositionSettingView : MonoBehaviour
{
    [SerializeField] TMP_InputField inputFieldX;
    [SerializeField] TMP_InputField inputFieldY;
    [SerializeField] TMP_InputField inputFieldZ;
    [SerializeField] Button setRightPositionButton;
    [SerializeField] Button setLeftPositionButton;

    public Action<Vector3> OnChangeValueListner { get; set; }

    public Action OnPushSetRightPositionButtonListner { get; set; }

    public Action OnPushSetLeftPositionButtonListner { get; set; }

    private void Start()
    {
        inputFieldX?.onEndEdit.AddListener((str) => OnChangeValue());
        inputFieldY?.onEndEdit.AddListener((str) => OnChangeValue());
        inputFieldZ?.onEndEdit.AddListener((str) => OnChangeValue());

        setRightPositionButton?.onClick.AddListener(OnPushSetRightPositionButton);
        setLeftPositionButton?.onClick.AddListener(OnPushSetLeftPositionButton);
    }

    public void OnChangePosition(Vector3 pos)
    {
        inputFieldX.text = pos.x.ToString();
        inputFieldY.text = pos.y.ToString();
        inputFieldZ.text = pos.z.ToString();
    }

    public void OnChangeValue()
    {
        if(!float.TryParse(inputFieldX.text,out float x)) { return; }
        if(!float.TryParse(inputFieldY.text,out float y)) { return; }
        if(!float.TryParse(inputFieldZ.text,out float z)) { return; }

        OnChangeValueListner?.Invoke(new Vector3(x, y, z));
    }

    private void OnPushSetRightPositionButton()
    {
        OnPushSetRightPositionButtonListner?.Invoke();
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnPushSetLeftPositionButton()
    {
        OnPushSetLeftPositionButtonListner?.Invoke();
        EventSystem.current.SetSelectedGameObject(null);
    }
}

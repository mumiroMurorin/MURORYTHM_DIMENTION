using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class ButtonView : MonoBehaviour
{
    [SerializeField] Button button;

    public event Action OnPushButtonListner;

    void Start()
    {
        button?.onClick.AddListener(OnPushButton);
    }

    private void OnPushButton()
    {
        OnPushButtonListner?.Invoke();
        EventSystem.current.SetSelectedGameObject(null);
    }
}

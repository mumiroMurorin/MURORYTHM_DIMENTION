using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace ChartEditor
{
    public class ChangeEditModeButton : MonoBehaviour
    {
        [SerializeField] Button button;

        public Action OnClickedListner;

        private void Start()
        {
            button.onClick.AddListener(OnClicked);
        }

        public void OnChangeEditMode(bool isInteracted)
        {
            button.interactable = !isInteracted;
        }

        public void OnClicked()
        {
            OnClickedListner?.Invoke();
        }

    }
}

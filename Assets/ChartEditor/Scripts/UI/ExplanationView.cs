using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace ChartEditor
{
    public class ExplanationView : MonoBehaviour
    {
        [SerializeField] Button closeButton;

        public Action OnClickCloseButtonListner;

        public void OnChangeEditMode(EditMode editMode)
        {
            this.gameObject.SetActive(editMode == EditMode.Explanation);
        }

        public void OnClickCloseButton()
        {
            OnClickCloseButtonListner?.Invoke();
        }
    }

}
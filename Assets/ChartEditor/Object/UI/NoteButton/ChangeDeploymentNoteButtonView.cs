using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace ChartEditor
{
    public class ChangeDeploymentNoteButtonView : MonoBehaviour
    {
        [SerializeField] Button button;

        public Action OnClickedListner;

        public void OnChangeDeploymentNote(bool isInteracted)
        {
            button.interactable = !isInteracted;
        }

        public void OnClicked()
        {
            OnClickedListner?.Invoke();
        }
    }

}

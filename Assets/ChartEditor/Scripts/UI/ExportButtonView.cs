using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    public class ExportButtonView : MonoBehaviour
    {
        [SerializeField] Button button;

        public System.Action OnClickedListner { get; set; }

        public void OnChangePlayMode(PlayMode playMode)
        {
            button.interactable = playMode != PlayMode.Play;
        }

        public void OnClicked()
        {
            OnClickedListner.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    public class ChartShortenButtonView : MonoBehaviour
    {
        [SerializeField] Button button;

        public System.Action OnClickedListner { get; set; }

        public void OnChangePlayMode(PlayMode playMode)
        {
            button.interactable = playMode != PlayMode.Play;
        }

        public void OnClicked()
        {
            OnClickedListner?.Invoke();
        }
    }

}

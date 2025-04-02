using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    public class MusicBrowseButtonView : MonoBehaviour
    {
        [SerializeField] Button button;

        public System.Action OnClickedListner { get; }

        public void OnChangePlayMode(PlayMode playMode)
        {
            button.interactable = playMode != PlayMode.Play;
        }

        public void OnClicked()
        {
            OnClickedListner.Invoke();
        }

    }

}

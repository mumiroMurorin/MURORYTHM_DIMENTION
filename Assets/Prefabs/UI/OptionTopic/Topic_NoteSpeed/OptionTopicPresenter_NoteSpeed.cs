using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_NoteSpeed : MonoBehaviour, IOptionTopicPresenter
    {
        [SerializeField] OptionTopicView_NoteSpeed view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.NoteSpeed
                .Subscribe(view.OnChangeSpeed)
                .AddTo(this.gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {

        }
    }

}

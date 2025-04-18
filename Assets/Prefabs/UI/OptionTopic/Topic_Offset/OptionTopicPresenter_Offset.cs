using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_Offset : MonoBehaviour, IOptionTopicPresenter
    {
        [SerializeField] OptionTopicView_Offset view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.OffsetMs
                .Subscribe(_ => view.OnChangeOffset(optionGetter.OffsetDisplay))
                .AddTo(this.gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {

        }
    }

}

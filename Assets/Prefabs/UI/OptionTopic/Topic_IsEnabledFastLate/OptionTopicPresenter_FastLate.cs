using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_FastLate : MonoBehaviour, IOptionTopicPresenter
    {
        [SerializeField] OptionTopicView_FastLate view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.IsEnabledFastLate
                .Subscribe(_ => view.OnChangeEnabledFastLate(optionGetter.EnabledFastLateDisplay))
                .AddTo(this.gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {

        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_InfoSub : MonoBehaviour, IOptionTopicPresenter
    {
        [SerializeField] OptionTopicView_InfoSub view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.SubInfo
                .Subscribe(_ => view.OnChangeSubInfo(optionGetter.SubInfoDisplay))
                .AddTo(this.gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {

        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_InfoMain : MonoBehaviour, IOptionTopicPresenter
    {
        [SerializeField] OptionTopicView_InfoMain view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.MainInfo
                .Subscribe(_ => view.OnChangeMainInfo(optionGetter.MainInfoDisplay))
                .AddTo(this.gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {

        }
    }

}

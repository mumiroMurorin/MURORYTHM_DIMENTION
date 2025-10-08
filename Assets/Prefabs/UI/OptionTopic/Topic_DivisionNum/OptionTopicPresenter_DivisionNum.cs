using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_DivisionNum : MonoBehaviour, IOptionTopicPresenter
    {
        [SerializeField] OptionTopicView_DivisionNum view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.GroundDivisionNum
                .Subscribe(_ => view.OnChangeDivisionNum(optionGetter.GroundDivisionNumDisplay))
                .AddTo(this.gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {

        }
    }

}

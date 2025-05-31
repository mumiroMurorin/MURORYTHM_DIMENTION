using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_JudgementSEVolume : MonoBehaviour, IOptionTopicPresenter
    {
        [SerializeField] OptionTopicView_JudgementSEVolume view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.JudgementSEVolume
                .Subscribe(_ => view.OnChangeVolume(optionGetter.JudgementSEVolumeDisplay))
                .AddTo(this.gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {

        }
    }

}

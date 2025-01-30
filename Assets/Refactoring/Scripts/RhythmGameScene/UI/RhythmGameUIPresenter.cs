using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace Refactoring
{
    public class RhythmGameUIPresenter : MonoBehaviour
    {
        [SerializeField] Combo_View combo_view;

        IScoreGetter scoreGetter_model;

        [Inject]
        public void Constructor(IScoreGetter scoreGetter)
        {
            scoreGetter_model = scoreGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // ÉRÉìÉ{êî
            scoreGetter_model.Combo
                .Subscribe(combo_view.OnChangeCombo)
                .AddTo(this.gameObject);
        }
    }

}

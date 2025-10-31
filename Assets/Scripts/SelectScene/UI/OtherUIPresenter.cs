using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace UIInRhythmGameScene
{
    public class OtherUIPresenter : MonoBehaviour
    {
        [SerializeField] MenuCircleView menuCircle_view;
        [SerializeField] SerializeInterface<TransitionerInSelectScene.IPhaseStatusGetterInSelectScene> phaseGetter;

        [Inject] IMusicDataListGetter musicDataListGetter;

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 難易度変更
            musicDataListGetter?.Difficulty
                .Subscribe(menuCircle_view.OnChangeDifficulty)
                .AddTo(this.gameObject);

            // フェーズ変更
            phaseGetter?.Value?.PhaseStatus
                .Subscribe(menuCircle_view.OnChangePhase)
                .AddTo(this.gameObject);
        }
    }

}
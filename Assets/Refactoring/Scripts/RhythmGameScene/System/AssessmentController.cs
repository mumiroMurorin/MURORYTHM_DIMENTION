using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System;

namespace Refactoring
{
    public class AssessmentController : MonoBehaviour, IAssessmentController
    {
        [Header("終了演出")]
        [SerializeField] List<SerializeInterface<IAssessmentPlayer>> players;

        IScoreGetter scoreGetter;

        [Inject]
        public void Constructor(IScoreGetter scoreGetter)
        {
            this.scoreGetter = scoreGetter;
        }

        void IAssessmentController.PlayAnimation(Action callback)
        {
            foreach (var player in players)
            {
                if (player.Value.ConditionChecker(scoreGetter.CurrentComboRank.Value))
                {
                    player.Value.PlayAnimation(callback);
                    return;
                }
            }

            Debug.LogWarning($"【System】ステータスに対する評価アニメーションがありません: {scoreGetter.CurrentComboRank.Value}");
        }
    }

}

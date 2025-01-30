using System.Collections;
using System.Collections.Generic;
using VContainer;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class InteractNoteEffectController : MonoBehaviour
    {
        [SerializeField] List<InteractNoteEffectSpawner> spawners;

        IScoreGetter scoreGetter;

        [Inject]
        public void Constructor(IScoreGetter scoreGetter)
        {
            this.scoreGetter = scoreGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // �L�^���Ď��A��������G�t�F�N�g�𔭐�������
            scoreGetter.NoteJudgementDatas
                .ObserveAdd()
                .Subscribe(value => SpawnEffect(value.Value))
                .AddTo(this.gameObject);
        }

        private void SpawnEffect(NoteJudgementData judgementData)
        {
            foreach(InteractNoteEffectSpawner spawner in spawners)
            {
                if (spawner.ConditionChecker(judgementData))
                {
                    spawner.Spawn(judgementData);
                    return;
                }
            }

        }
    }

}

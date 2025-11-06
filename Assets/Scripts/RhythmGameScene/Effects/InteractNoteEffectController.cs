using System.Collections;
using System.Collections.Generic;
using VContainer;
using UnityEngine;
using UniRx;

public class InteractNoteEffectController : MonoBehaviour
{
    [SerializeField] SymphonyType symphonyType;
    [SerializeField] List<InteractNoteEffectSpawner> spawners;

    [Inject] IScoreGetter scoreGetter;
    [Inject] IMusicDataGetter musicDataGetter;

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        if (this.symphonyType != musicDataGetter.Music.Value.SymphonyType) { return; }

        // 記録を監視、増え次第エフェクトを発生させる
        scoreGetter.NoteJudgementDatas
            .ObserveAdd()
            .Subscribe(value => SpawnEffect(value.Value))
            .AddTo(this.gameObject);
    }

    private void SpawnEffect(NoteJudgementData judgementData)
    {
        foreach (InteractNoteEffectSpawner spawner in spawners)
        {
            if (spawner.ConditionChecker(judgementData))
            {
                spawner.Spawn(judgementData);
            }
        }

    }
}


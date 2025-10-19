using System.Collections;
using System.Collections.Generic;
using VContainer;
using UnityEngine;
using UniRx;

public class JudgementEffectController : MonoBehaviour
{
    [SerializeField] List<JudgementEffectSpawner> spawners;

    IScoreGetter scoreGetter;
    IOptionGetter optionGetter;

    [Inject]
    public void Constructor(IScoreGetter scoreGetter, IOptionGetter optionGetter)
    {
        this.scoreGetter = scoreGetter;
        this.optionGetter = optionGetter;
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        // 記録を監視、増え次第エフェクトを発生させる
        scoreGetter.NoteJudgementDatas
            .ObserveAdd()
            .Subscribe(value => SpawnEffect(value.Value))
            .AddTo(this.gameObject);
    }

    private void SpawnEffect(NoteJudgementData judgementData)
    {
        judgementData.IsEnabledFastLate = optionGetter.IsEnabledFastLate.Value;

        foreach (JudgementEffectSpawner spawner in spawners)
        {
            if (spawner.ConditionChecker(judgementData))
            {
                spawner.Spawn(judgementData);
                return;
            }
        }

    }
}


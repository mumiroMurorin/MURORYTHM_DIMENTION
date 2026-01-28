using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class JudgementEffectSpawner : MonoBehaviour
{
    [SerializeField] int poolSize = 20;
    [SerializeField] protected Transform parent;
    [SerializeField] GameObject effectPrefab;

    protected Stack<IJudgementEffectController> effectPool;

    private void Start()
    {
        // プールの初期化
        effectPool = new Stack<IJudgementEffectController>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            effectPool.Push(Instantiate());
        }
    }

    /// <summary>
    /// 判定の出現に適しているか判定する
    /// </summary>
    /// <returns></returns>
    public abstract bool ConditionChecker(NoteJudgementData judgementData);

    protected virtual IJudgementEffectController Instantiate()
    {
        var obj = Instantiate(effectPrefab, parent);

        if (!obj.TryGetComponent(out IJudgementEffectController effect)) { return null; }
        return effect;
    }

    /// <summary>
    /// 役目を終えたエフェクトを収容
    /// </summary>
    /// <param name="controller"></param>
    private void ReturnToPool(IJudgementEffectController effect)
    {
        effectPool.Push(effect);
    }

    public IJudgementEffectController Spawn(NoteJudgementData judgementData)
    {
        // 一旦無かったら返す
        if (effectPool.Count <= 0) { return null; }

        var effect = effectPool.Pop();

        effect.SetEffect(judgementData.Judgement, ReturnToPool, judgementData.IsEnabledFastLate ? judgementData.TimingError : 0f);
        effect.SetTransform(CalcSpawnPos(judgementData), CalcSpawnRotate(judgementData));
        effect.Play();

        return effect;
    }

    protected abstract Vector3 CalcSpawnPos(NoteJudgementData judgementData);

    protected abstract Quaternion CalcSpawnRotate(NoteJudgementData judgementData);
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractNoteEffectSpawner : MonoBehaviour
{
    [SerializeField] int poolSize = 20;
    [SerializeField] protected GameObject effectPrefab;
    [SerializeField] protected Transform parent;

    protected Stack<IInteractNoteEffectController> effectPool;

    private void Start()
    {
        // プールの初期化
        effectPool = new Stack<IInteractNoteEffectController>(poolSize);

        for(int i = 0; i < poolSize; i++)
        {
            effectPool.Push(Instantiate());
        }
    }

    /// <summary>
    /// インスタンス化してGetComponentしたinterfaceを返す
    /// </summary>
    /// <returns></returns>
    protected virtual IInteractNoteEffectController Instantiate()
    {
        var obj = Instantiate(effectPrefab, parent);

        if (!obj.TryGetComponent(out IInteractNoteEffectController effect)) { return null; }
        return effect;
    }

    /// <summary>
    /// 役目を終えたエフェクトを収容
    /// </summary>
    /// <param name="controller"></param>
    private void ReturnToPool(IInteractNoteEffectController controller)
    {
        effectPool.Push(controller);
    }

    /// <summary>
    /// 評価アニメーションの出現に適しているか判定する
    /// </summary>
    /// <returns></returns>
    public abstract bool ConditionChecker(NoteJudgementData judgementData);

    public IInteractNoteEffectController Spawn(NoteJudgementData judgementData)
    {
        var effect = effectPool.Pop();

        // 一旦無かったら返す
        if (effect == null) { return null; }
        
        effect.SetTransform(CalcSpawnPos(judgementData), CalcSpawnRotate(judgementData));
        effect.SetEffect(judgementData.NoteData, ReturnToPool);
        effect.Play();

        return effect;
    }

    protected abstract Vector3 CalcSpawnPos(NoteJudgementData judgementData);

    protected abstract Quaternion CalcSpawnRotate(NoteJudgementData judgementData);
}


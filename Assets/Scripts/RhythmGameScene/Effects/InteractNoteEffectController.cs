using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class InteractNoteEffectController : MonoBehaviour, IInteractNoteEffectController
{
    [SerializeField] JudgementToEffect[] effects;

    private void Start()
    {
        this.gameObject.SetActive(false);
    }

    public void SetEffect(INoteData noteData, Judgement judgement, Action<IInteractNoteEffectController> returnToPool)
    {
        bool isExist = false;
        foreach (var effect in effects)
        {
            bool applicable = effect.CheckCondition(judgement);
            isExist |= applicable;

            effect.Effect.gameObject.SetActive(applicable);

            if (applicable) 
            { 
                effect.Effect.SetEffect(noteData, () => {
                    this.gameObject.SetActive(false);
                    returnToPool(this);
                });
            }
        }

        // 判定に伴うエフェクトが存在しなかった場合
        if (!isExist)
        {
            this.gameObject.SetActive(false);
            returnToPool(this);
        }
    }

  　public void SetTransform(Vector3 pos, Quaternion rotation)
    {
        this.gameObject.transform.position = pos;
        this.gameObject.transform.rotation = rotation;
    }

    public void Play()
    {
        this.gameObject.SetActive(true);
        foreach (var effect in effects)
        {
            // 再生されるエフェクトはSetEffect内でアクティブになってるはず
            if (effect.Effect == null) { continue; }
            if (!effect.Effect.gameObject.activeInHierarchy) { continue; }
            effect.Effect.Play();
        }

        AfterPlay();
    }

    protected virtual void AfterPlay() { }

    [System.Serializable]
    class JudgementToEffect
    {
        [SerializeField] Judgement judgement;
        [SerializeField] InteractNoteEffect effect;
        
        public bool CheckCondition(Judgement judgement)
        {
            return this.judgement == judgement;
        }

        public InteractNoteEffect Effect { get { return effect; } }
    }
}

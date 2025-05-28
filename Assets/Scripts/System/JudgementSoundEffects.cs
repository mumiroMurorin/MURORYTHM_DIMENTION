using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Perfect～Goodまでの判定許容範囲をまとめたクラス
/// </summary>
[CreateAssetMenu(menuName = "ScriptableObject/JudgementSoundEffects", fileName = "JudgementSoundEffects")]
public class JudgementSoundEffects : ScriptableObject
{
    [System.Serializable]
    public class JudgementToSE
    {
        [SerializeField] Judgement judgement;
        [SerializeField] AudioClip audioClip;

        public void LoadAudioClip()
        {
            audioClip.LoadAudioData();
        }

        public AudioClip CheckAndGetAudioClip(Judgement judgement)
        {
            if(this.judgement != judgement) { return null; }
            return audioClip;
        }
    }

    [SerializeField] NoteType noteType;
    [SerializeField] JudgementToSE[] sounds;

    public void LoadSE()
    {
        foreach(var jts in sounds)
        {
            jts.LoadAudioClip();
        }
    }

    /// <summary>
    /// 指定された判定の判定音を返す
    /// </summary>
    /// <param name="judgement"></param>
    /// <returns></returns>
    public AudioClip GetAudioClip(NoteType noteType, Judgement judgement)
    {
        if(this.noteType != noteType) { return null; }

        foreach(var jts in sounds)
        {
            var clip = jts.CheckAndGetAudioClip(judgement);
            if(clip == null) { continue; }

            return clip;
        }

        return null;
    }
}
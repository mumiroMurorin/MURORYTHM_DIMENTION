using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SelectAudioCtrl : MonoBehaviour
{
    [Header("音楽フェード時間")]
    [SerializeField] private float fade_duration = 0.2f;
    [Header("おーでぃおそーす")]
    [SerializeField] private AudioSource se_source;
    [SerializeField] private AudioSource bgm_source;
    [Header("選択音")]
    [SerializeField] private AudioClip select_clip;
    [Header("楽曲確定音")]
    [SerializeField] private AudioClip musicDecision_clip;
    [Header("戻る音")]
    [SerializeField] private AudioClip back_clip;
    [Header("上げ下げ音")]
    [SerializeField] private AudioClip upDown_clip;

    //選択音を鳴らす
    public void PlaySelectAudio() { se_source.PlayOneShot(select_clip); }

    //楽曲決定音を鳴らす
    public void PlayDecisionAudio() { se_source.PlayOneShot(musicDecision_clip); }

    //戻る音を鳴らす
    public void PlayBackAudio() { se_source.PlayOneShot(back_clip); }

    //上げ下げ音を鳴らす
    public void PlayUpDownAudio() { se_source.PlayOneShot(upDown_clip); }

    //楽曲を流す
    public void PlayMusic(AudioClip clip)
    {
        bgm_source.loop = true;
        if (bgm_source.isPlaying) {
            bgm_source.DOFade(0f, fade_duration).OnComplete(() =>
            {
                bgm_source.clip = clip;
                bgm_source.DOFade(1f, fade_duration);
                bgm_source.Play();
            }); }
        else { 
            bgm_source.clip = clip;
            bgm_source.DOFade(1f, fade_duration);
            bgm_source.Play();
        }
    }
}

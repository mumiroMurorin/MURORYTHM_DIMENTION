using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SelectAudioCtrl : MonoBehaviour
{
    [Header("���y�t�F�[�h����")]
    [SerializeField] private float fade_duration = 0.2f;
    [Header("���[�ł������[��")]
    [SerializeField] private AudioSource se_source;
    [SerializeField] private AudioSource bgm_source;
    [Header("�I����")]
    [SerializeField] private AudioClip select_clip;
    [Header("�y�Ȋm�艹")]
    [SerializeField] private AudioClip musicDecision_clip;
    [Header("�߂鉹")]
    [SerializeField] private AudioClip back_clip;
    [Header("�グ������")]
    [SerializeField] private AudioClip upDown_clip;

    //�I������炷
    public void PlaySelectAudio() { se_source.PlayOneShot(select_clip); }

    //�y�Ȍ��艹��炷
    public void PlayDecisionAudio() { se_source.PlayOneShot(musicDecision_clip); }

    //�߂鉹��炷
    public void PlayBackAudio() { se_source.PlayOneShot(back_clip); }

    //�グ��������炷
    public void PlayUpDownAudio() { se_source.PlayOneShot(upDown_clip); }

    //�y�Ȃ𗬂�
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

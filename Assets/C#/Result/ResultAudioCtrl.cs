using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultAudioCtrl : MonoBehaviour
{
    [Header("BGM用")]
    [SerializeField] private AudioSource audio_source;
    [Header("SE用")]
    [SerializeField] private AudioSource se_source;

    [Header("リザルト音楽")]
    [SerializeField] private AudioClip result_clip;

    [Header("タッチ音")]
    [SerializeField] private AudioClip touch_clip;

    private bool isBGMFade;

    //リザルトBGMを流す
    public void PlayResultBGM()
    {
        if (!audio_source.isPlaying)
        {
            audio_source.clip = result_clip;
            audio_source.Play();
        }
    }

    //リザルトBGMのフェードアウト
    public void FadeOutResultBGM()
    {
        if (!isBGMFade && audio_source.isPlaying)
        {
            StartCoroutine(BGMFadeOut());
            isBGMFade = true;
        }
    }

    //タッチ音を流す
    public void PlayTouchSE()
    {
        se_source.PlayOneShot(touch_clip);
    }

    private IEnumerator BGMFadeOut()
    {
        for(int i = 0; i < 10; i++) {
            audio_source.volume -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }
}

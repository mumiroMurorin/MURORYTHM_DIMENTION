using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultAudioCtrl : MonoBehaviour
{
    [Header("BGM�p")]
    [SerializeField] private AudioSource audio_source;
    [Header("SE�p")]
    [SerializeField] private AudioSource se_source;

    [Header("���U���g���y")]
    [SerializeField] private AudioClip result_clip;

    [Header("�^�b�`��")]
    [SerializeField] private AudioClip touch_clip;

    private bool isBGMFade;

    //���U���gBGM�𗬂�
    public void PlayResultBGM()
    {
        if (!audio_source.isPlaying)
        {
            audio_source.clip = result_clip;
            audio_source.Play();
        }
    }

    //���U���gBGM�̃t�F�[�h�A�E�g
    public void FadeOutResultBGM()
    {
        if (!isBGMFade && audio_source.isPlaying)
        {
            StartCoroutine(BGMFadeOut());
            isBGMFade = true;
        }
    }

    //�^�b�`���𗬂�
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AudioCtrl : MonoBehaviour
{
    [Header("�y�ȗp")]
    [SerializeField] private AudioSource audio_source;

    private AudioClip audioClip;
    private bool isLoadcomp;

    //���y�̍Đ�
    public void PlayAudioSource()
    {
        audio_source.clip = audioClip;
        audio_source.Play();
    }

    //���y�̃��[�h
    public void AudioLoad(AudioClip clip)
    {
        audioClip = clip;
        isLoadcomp = true;
    }

    //���[�h���I��������Ԃ�
    public bool IsReturnLoadcomp()
    {
        return isLoadcomp;
    }

}

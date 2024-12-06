using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AudioCtrl : MonoBehaviour
{
    [Header("楽曲用")]
    [SerializeField] private AudioSource audio_source;

    private AudioClip audioClip;
    private bool isLoadcomp;

    //音楽の再生
    public void PlayAudioSource()
    {
        audio_source.clip = audioClip;
        audio_source.Play();
    }

    //音楽のロード
    public void AudioLoad(AudioClip clip)
    {
        audioClip = clip;
        isLoadcomp = true;
    }

    //ロードが終わったか返す
    public bool IsReturnLoadcomp()
    {
        return isLoadcomp;
    }

}

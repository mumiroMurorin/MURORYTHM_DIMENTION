using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorAudioCtrl : MonoBehaviour
{
    [Header("ソース")]
    [SerializeField] private AudioSource music;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //オーディオのセット
    public void SetAudioClip(AudioClip c)
    {
        music.clip = c;
    }

    //音楽の再生
    public void AudioPlay()
    {
        music.Play();
    }
    
    //音楽の(一時)停止
    public void AudioStop(bool isPause)
    {
        float t = music.time;
        music.Stop();
        if (isPause) { music.time = t; }
    }

    //再生時間の記憶
    public void MemoryAudioTime(float t)
    {
        music.time = t;
    }

    //音声の総時間を返す(秒)
    public float ReturnAudioFullTime()
    {
        return music.clip.length;
    }

    //音声の今の時間を返す
    public float ReturnAudioNowTime()
    {
        return music.time;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorAudioCtrl : MonoBehaviour
{
    [Header("�\�[�X")]
    [SerializeField] private AudioSource music;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //�I�[�f�B�I�̃Z�b�g
    public void SetAudioClip(AudioClip c)
    {
        music.clip = c;
    }

    //���y�̍Đ�
    public void AudioPlay()
    {
        music.Play();
    }
    
    //���y��(�ꎞ)��~
    public void AudioStop(bool isPause)
    {
        float t = music.time;
        music.Stop();
        if (isPause) { music.time = t; }
    }

    //�Đ����Ԃ̋L��
    public void MemoryAudioTime(float t)
    {
        music.time = t;
    }

    //�����̑����Ԃ�Ԃ�(�b)
    public float ReturnAudioFullTime()
    {
        return music.clip.length;
    }

    //�����̍��̎��Ԃ�Ԃ�
    public float ReturnAudioNowTime()
    {
        return music.time;
    }
}
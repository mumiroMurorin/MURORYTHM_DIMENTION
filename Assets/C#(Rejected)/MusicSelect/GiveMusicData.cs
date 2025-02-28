using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiveMusicData : MonoBehaviour
{
    private static GiveMusicData instance;

    //�Q�[���f�[�^
    MusicData data;
    DifficulityName difficulity;
    MusicRecord record;
    float note_speed;
    int note_offset;

    //�Ǘ��҃f�[�^
    RootOption rootOption;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //To���y�V�[��
    public void InitToMusicScene(MusicData md, DifficulityName diff, float speed, int offset, RootOption r)
    {
        data = md;
        note_speed = speed;
        note_offset = offset;
        difficulity = diff;
        rootOption = r;
    }

    //To���U���g�V�[��
    public void InitToResultScene(MusicRecord record)
    {
        this.record = record;

        //�X�R�A�̍X�V
        MusicRecord m = new MusicRecord
        {
            score = Mathf.Max(data.GetMusicRecord(difficulity).score, record.score),
            rank = (ScoreRank)Mathf.Max((int)data.GetMusicRecord(difficulity).rank, (int)record.rank),
            combo_rank = (ComboRank)Mathf.Max((int)data.GetMusicRecord(difficulity).combo_rank, (int)record.combo_rank),
            judge_num = record.judge_num
        };
        data.SetMusicRecord(difficulity, m);
    }

    //------------���^�[���n------------

    //MusicData��Ԃ�
    public MusicData ReturnMusicdata() { return data; }

    //���ʂ�Ԃ�
    public TextAsset GetChart() { return data.GetChart(difficulity); }

    //���y�t�@�C����Ԃ�
    public AudioClip GetMusicClip() { return data.MusicClip; }

    //�m�[�g�X�s�[�h��Ԃ�
    public float ReturnNoteSpeed() { return note_speed; }

    //�m�[�g�I�t�Z�b�g��Ԃ�
    public int ReturnNoteOffset() { return note_offset; }

    //��Փx��Ԃ�
    public DifficulityName ReturnDifficulity() { return difficulity; }

    //Record��Ԃ�
    public MusicRecord ReturnRecord() { return record; }

    //RootOption��Ԃ�
    public RootOption ReturnRootOption() { return rootOption; }

    //��Փx����F��Ԃ�
    public Color32 ReturnDifficulityColor32(DifficulityName diff)
    {
        switch (diff)
        {
            case DifficulityName.Initiate:
                return new Color32(0, 99, 21, 255);
            case DifficulityName.Fanatic:
                return new Color32(118, 118, 27, 255);
            case DifficulityName.Skyclad:
                return new Color32(121, 38, 156, 255);
            case DifficulityName.Dream:
                return new Color32(0, 0, 0, 255);
            default:
                Debug.LogWarning("�m����Փx�����Ă���:" + diff.ToString());
                return new Color32(0, 0, 0, 0);
        }
    }
}

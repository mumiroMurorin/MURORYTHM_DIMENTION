using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiveMusicData : MonoBehaviour
{
    private static GiveMusicData instance;

    //ゲームデータ
    MusicData data;
    DifficulityName difficulity;
    MusicRecord record;
    float note_speed;
    int note_offset;

    //管理者データ
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

    //To音楽シーン
    public void InitToMusicScene(MusicData md, DifficulityName diff, float speed, int offset, RootOption r)
    {
        data = md;
        note_speed = speed;
        note_offset = offset;
        difficulity = diff;
        rootOption = r;
    }

    //Toリザルトシーン
    public void InitToResultScene(MusicRecord record)
    {
        this.record = record;

        //スコアの更新
        MusicRecord m = new MusicRecord
        {
            score = Mathf.Max(data.GetMusicRecord(difficulity).score, record.score),
            rank = (ScoreRank)Mathf.Max((int)data.GetMusicRecord(difficulity).rank, (int)record.rank),
            combo_rank = (ComboRank)Mathf.Max((int)data.GetMusicRecord(difficulity).combo_rank, (int)record.combo_rank),
            judge_num = record.judge_num
        };
        data.SetMusicRecord(difficulity, m);
    }

    //------------リターン系------------

    //MusicDataを返す
    public MusicData ReturnMusicdata() { return data; }

    //譜面を返す
    public TextAsset GetChart() { return data.GetChart(difficulity); }

    //音楽ファイルを返す
    public AudioClip GetMusicClip() { return data.MusicClip; }

    //ノートスピードを返す
    public float ReturnNoteSpeed() { return note_speed; }

    //ノートオフセットを返す
    public int ReturnNoteOffset() { return note_offset; }

    //難易度を返す
    public DifficulityName ReturnDifficulity() { return difficulity; }

    //Recordを返す
    public MusicRecord ReturnRecord() { return record; }

    //RootOptionを返す
    public RootOption ReturnRootOption() { return rootOption; }

    //難易度から色を返す
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
                Debug.LogWarning("知らん難易度入ってきた:" + diff.ToString());
                return new Color32(0, 0, 0, 0);
        }
    }
}

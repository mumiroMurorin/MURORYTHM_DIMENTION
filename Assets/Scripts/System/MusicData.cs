using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Refactoring
{
    [CreateAssetMenu(menuName = "ScriptableObject/MusicData", fileName = "Music")]
    public class MusicData : ScriptableObject
    {
        [Header("曲名")]
        [SerializeField] private string music_name;
        public string MusicName { get { return music_name; } }

        [Header("コンポーザー")]
        [SerializeField] private string composer_name;
        public string ComposerName { get { return composer_name; } }

        [Header("サムネイル")]
        [SerializeField] private Sprite music_spr;
        public Sprite MusicSprite { get { return music_spr; } }

        [Header("テーマ画像")]
        [SerializeField] private Sprite theme_spr;
        public Sprite ThemeSprite { get { return theme_spr; } }

        [Header("音楽ファイル")]
        [SerializeField] private AudioClip clip;
        public AudioClip MusicClip { get { return clip; } }

        [Header("視聴ファイル")]
        [SerializeField] private AudioClip sample_clip;
        public AudioClip SampleClip { get { return sample_clip; } }

        [Header("難易度")]
        [SerializeField] private int[] difficulities = new int[Enum.GetNames(typeof(Difficulty)).Length];
        public int GetDifficulity(Difficulty name) { return difficulities[(int)name]; }

        [Header("譜面")]
        [SerializeField] private TextAsset[] charts = new TextAsset[Enum.GetNames(typeof(Difficulty)).Length];
        public TextAsset GetChart(Difficulty name) { return charts[(int)name]; }

        [Header("記録")]
        [SerializeField] private MusicRecord[] records = new MusicRecord[Enum.GetNames(typeof(Difficulty)).Length];
        public MusicRecord GetMusicRecord(Difficulty name) { return records[(int)name]; }
        public void SetMusicRecord(Difficulty name, MusicRecord new_record) { records[(int)name] = new_record; }
    }
}
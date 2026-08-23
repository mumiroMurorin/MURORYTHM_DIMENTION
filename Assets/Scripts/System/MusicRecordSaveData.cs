using System;
using System.Collections.Generic;

[Serializable]
public class MusicRecordSaveData
{
    public int score;
    public ComboRank comboRank;
}

[Serializable]
public class MusicRecordRankingSaveData
{
    public string chartKey;
    public List<MusicRecordSaveData> records = new List<MusicRecordSaveData>();
}

[Serializable]
public class MusicRecordSaveDatabase
{
    public List<MusicRecordRankingSaveData> records = new List<MusicRecordRankingSaveData>();
}

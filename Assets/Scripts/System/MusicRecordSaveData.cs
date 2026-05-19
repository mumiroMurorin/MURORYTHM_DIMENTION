using System;
using System.Collections.Generic;

[Serializable]
public class MusicRecordSaveData
{
    public string chartKey;
    public int score;
    public ComboRank comboRank;
}

[Serializable]
public class MusicRecordSaveDatabase
{
    public List<MusicRecordSaveData> records = new List<MusicRecordSaveData>();
}

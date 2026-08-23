using System.Collections.Generic;
using UnityEngine;

namespace UIInResultScene
{
    public class ResultRankingListView : MonoBehaviour
    {
        const int MaxDisplayCount = 5;

        [SerializeField] RankingItemView itemPrefab;
        [SerializeField] Transform contentRoot;
        [SerializeField] bool clearExistingChildren = true;

        public void ShowRanking(MusicData musicData, Difficulty difficulty, MusicRecord currentRecord)
        {
            ClearItems();

            if (musicData == null) { return; }
            if (currentRecord == null) { return; }
            if (itemPrefab == null || contentRoot == null) { return; }

            string chartKey = MusicRecordPersistence.MakeChartKey(musicData.MusicName, difficulty);
            if (!MusicRecordPersistence.TryGetSavedRecords(chartKey, out var records))
            {
                return;
            }

            CreateItems(records, currentRecord);
        }

        void CreateItems(IReadOnlyList<MusicRecordSaveData> records, MusicRecord currentRecord)
        {
            bool currentPlayApplied = false;
            int count = Mathf.Min(records.Count, MaxDisplayCount);

            for (int i = 0; i < count; i++)
            {
                var record = records[i];
                if (record == null) { continue; }

                bool isCurrentPlay = !currentPlayApplied
                    && record.score == currentRecord.Score
                    && record.comboRank == currentRecord.ComboRank;

                var item = Instantiate(itemPrefab, contentRoot);
                item.SetRankingData(i + 1, record.score, isCurrentPlay);

                if (isCurrentPlay)
                {
                    currentPlayApplied = true;
                }
            }
        }

        void ClearItems()
        {
            if (!clearExistingChildren || contentRoot == null) { return; }

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }
}

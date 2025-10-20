using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicTopicController : MonoBehaviour
{
    [Header("タイプ別トピックUIオブジェクト")]
    [SerializeField] SymphonyTypeToMusicTopic[] typeToTopic;

    MusicData currentSetData;
    Difficulty currentSetDifficulty;

    /// <summary>
    /// 楽曲データのセット
    /// </summary>
    /// <param name="data"></param>
    /// <param name="difficulty"></param>
    public void SetMusicTopic(MusicData data)
    {
        currentSetData = data;
        UpdateTopicInfo(currentSetData, currentSetDifficulty);
    }

    /// <summary>
    /// 難易度のセット
    /// </summary>
    /// <param name="b"></param>
    public void SetDifficulty(Difficulty difficulty)
    {
        currentSetDifficulty = difficulty;
        UpdateTopicInfo(currentSetData, currentSetDifficulty);
    }

    /// <summary>
    /// トピックの更新
    /// </summary>
    /// <param name="data"></param>
    /// <param name="difficulty"></param>
    private void UpdateTopicInfo(MusicData data, Difficulty difficulty)
    {
        if(data == null) { return; }

        var targetTopic = EnableAndDisableTopic(data.SymphonyType);

        targetTopic.OnSetMusicTopic(data);
        targetTopic.OnSetDifficulty(difficulty, data.GetDifficulty(difficulty));
        targetTopic.OnSetScore(data.GetMusicRecord(difficulty));
    }

    /// <summary>
    /// トピックの切り替え
    /// </summary>
    /// <param name="symphonyType"></param>
    /// <returns></returns>
    private MusicTopic EnableAndDisableTopic(SymphonyType symphonyType)
    {
        MusicTopic target = null;

        foreach(var ttt in typeToTopic)
        {
            if (ttt.CheckCondition(symphonyType)) { target = ttt.MusicTopic; }
            ttt.MusicTopic.SetObjActive(ttt.CheckCondition(symphonyType));
        }

        return target;
    } 

    /// <summary>
    /// 表示非表示切り替え
    /// </summary>
    /// <param name="b"></param>
    public void SetObjActive(bool isActive)
    {
        this.gameObject.SetActive(isActive);
    }

    [System.Serializable]
    class SymphonyTypeToMusicTopic
    {
        [SerializeField] SymphonyType symphonyType;
        [SerializeField] MusicTopic topic;

        public MusicTopic MusicTopic { get { return topic; } }

        public bool CheckCondition(SymphonyType symphonyType) { return this.symphonyType == symphonyType; }
    }
}
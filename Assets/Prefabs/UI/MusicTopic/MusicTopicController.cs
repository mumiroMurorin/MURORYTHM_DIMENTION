using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicTopicController : MonoBehaviour
{
    [Header("タイプ別トピックUIオブジェクト")]
    [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;
    [SerializeField] Transform topicParent;
    [SerializeField] float subTopicScale = 0.6f;

    MusicData currentSetData;
    Difficulty currentSetDifficulty;
    readonly Dictionary<SymphonyType, MusicTopic> typeToTopic = new Dictionary<SymphonyType, MusicTopic>();

    private void Awake()
    {
        GenerateTopicsIfNeeded();
    }

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
        if (targetTopic == null) { return; }

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
        GenerateTopicsIfNeeded();

        MusicTopic target = null;
        SymphonyTypePresentationData presentationData = symphonyTypePresentationDatabase?.Get(symphonyType);
        SymphonyType targetSymphonyType = presentationData != null ? presentationData.SymphonyType : symphonyType;

        foreach(var pair in typeToTopic)
        {
            bool isTarget = pair.Key == targetSymphonyType;
            if (isTarget) { target = pair.Value; }
            pair.Value.SetObjActive(isTarget);
        }

        if (target == null)
        {
            Debug.LogWarning($"[MusicTopicController] MusicTopic is not found: {symphonyType}");
        }

        return target;
    }

    /// <summary>
    /// 全属性分のトピックを親オブジェクト配下に生成
    /// </summary>
    private void GenerateTopicsIfNeeded()
    {
        if (typeToTopic.Count > 0) { return; }
        if (symphonyTypePresentationDatabase == null) { return; }
        if (symphonyTypePresentationDatabase.PresentationDatas == null) { return; }

        Transform parent = GetTopicParent();
        foreach (SymphonyTypePresentationData presentationData in symphonyTypePresentationDatabase.PresentationDatas)
        {
            if (presentationData == null) { continue; }
            if (typeToTopic.ContainsKey(presentationData.SymphonyType)) { continue; }

            MusicTopic topicPrefab = presentationData.MusicTopicPrefab;
            if (topicPrefab == null)
            {
                Debug.LogWarning($"[MusicTopicController] MusicTopic prefab is not set: {presentationData.SymphonyType}");
                continue;
            }

            MusicTopic topic = Instantiate(topicPrefab, parent);
            topic.transform.localPosition = Vector3.zero;
            topic.transform.localRotation = Quaternion.identity;
            topic.transform.localScale = GetGeneratedTopicScale();

            if (topic.transform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }

            topic.SetObjActive(false);
            typeToTopic.Add(presentationData.SymphonyType, topic);
        }
    }

    private Transform GetTopicParent()
    {
        if (topicParent != null) { return topicParent; }

        Transform foundTopicParent = transform.Find("TopicAxis");
        topicParent = foundTopicParent != null ? foundTopicParent : transform;
        return topicParent;
    }

    /// <summary>
    /// MainMusicTopicPar以外は既存Topicに合わせて小さく生成
    /// </summary>
    /// <returns></returns>
    private Vector3 GetGeneratedTopicScale()
    {
        if (gameObject.name == "MainMusicTopicPar")
        {
            return Vector3.one;
        }

        return new Vector3(subTopicScale, subTopicScale, 1f);
    }

    /// <summary>
    /// 表示非表示切り替え
    /// </summary>
    /// <param name="b"></param>
    public void SetObjActive(bool isActive)
    {
        this.gameObject.SetActive(isActive);
    }
}
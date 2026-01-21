using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class FadeController : MonoBehaviour
{
    [Header("通常フェード")]
    [SerializeField] FadeClass defaultFadeIn;
    [SerializeField] FadeClass defaultFadeOut;

    [Header("シンフォニータイプに対するフェード")]
    [SerializeField] SymponyTypeToFade[] symponyTypeToFadeIns;
    [SerializeField] SymponyTypeToFade[] symponyTypeToFadeOuts;

    /// <summary>
    /// 楽曲情報に依存しないフェードイン
    /// </summary>
    /// <param name="callBack"></param>
    public void FadeIn(System.Action callBack = null)
    {
        foreach (var f in symponyTypeToFadeIns)
        {
            f?.SetActive(false);
        }

        foreach (var f in symponyTypeToFadeOuts)
        {
            f?.SetActive(false);
        }

        defaultFadeIn?.SetActive(true);
        defaultFadeIn?.TimeLinePlayer.PlayAnimation(callBack);

        if (defaultFadeIn == null) { callBack?.Invoke(); }
    }

    public void FadeIn(IMusicDataGetter dataGetter, System.Action callBack = null)
    {
        if (dataGetter == null) { FadeIn(callBack); return; }
        if (dataGetter.Music == null) { FadeIn(callBack); return; }
        if (dataGetter.Music.Value == null) { FadeIn(callBack); return; }

        FadeIn(dataGetter.Music.Value.SymphonyType, callBack);
    }

    public void FadeIn(SymphonyType symphonyType, System.Action callBack = null)
    {
        bool isPlayed = false;
        foreach(var player in symponyTypeToFadeIns)
        {
            if (player.ConditionCheck(symphonyType))
            {
                player.SetActive(true);
                player.TimeLinePlayer.PlayAnimation(callBack);
                isPlayed = true;
            }
            else
            {
                player.SetActive(false);
            }
        }

        if (!isPlayed) { callBack?.Invoke(); }
    }


    /// <summary>
    /// 楽曲情報に依存しないフェードアウト
    /// </summary>
    /// <param name="callBack"></param>
    public void FadeOut(System.Action callBack = null)
    {
        foreach (var f in symponyTypeToFadeIns)
        {
            f?.SetActive(false);
        }

        foreach (var f in symponyTypeToFadeOuts)
        {
            f?.SetActive(false);
        }

        defaultFadeOut?.SetActive(true);
        defaultFadeOut?.TimeLinePlayer.PlayAnimation(callBack);

        if (defaultFadeOut == null) { callBack?.Invoke(); }
    }

    public void FadeOut(IMusicDataGetter dataGetter, System.Action callBack = null)
    {
        if (dataGetter == null) { FadeOut(callBack); return; }
        if (dataGetter.Music == null) { FadeOut(callBack); return; }
        if (dataGetter.Music.Value == null) { FadeOut(callBack); return; }

        FadeOut(dataGetter.Music.Value.SymphonyType, callBack);
    }

    public void FadeOut(SymphonyType symphonyType, System.Action callBack = null)
    {
        bool isPlayed = false;
        foreach (var player in symponyTypeToFadeOuts)
        {
            if (player.ConditionCheck(symphonyType))
            {
                player.SetActive(true);
                player.TimeLinePlayer.PlayAnimation(callBack);
                isPlayed = true;
            }
            else
            {
                player.SetActive(false);
            }
        }

        if (!isPlayed) { callBack?.Invoke(); }
    }


    [System.Serializable]
    class SymponyTypeToFade
    {
        [SerializeField] SymphonyType symphonyType;
        [SerializeField] TimelinePlayer timeLinePlayer;
        [SerializeField] GameObject obj;

        public bool ConditionCheck(SymphonyType symphonyType) { return this.symphonyType == symphonyType; }

        public ITimelinePlayer TimeLinePlayer { get { return timeLinePlayer; } }

        public void SetActive(bool isActive) { obj.SetActive(isActive); }
    }

    [System.Serializable]
    class FadeClass
    {
        [SerializeField] TimelinePlayer timeLinePlayer;
        [SerializeField] GameObject obj;

        public ITimelinePlayer TimeLinePlayer { get { return timeLinePlayer; } }

        public void SetActive(bool isActive) { obj.SetActive(isActive); }
    }
}

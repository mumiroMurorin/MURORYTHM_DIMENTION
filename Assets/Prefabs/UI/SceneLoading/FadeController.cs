using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class FadeController : MonoBehaviour
{
    IMusicDataGetter dataGetter;

    [SerializeField] SymponyTypeToFade[] symponyTypeToFadeIns;
    [SerializeField] SymponyTypeToFade[] symponyTypeToFadeOuts;

    [Inject]
    public void Constructor(IMusicDataGetter musicDataGetter)
    {
        this.dataGetter = musicDataGetter;
    }

    public void FadeIn(System.Action callBack = null)
    {
        if (dataGetter == null) { FadeIn(SymphonyType.None, callBack); return; }
        if (dataGetter.Music == null) { FadeIn(SymphonyType.None, callBack); return; }
        if (dataGetter.Music.Value == null) { FadeIn(SymphonyType.None, callBack); return; }

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

    public void FadeOut(System.Action callBack = null)
    {
        if (dataGetter == null) { FadeOut(SymphonyType.None, callBack); return; }
        if (dataGetter.Music == null) { FadeOut(SymphonyType.None, callBack); return; }
        if (dataGetter.Music.Value == null) { FadeOut(SymphonyType.None, callBack); return; }

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
}

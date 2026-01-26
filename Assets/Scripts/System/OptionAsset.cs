using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OptionAsset", menuName = "ScriptableObject/OptionAsset")]
public class OptionAsset : ScriptableObject
{
    [SerializeField] float noteSpeed = 100f;
    [SerializeField] float seVolume = 0.8f;
    [SerializeField] float bgmVolume = 0.8f;
    [SerializeField] float offset = 0;
    [SerializeField] int divisionNum = 4;
    [SerializeField] bool isEnabledFastLate = false;
    [SerializeField] InfoTypeMain mainInfo = InfoTypeMain.Combo;
    [SerializeField] InfoTypeSub subInfo = InfoTypeSub.None;

    public float NoteSpeed { get { return noteSpeed; } }
    public float SeVolume { get { return seVolume; } }
    public float BgmVolume { get { return bgmVolume; } }
    public float Offset { get { return offset; } }
    public int DivisionNum { get { return divisionNum; } }
    public bool IsEnabledFastLate { get { return isEnabledFastLate; } }
    public InfoTypeMain MainInfo { get { return mainInfo; } }
    public InfoTypeSub SubInfo { get { return subInfo; } }
}
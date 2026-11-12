using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Perfect～Goodまでの判定許容範囲をまとめたクラス
/// </summary>
[CreateAssetMenu(menuName = "ScriptableObject/JudgementWindow", fileName = "JudgementWindow")]
public class JudgementWindowObject : ScriptableObject
{
    [SerializeField] JudgementWindow judgementWindow;
    
    public JudgementWindow JudgementWindow { get { return judgementWindow; } }
}

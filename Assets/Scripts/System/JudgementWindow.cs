using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Perfect`Good‚Ü‚Å‚Ì”»’è‹–—e”ÍˆÍ‚ğ‚Ü‚Æ‚ß‚½ƒNƒ‰ƒX
/// </summary>
[CreateAssetMenu(menuName = "ScriptableObject/JudgementWindow", fileName = "JudgementWindow")]
public class JudgementWindowObject : ScriptableObject
{
    [SerializeField] JudgementWindow judgementWindow;
    
    public JudgementWindow JudgementWindow { get { return judgementWindow; } }
}

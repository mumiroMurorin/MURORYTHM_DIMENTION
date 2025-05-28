using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteSoundCollider : MonoBehaviour
    {
        [SerializeField] NoteType noteType;

        public void PlaySE()
        {
            SoundManager.Instance.PlaySE(noteType, Judgement.Perfect);
        }
    }
}

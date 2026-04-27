using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteSoundCollider : MonoBehaviour
    {
        [SerializeField] NoteObject note;

        public void PlaySE()
        {
            SoundManager.Instance.PlaySE(DeployableNoteDataUtil.ToNoteType(note.NoteData.NoteType), Judgement.Perfect);
        }
    }
}

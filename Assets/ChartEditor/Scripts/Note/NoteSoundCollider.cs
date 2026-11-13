using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteSoundCollider : MonoBehaviour
    {
        // 全ノーツ共通の音として使うノーツの種類
        const NoteType SINGLE_SOUND_NOTE_TYPE = NoteType.Touch;

        [SerializeField] NoteObject note;

        /// <param name="isSingleSound">true のとき、ノーツの種類に関係なく共通の音(タッチノーツの音)を鳴らす</param>
        public void PlaySE(bool isSingleSound = false)
        {
            NoteType noteType = isSingleSound
                ? SINGLE_SOUND_NOTE_TYPE
                : DeployableNoteDataUtil.ToNoteType(note.NoteData.NoteType);

            SoundManager.Instance.PlaySE(noteType, Judgement.Perfect);
        }
    }
}

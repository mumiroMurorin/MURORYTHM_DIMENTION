using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteSelectable : MonoBehaviour, ISelectableNoteObject
    {
        [SerializeField] NoteObject noteObject;
        [Tooltip("選択時のアウトライン色")]
        [SerializeField] private ColorSetting outlineColorOnSelect;

        public NoteObject NoteObject => noteObject;

        void ISelectableNoteObject.OnDeselect()
        {
            noteObject.OutlineColors.Remove(outlineColorOnSelect);
        }

        void ISelectableNoteObject.OnSelect()
        {
            noteObject.OutlineColors.Add(outlineColorOnSelect);
        }
    }

}

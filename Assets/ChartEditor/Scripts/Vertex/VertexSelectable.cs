using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(VertexObject))]
    public class VertexSelectable : MonoBehaviour, ISelectableVertexObject
    {
        [SerializeField] VertexObject vertexObject;
        [Tooltip("選択時のアウトライン色")]
        [SerializeField] private ColorSetting outlineColorOnSelect;

        public VertexObject VertexObject => vertexObject;

        void ISelectableVertexObject.OnDeselect()
        {
            vertexObject.OutlineColors.Remove(outlineColorOnSelect);
        }

        void ISelectableVertexObject.OnSelect()
        {
            vertexObject.OutlineColors.Add(outlineColorOnSelect);
        }
    }

}

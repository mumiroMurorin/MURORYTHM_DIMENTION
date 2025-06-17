using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(VertexObject))]
    public class VertexSelectable : MonoBehaviour, ISelectableVertexObject
    {
        [Tooltip("選択時のアウトライン色")]
        [SerializeField] private ColorSetting outlineColorOnSelect;

        VertexObject vertexObject;

        public VertexObject VertexObject => vertexObject;

        private void Start()
        {
            vertexObject = GetComponent<VertexObject>();
        }

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

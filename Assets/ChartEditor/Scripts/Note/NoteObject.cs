using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityFx.Outline;
using System;
using System.Linq;

namespace ChartEditor
{
    /// <summary>
    /// 譜面上のノーツオブジェクト
    /// </summary>
    public abstract class NoteObject : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private Renderer noteRenderer;
        [SerializeField] private Collider[] colliders;
        [SerializeField] private OutlineBehaviour outline;

        public NoteData NoteData { get; set; }

        /// <summary>
        /// 全てのコライダーの有効／無効を切り替える
        /// </summary>
        public void SetCollidersActive(bool isActive)
        {
            foreach (var col in colliders)
            {
                col.enabled = isActive;
            }
        }

        /// <summary>
        /// アウトラインカラーの設定用メソッド
        /// </summary>
        public void SetOutlineColor(Color color)
        {
            if (outline != null) { outline.OutlineColor = color; }
        }

        /// <summary>
        /// アウトラインのON/OFFを切り替える
        /// </summary>
        public void SetOutlineActive(bool active)
        {
            if (outline != null) { outline.enabled = active; }
        }

        /// <summary>
        /// このオブジェクトの削除
        /// </summary>
        public void Destroy()
        {
            Destroy(this.gameObject);
        }
    }
}

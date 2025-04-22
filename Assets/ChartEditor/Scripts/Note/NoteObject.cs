using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityFx.Outline;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    /// <summary>
    /// 譜面上のノーツオブジェクト
    /// </summary>
    public abstract class NoteObject : MonoBehaviour
    {
        [Header("Basic Settings")]
        [Tooltip("明滅時間")]
        [SerializeField] private float blinkDuration = 1f;
        [SerializeField] private Renderer noteRenderer;
        [SerializeField] private Collider[] colliders;
        [SerializeField] private OutlineBehaviour outline;

        public NoteData NoteData { get; set; }

        public Func<AddressInChart, Transform> GetParentTransformFunc;

        CancellationTokenSource cts;
        
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
        public void SetOutlineColor(Color color, bool isBlinking)
        {
            if (outline != null) { outline.OutlineColor = color; }

            // いったん点滅を止める
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            // 点滅開始
            if (isBlinking)
            {
                cts = new CancellationTokenSource();
                OutlineBlinkLoopAsync(cts.Token).Forget();
            }
        }

        /// <summary>
        /// アウトラインのON/OFFを切り替える
        /// </summary>
        public void SetOutlineActive(bool active)
        {
            if (outline != null) { outline.enabled = active; }
        }

        /// <summary>
        /// アウトラインの明滅を非同期ループで行う
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask OutlineBlinkLoopAsync(CancellationToken token)
        {
            Color baseColor = outline.OutlineColor;
            Color transparent = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Fade(baseColor, transparent, blinkDuration, token);
                    await Fade(transparent, baseColor, blinkDuration, token);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            finally
            {
                outline.OutlineColor = baseColor; // 最後に元の色に戻す
            }
        }

        /// <summary>
        /// 指定時間かけて色を補間
        /// </summary>
        private async UniTask Fade(Color from, Color to, float duration, CancellationToken token)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / duration);
                outline.OutlineColor = Color.Lerp(from, to, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
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

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
        [SerializeField] private GameObject[] colliderObjects;
        [SerializeField] private OutlineBehaviour outline;

        List<Collider> colliders;

        public ReactiveCollection<ColorSetting> OutlineColors { get; private set; } = new ReactiveCollection<ColorSetting>();

        public IDeployableNoteData NoteData { get; set; }

        public Func<AddressWithinRange, Transform> GetParentTransformFunc { get; set; }

        CancellationTokenSource cts;

        private void Awake()
        {
            colliders = new List<Collider>();
            foreach (var obj in colliderObjects)
            {
                colliders.AddRange(obj.GetComponents<Collider>());
            }
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 最初からセットしてあったら色を変える
            if (OutlineColors.Count > 0) { SetOutlineColor(OutlineColors[^1]); }

            // アウトライン色の変更
            OutlineColors?.ObserveAdd()
                .Subscribe(color => SetOutlineColor(color.Value))
                .AddTo(this.gameObject);

            OutlineColors?.ObserveRemove()
                .Subscribe(_ => {
                    if (OutlineColors.Count == 0) { SetOutlineActive(false); }
                    else { SetOutlineColor(OutlineColors[^1]); }
                })
                .AddTo(this.gameObject);
        }

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
        private void SetOutlineColor(ColorSetting outlineColor)
        {
            if (outline == null) { return; }

            // いったん点滅を止める
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            outline.OutlineColor = outlineColor.Color;
            SetOutlineActive(true);

            // 点滅開始
            if (outlineColor.IsBlinking)
            {
                cts = new CancellationTokenSource();
                OutlineBlinkLoopAsync(cts.Token).Forget();
            }
        }

        /// <summary>
        /// アウトラインのON/OFFを切り替える
        /// </summary>
        private void SetOutlineActive(bool isActive)
        {
            if (outline != null) { outline.enabled = isActive; }
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
                //outline.OutlineColor = baseColor; // 最後に元の色に戻す
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

        public void SetColor(Color color)
        {
            noteRenderer.material.color = color;
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

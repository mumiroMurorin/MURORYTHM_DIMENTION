using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityFx.Outline;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace ChartEditor
{
    public class CheckStackingCollider : MonoBehaviour, IJudgeStackingCollider
    {
        [SerializeField] List<DeploymentNoteType> warninigNoteTypes;
        [SerializeField] Color warningOutlineColor;
        [SerializeField] float blinkDuration = 0.5f;

        [SerializeField] OutlineBehaviour outline;
        [SerializeField] NoteObject noteObject;

        public DeploymentNoteType NoteType => noteObject.NoteData.NoteType;

        List<IJudgeStackingCollider> stackList = new List<IJudgeStackingCollider>();
        CancellationTokenSource cts;

        public void NotifyDisable(IJudgeStackingCollider stack)
        {
            RemoveStackList(stack);
        }

        private void AddStackList(IJudgeStackingCollider stack)
        {
            stackList.Add(stack);
            SetOutline(true);
        }

        private void RemoveStackList(IJudgeStackingCollider stack)
        {
            stackList.RemoveAll(s => s == stack);
            if (stackList.Count == 0) { SetOutline(false); }
        }

        private void SetOutline(bool isActive)
        {
            outline.enabled = isActive;

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();

            if (!isActive) { return; }

            OutlineBlinkLoopAsync(cts.Token).Forget();
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

        private void OnTriggerEnter(Collider other)
        {
            // 被り判定持ちでなければ返す
            if(!other.transform.parent.TryGetComponent(out IJudgeStackingCollider stack)) { return; }
            if (warninigNoteTypes == null) { return; }


            // 被っているノートに警告を出すか判定
            foreach (var type in warninigNoteTypes)
            {
                if(stack.NoteType == type) 
                {
                    AddStackList(stack);
                    return;
                }
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            // 被り判定持ちでなければ返す
            if (!other.transform.parent.TryGetComponent(out IJudgeStackingCollider stack)) { return; }
            if (warninigNoteTypes == null) { return; }

            // 被っているノートに警告を出すか判定
            foreach (var type in warninigNoteTypes)
            {
                if (stack.NoteType == type)
                {
                    RemoveStackList(stack);
                    return;
                }
            }
        }

        private void OnDisable()
        {
            // 重なった全ノーツにDisable通知を行う
            foreach(var stack in stackList)
            {
                stack.NotifyDisable(this);
            }
        }
    }

}
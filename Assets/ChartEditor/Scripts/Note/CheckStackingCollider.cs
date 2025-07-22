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

        [Tooltip("警告アウトライン色")]
        [SerializeField] private ColorSetting outlineColorOnStacking;

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
            if (stackList.Count == 1) { noteObject.OutlineColors.Add(outlineColorOnStacking); }
        }

        private void RemoveStackList(IJudgeStackingCollider stack)
        {
            stackList.RemoveAll(s => s == stack);
            if (stackList.Count == 0) { noteObject.OutlineColors.Remove(outlineColorOnStacking); }
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
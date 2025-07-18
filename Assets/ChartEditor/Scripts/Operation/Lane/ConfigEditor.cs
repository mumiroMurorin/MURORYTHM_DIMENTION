using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using System;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class ConfigEditor : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        ReactiveProperty<IRhythmConfigurableSubDivisionCollider> subConfigCollider = new ReactiveProperty<IRhythmConfigurableSubDivisionCollider>();
        public IReadOnlyReactiveProperty<IRhythmConfigurableSubDivisionCollider> SubDivisionConfig => subConfigCollider;

        ReactiveProperty <IRhythmConfigurableBarCollider> barConfigCollider = new ReactiveProperty<IRhythmConfigurableBarCollider>();
        public IReadOnlyReactiveProperty<IRhythmConfigurableBarCollider> BarConfig => barConfigCollider;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        private void Update()
        {
            if(dataGetter.EditNoteType.Value != EditNoteType.Ground && dataGetter.EditNoteType.Value != EditNoteType.Space) { return; }
            if(dataGetter.CurrentEditMode.Value != EditMode.EditBarConfig && dataGetter.CurrentEditMode.Value != EditMode.EditSubDivisionConfig) { return; }

            // 左クリック
            if (Input.GetMouseButtonDown(0)) { StartEditConfigOnClick(); }
        }

        /// <summary>
        /// コンフィグの編集
        /// </summary>
        private void StartEditConfigOnClick()
        {
            var subDivisionCollider = dataGetter.GetInteractableCollider<IRhythmConfigurableSubDivisionCollider>();
            var barCollider = dataGetter.GetInteractableCollider<IRhythmConfigurableBarCollider>();

            // エディットモードの変更
            if(subDivisionCollider != null)
            {
                subConfigCollider.Value = subDivisionCollider;
                dataSetter.SetEditMode(EditMode.EditingSubDivisionConfig);
            }
            else if(barCollider != null)
            {
                barConfigCollider.Value = barCollider;
                dataSetter.SetEditMode(EditMode.EditingBarConfig);
            }
        }

        public void ChangeBarConfig(BarConfig barConfig)
        {
            int barIndex = barConfigCollider.Value.BarDataGetter.BarIndex;
            var previousBarData = barConfigCollider.Value.BarDataGetter;
            var previousBarConfig = previousBarData.BarConfig;

            // コンフィグが変更できるか調べる
            if (!IsChangableBarConfig(previousBarData, barConfig)) 
            {
                Debug.Log($"【コンフィグ】対応しない分線上にノーツがあるため変更できません");
                return;
            }

            // 変更
            Record(() => {
                ChangeBarConfig(barIndex, barConfig, previousBarData);
            }, 
            // 元に戻す
            () => {
                ChangeBarConfig(barIndex, previousBarConfig, previousBarData);
            });
        }

        /// <summary>
        /// コンフィグが変更できるか調べる
        /// </summary>
        /// <returns></returns>
        private bool IsChangableBarConfig(IBarDataGetter previousBarData, BarConfig barConfig)
        {
            // 公約数分線上意外にノーツがある場合は変更できない
            int oldCount = previousBarData.SubDivisionDatas.Count;
            int newCount = barConfig.BeatCount * barConfig.DivisionNum;
            for (int i = 0; i < oldCount; i++)
            {
                // 整数かどうかの判定,分線の位置が被るかどうかの判定
                if ((newCount * i) % oldCount == 0) { continue; }

                // 被らない分線上にノートがあったら警告吐いて終了
                if (previousBarData.SubDivisionDatas[i].NoteDatas.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// コンフィグの変更、もとあったノーツを再編成する
        /// </summary>
        /// <param name="beforeConfig"></param>
        /// <param name="afterConfig"></param>
        /// <param name="barData"></param>
        private void ChangeBarConfig(int barIndex, BarConfig afterConfig, IBarDataGetter barData)
        {
            if(barData.BarConfig == afterConfig) { return; }

            // 元あった分線データを割り振る
            int newSubCount = afterConfig.BeatCount * afterConfig.DivisionNum;
            var indexToSubdivisionData = new List<IndexToSubdivisionData>();
            var beforeList = Enumerable.Range(0, barData.SubDivisionDatas.Count).Select(i => (float)i / barData.SubDivisionDatas.Count).ToList();
            var afterList = Enumerable.Range(0, newSubCount).Select(i => (float)i / newSubCount).ToList();
            beforeList = beforeList.SnapToNearest(afterList);

            // 以前のデータを保存し、ノーツを削除
            for (int i = 0; i < barData.SubDivisionDatas.Count; i++)
            {
                var subData = barData.SubDivisionDatas[i];

                int noteCount = subData.NoteDatas.Count;
                for (int j = 0; j < noteCount; j++)
                {
                    var note = subData.NoteDatas[0];
                    var index = (int)MathF.Round(beforeList[i] * newSubCount);

                    indexToSubdivisionData.Add(new IndexToSubdivisionData(index, note));
                    dataGetter.ChartData.Value.RemoveNote(note);
                }
            }

            // コンフィグ変更
            dataGetter.ChartData.Value.SetBarDataConfig(barIndex, afterConfig);

            // 以前あったノーツを割り振る
            foreach (var sub in indexToSubdivisionData)
            {
                var newNote = sub.Note;
                newNote.SetAddress(new AddressWithinRange(barData.BarIndex, sub.Index, newNote.Address.Range));
                dataGetter.ChartData.Value.AddNote(newNote);
            }
        }

        public void ChangeSubDivisionConfig(SubdivisionConfig subConfig)
        {
            int barIndex = subConfigCollider.Value.SubDivisionDataGetter.BarIndex;
            int subIndex = subConfigCollider.Value.SubDivisionDataGetter.SubDivisionIndex;
            var previousSubConfig = subConfigCollider.Value.SubDivisionDataGetter.SubConfig;

            // 変更
            Record(() => {
                dataGetter.ChartData.Value.SetSubDivisionConfig(barIndex, subIndex, subConfig);
            },
            // 元に戻す
            () => {
                dataGetter.ChartData.Value.SetSubDivisionConfig(barIndex, subIndex, previousSubConfig);
            });
        }

        public void CloseConfig()
        {
            dataSetter.SetEditMode(EditMode.None);
        }

        private class IndexToSubdivisionData
        {
            public IndexToSubdivisionData(int index, IDeployableNoteData noteData)
            {
                Index = index;
                Note = noteData;
            }

            public int Index { get; set; }

            public IDeployableNoteData Note { get; set; }
        }
    }

}
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
        [SerializeField] LaneDeployer laneController;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        IRhythmConfigurableSubDivisionCollider subConfigCollider;
        IRhythmConfigurableBarCollider barConfigCollider;
        
        public SubdivisionConfig EditingSubdivisionConfig { get; private set; }

        public BarConfig EditingBarConfig { get; private set; }

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        bool isCanceled;

        private void Update()
        {
            if(dataGetter.EditNoteType.Value != EditNoteType.Ground && dataGetter.EditNoteType.Value != EditNoteType.Space) { return; }

            var editMode = dataGetter.CurrentEditMode.Value;

            // 設定パネルを開いている間: Escでキャンセル、Enterで適用
            if (editMode == EditMode.EditingBarConfig || editMode == EditMode.EditingSubDivisionConfig)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) { CancelConfig(); }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { CloseConfig(); }
                return;
            }

            if(editMode != EditMode.EditBarConfig && editMode != EditMode.EditSubDivisionConfig) { return; }

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
                subConfigCollider = subDivisionCollider;

                // 編集中コンフィグをセット
                var lineObj = subConfigCollider.subdivisionObj;
                var subData = laneController.GetData(lineObj);
                int barIndex = subData.BarData.BarIndex;
                int subIndex = subData.SubDivisionIndex;

                if (dataGetter.ChartData.Value.BarDatas.Count <= barIndex) { return; }
                if (dataGetter.ChartData.Value.BarDatas[barIndex].SubDivisionDatas.Count <= subIndex) { return; }
                var previousSubData = dataGetter.ChartData.Value.BarDatas[barIndex].SubDivisionDatas[subIndex];
                EditingSubdivisionConfig = previousSubData.SubConfig;

                dataSetter.SetEditMode(EditMode.EditingSubDivisionConfig);
            }
            else if(barCollider != null)
            {
                barConfigCollider = barCollider;

                // 編集中コンフィグをセット
                var lineObj = barConfigCollider.barObj;
                var subData = laneController.GetData(lineObj);
                int barIndex = subData.BarData.BarIndex;

                if (dataGetter.ChartData.Value.BarDatas.Count <= barIndex) { return; }
                var previousBarData = dataGetter.ChartData.Value.BarDatas[barIndex];
                EditingBarConfig = previousBarData.BarConfig;

                dataSetter.SetEditMode(EditMode.EditingBarConfig);
            }
        }

        public void ChangeBarConfig(BarConfig barConfig)
        {
            // 不正な値(0以下など)は線の位置が計算できなくなるので受け付けない
            if (barConfig == null || !barConfig.IsValid)
            {
                MessageDialog.ShowError("小節の設定を変更できません", "拍子の分子と分割数は1以上の整数、拍子の分母は0より大きい数を入力してください。");
                return;
            }

            var lineObj = barConfigCollider.barObj;
            var subData = laneController.GetData(lineObj);
            int barIndex = subData.BarData.BarIndex;

            if (dataGetter.ChartData.Value.BarDatas.Count <= barIndex) { return; }
            var previousBarData = dataGetter.ChartData.Value.BarDatas[barIndex];
            var previousBarConfig = previousBarData.BarConfig;

            // 値が変わらないときは何もしない (Undo履歴にも積まない)
            if (previousBarConfig == barConfig) { return; }

            // コンフィグが変更できるか調べる
            if (!IsChangableBarConfig(previousBarData, barConfig))
            {
                MessageDialog.ShowError("小節の設定を変更できません", "変更後に無くなる分線の上にノーツがあります。\nノーツを移動または削除してから変更してください。");
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
            var lineObj = subConfigCollider.subdivisionObj;
            var subData = laneController.GetData(lineObj);
            int barIndex = subData.BarData.BarIndex;
            int subIndex = subData.SubDivisionIndex;

            if (dataGetter.ChartData.Value.BarDatas.Count <= barIndex) { return; }
            if (dataGetter.ChartData.Value.BarDatas[barIndex].SubDivisionDatas.Count <= subIndex) { return; }
            var previousSubData = dataGetter.ChartData.Value.BarDatas[barIndex].SubDivisionDatas[subIndex];
            var previousSubConfig = previousSubData.SubConfig;

            // 値が変わらないときは何もしない (Undo履歴にも積まない)
            if (previousSubConfig.Bpm == subConfig.Bpm && previousSubConfig.SpeedRatio == subConfig.SpeedRatio) { return; }

            // 変更
            Record(() => {
                dataGetter.ChartData.Value.SetSubDivisionConfig(barIndex, subIndex, subConfig);
            },
            // 元に戻す
            () => {
                dataGetter.ChartData.Value.SetSubDivisionConfig(barIndex, subIndex, previousSubConfig);
            });
        }

        /// <summary>
        /// 入力内容を適用して設定パネルを閉じる
        /// </summary>
        public void CloseConfig()
        {
            isCanceled = false;
            dataSetter.SetEditMode(EditMode.None);
        }

        /// <summary>
        /// 入力内容を破棄して設定パネルを閉じる
        /// </summary>
        public void CancelConfig()
        {
            isCanceled = true;
            dataSetter.SetEditMode(EditMode.None);
        }

        /// <summary>
        /// 直前に閉じた操作がキャンセルだったかを返し、フラグを戻す
        /// </summary>
        public bool ConsumeCanceled()
        {
            bool value = isCanceled;
            isCanceled = false;
            return value;
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
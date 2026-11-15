using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using System;
using VContainer;
using UnityEngine.EventSystems;
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

        /// <summary>
        /// 設定を開いている小節 (0 始まり。1小節目が 0)。分線の設定を開いているときは、その分線がある小節
        /// </summary>
        public int EditingBarIndex { get; private set; } = -1;

        /// <summary>
        /// 設定を開いている分線の、小節の中での番号 (0 始まり。小節線が 0)。小節の設定を開いているときは -1
        /// </summary>
        public int EditingSubDivisionIndex { get; private set; } = -1;

        /// <summary>
        /// 設定を開くときにクリックした、レーンの上の表示 (小節の情報、分線の BPM の欄) の当たり判定
        /// (今どの線の設定を開いているかを、枠で囲んで示すのに使う)
        /// </summary>
        public Collider EditingTargetCollider { get; private set; }

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
            // ボタンやパネルなどの画面部品の上をクリックしたときは、その裏にある線の設定を開かない
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return; }

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
                EditingBarIndex = barIndex;
                EditingSubDivisionIndex = subIndex;
                EditingTargetCollider = GetCollider(subDivisionCollider);

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
                EditingBarIndex = barIndex;
                EditingSubDivisionIndex = -1;
                EditingTargetCollider = GetCollider(barCollider);

                dataSetter.SetEditMode(EditMode.EditingBarConfig);
            }
        }

        /// <summary>
        /// クリックされた表示の、当たり判定そのもの
        /// </summary>
        private static Collider GetCollider(IInteractableCollider interactable)
        {
            var component = interactable as Component;
            return component != null ? component.GetComponent<Collider>() : null;
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

            // 元に戻す・やり直すときは、変更の前後の小節の状態(設定・分線ごとのBPMとスピード倍率・ノーツの位置)を、
            // 小節の番号を頼りにそのまま入れ直す
            // ・拍子や分割数を逆向きに変え直すだけでは、小節の途中のBPMの変わり目や、ノーツの細かい位置が戻らない
            // ・小節のデータそのものを覚えておくと、小節が消されて作り直された後 (短縮を元に戻した後など) に、
            //   もう無い小節からノーツを集めようとして、今の小節のノーツを消してしまう
            var chartData = dataGetter.ChartData.Value;
            var before = new BarSnapshot(chartData.BarDatas[barIndex]);
            BarSnapshot after = null;

            // 変更
            Record(() => {
                if (barIndex >= chartData.BarDatas.Count) { return; }

                if (after == null)
                {
                    ChangeBarConfig(barIndex, barConfig, chartData.BarDatas[barIndex]);
                    after = new BarSnapshot(chartData.BarDatas[barIndex]);
                }
                else
                {
                    after.Restore(chartData, barIndex);
                }
            },
            // 元に戻す
            () => {
                before.Restore(chartData, barIndex);
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

            // 変更は、同じ値が続くかぎり後ろの分線もまとめて書き換える。
            // 元に戻すときに「前の値で同じ変更をかけ直す」と、変更によって後ろの区間と同じ値になっていた場合に、
            // その区間まで書き換えてしまう。変更の前後の値をそのまま写しておき、写しを入れ直す
            var chartData = dataGetter.ChartData.Value;
            var beforeBpms = new List<float>();
            var beforeSpeedRatios = new List<float>();
            chartData.CopySubDivisionConfigsFrom(barIndex, subIndex, beforeBpms, beforeSpeedRatios);
            List<float> afterBpms = null;
            List<float> afterSpeedRatios = null;

            // 変更
            Record(() => {
                if (afterBpms != null)
                {
                    chartData.RestoreSubDivisionConfigsFrom(barIndex, subIndex, afterBpms, afterSpeedRatios);
                    return;
                }

                chartData.SetSubDivisionConfig(barIndex, subIndex, subConfig);

                afterBpms = new List<float>();
                afterSpeedRatios = new List<float>();
                chartData.CopySubDivisionConfigsFrom(barIndex, subIndex, afterBpms, afterSpeedRatios);

                // 写しは、実際に値が変わった分線までにする (それより後ろは、覚えておかなくてよい)
                int changedCount = 0;
                for (int i = 0; i < beforeBpms.Count && i < afterBpms.Count; i++)
                {
                    if (beforeBpms[i] != afterBpms[i] || beforeSpeedRatios[i] != afterSpeedRatios[i]) { changedCount = i + 1; }
                }

                TrimConfigCopy(beforeBpms, beforeSpeedRatios, changedCount);
                TrimConfigCopy(afterBpms, afterSpeedRatios, changedCount);
            },
            // 元に戻す
            () => {
                chartData.RestoreSubDivisionConfigsFrom(barIndex, subIndex, beforeBpms, beforeSpeedRatios);
            });
        }

        /// <summary>
        /// 分線ごとの値の写しを、先頭から count 個だけにする
        /// </summary>
        private static void TrimConfigCopy(List<float> bpms, List<float> speedRatios, int count)
        {
            if (bpms.Count > count) { bpms.RemoveRange(count, bpms.Count - count); }
            if (speedRatios.Count > count) { speedRatios.RemoveRange(count, speedRatios.Count - count); }

            bpms.TrimExcess();
            speedRatios.TrimExcess();
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
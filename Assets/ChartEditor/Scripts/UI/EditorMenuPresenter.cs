using System;
using TMPro;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    /// <summary>
    /// メニューやクイックメニューの中に表示する、設定の今の値の種類
    /// (番号はシーンに保存されているので、後から変えない)
    /// </summary>
    public enum EditorMenuValue
    {
        NoteSpeed = 1,
        ScrollSensitivity = 2,
        HitVolume = 3,
        MusicVolume = 4,
    }

    /// <summary>
    /// 画面上のメニューバー (シーンの MenuCanvas) とクイックメニュー (編集画面の左下の設定欄) の項目と、譜面エディタの機能をつなぐ
    ///
    /// メニューバーとクイックメニューは、同じ部品 (EditorMenuItem) でできていて、同じ設定を見ている。
    /// ・項目が押されたら、対応する機能を呼ぶ。機能そのものは、キー操作と同じものを使う
    ///   (保存なら ChartDataExporter、元に戻すなら InputHandler、など)
    /// ・項目の状態を今の状態に合わせる (押せるかどうか、どの値が選ばれているか、チェックが入っているか、今の値)
    ///   メニューバーの項目は一覧を開いている間だけ、クイックメニューの項目はいつも
    /// ・メニューバーは、編集画面でも全画面プレビューでも、いつも出しておく。
    ///   譜面の読み込み中と、設定のダイアログなどを開いている間は、押せなくする
    /// </summary>
    public class EditorMenuPresenter : MonoBehaviour
    {
        [Serializable]
        class ValueLabel
        {
            public EditorMenuValue value;
            public TextMeshProUGUI label;
        }

        [Header("Views")]
        [SerializeField] EditorMenuBar menuBar_view;
        [Tooltip("メニューバーの一覧にある項目")]
        [SerializeField] EditorMenuItem[] items_view;
        [Tooltip("クイックメニュー (左下の設定欄) にある項目")]
        [SerializeField] EditorMenuItem[] quickItems_view;
        [Tooltip("設定の今の値を表示する文字 (ノートスピード、スクロール感度、判定音量、楽曲音量)")]
        [SerializeField] ValueLabel[] valueLabels_view;
        [Tooltip("延長する小節数・延長分の拍子・延長分の分割数の入力欄 (メニューの「譜面」と、クイックメニューの「譜面長さ」)")]
        [SerializeField] ChartExtendSettingsView[] chartExtendSettings_view;

        [Header("Models")]
        [SerializeField] ChartDataExporter chartDataExporter_model;
        [SerializeField] ChartDataImporter chartDataImporter_model;
        [SerializeField] LaneExtender laneExtender_model;
        [SerializeField] InputHandler inputHandler_model;
        [SerializeField] NotesCopier notesCopier_model;
        [SerializeField] NoteDestroyer noteDestroyer_model;

        [Header("Presenters")]
        [Tooltip("楽曲選択・譜面の延長を呼ぶ")]
        [SerializeField] EditorUIPresenter editorUI_presenter;
        [Tooltip("全画面プレビュー・編集画面へ戻る・プレビューの作り直しを呼ぶ")]
        [SerializeField] PreviewUIPresenter previewUI_presenter;

        // スクロール感度・音量を、ボタン1回で変える量
        const float STEP_UNIT = 0.1f;

        // メニューを押せなくするモード (設定のダイアログや説明書を開いている間)
        static readonly EditMode[] disabledEditModes =
        {
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
        };

        IChartEditorDataGetter dataGetter;
        IChartEditorOptionGetter optionGetter;
        IChartEditorOptionSetter optionSetter;
        INoteSpawnDataOptionGetter noteSpawnDataOptionGetter;
        INoteSpawnDataOptionSetter noteSpawnDataOptionSetter;

        // 今表示している値 (変わったときだけ、文字を作り直す)
        int displayedExtendBarCount = -1;
        string extendChartLabel;
        float[] displayedValues;

        [Inject]
        public void Construct(
            IChartEditorDataGetter dataGetter,
            IChartEditorOptionGetter optionGetter,
            IChartEditorOptionSetter optionSetter,
            INoteSpawnDataOptionGetter noteSpawnDataOptionGetter,
            INoteSpawnDataOptionSetter noteSpawnDataOptionSetter)
        {
            this.dataGetter = dataGetter;
            this.optionGetter = optionGetter;
            this.optionSetter = optionSetter;
            this.noteSpawnDataOptionGetter = noteSpawnDataOptionGetter;
            this.noteSpawnDataOptionSetter = noteSpawnDataOptionSetter;
        }

        private void Start()
        {
            SubscribeClicks(items_view);
            SubscribeClicks(quickItems_view);

            if (menuBar_view != null) { menuBar_view.OnOpenedListener += () => RefreshItems(items_view); }

            // 入力欄を、延長の設定とつなぐ
            if (chartExtendSettings_view != null)
            {
                foreach (var view in chartExtendSettings_view)
                {
                    ChartExtendSettings.Bind(view, optionGetter, optionSetter, this);
                }
            }
        }

        private void SubscribeClicks(EditorMenuItem[] items)
        {
            if (items == null) { return; }

            foreach (var item in items)
            {
                if (item != null) { item.OnClickedListener += OnItemClicked; }
            }
        }

        private void Update()
        {
            if (menuBar_view == null || dataGetter == null) { return; }

            // 譜面の読み込み中と、設定のダイアログ・説明書を開いている間は、押せなくする
            bool isInteractable =
                !dataGetter.IsLoadingChart.Value &&
                !dataGetter.CurrentEditMode.Value.IsInEditModeList(disabledEditModes);
            menuBar_view.SetInteractable(isInteractable);

            // メニューバーの項目は、一覧を開いている間だけ、状態を更新し続ける (再生の状態や選択の変化に合わせる)
            if (menuBar_view.IsOpen) { RefreshItems(items_view); }
            // クイックメニューの項目は、いつも見えているので、いつも更新する
            RefreshItems(quickItems_view);

            RefreshValueLabels();
        }

        /// <summary>
        /// 全画面プレビューの中にいるか
        /// </summary>
        private bool IsInFullScreenPreview => dataGetter.EditNoteType.Value == EditNoteType.Preview;

        /// <summary>
        /// 項目が押されたとき
        /// </summary>
        private void OnItemClicked(EditorMenuItem item)
        {
            // 押せない状態の項目は、何もしない (見た目が切り替わる前に押されたときのため、もう一度確かめる)
            if (!IsEnabled(item)) { return; }

            // 先にメニューを閉じる (ファイルを選ぶ画面などが、メニューを開いたまま出ないように)
            if (item.ClosesMenu) { menuBar_view?.Close(); }

            Execute(item.Command, item.Value);

            // 変えた結果を、すぐに見せる
            RefreshItems(quickItems_view);
            if (menuBar_view != null && menuBar_view.IsOpen) { RefreshItems(items_view); }
            RefreshValueLabels();
        }

        private void Execute(EditorMenuCommand command, int value)
        {
            switch (command)
            {
                // ---- ファイル
                case EditorMenuCommand.OpenChart:
                    chartDataImporter_model?.Import();
                    break;
                case EditorMenuCommand.SaveChart:
                    chartDataExporter_model?.Export();
                    break;
                case EditorMenuCommand.SaveChartAs:
                    chartDataExporter_model?.ExportNewFile();
                    break;
                case EditorMenuCommand.SelectMusic:
                    editorUI_presenter?.BrowseAudioFile();
                    break;

                // ---- 編集
                case EditorMenuCommand.Undo:
                    inputHandler_model?.UndoOnce();
                    break;
                case EditorMenuCommand.Redo:
                    inputHandler_model?.RedoOnce();
                    break;
                case EditorMenuCommand.CopyNotes:
                    notesCopier_model?.CopySelectedNotes();
                    break;
                case EditorMenuCommand.DeleteNotes:
                    noteDestroyer_model?.DestroySelectedNotes();
                    break;

                // ---- 譜面
                case EditorMenuCommand.ExtendChart:
                    editorUI_presenter?.ExtendChart();
                    break;
                case EditorMenuCommand.RemoveLastBar:
                    laneExtender_model?.ChangeChartLength(-1);
                    break;

                // ---- 表示
                case EditorMenuCommand.SelectLaneDivision:
                    optionSetter.SelectLaneDivisionNum(value);
                    break;
                case EditorMenuCommand.SelectWindowSize:
                    if (Enum.IsDefined(typeof(Resolution), value)) { optionSetter.SetResolution((Resolution)value); }
                    break;
                case EditorMenuCommand.StartPreview:
                    // 全画面プレビューの中では、この項目は「編集画面へ戻る」になっている
                    if (IsInFullScreenPreview) { previewUI_presenter?.BackToEditor(); }
                    else { previewUI_presenter?.StartFullScreenPreview(); }
                    break;
                case EditorMenuCommand.RebuildPreview:
                    previewUI_presenter?.RebuildPreview();
                    break;

                // ---- 再生
                case EditorMenuCommand.TogglePlay:
                    inputHandler_model?.TogglePlay();
                    break;
                case EditorMenuCommand.SelectPlaybackSpeed:
                    optionSetter.SelectPlaybackSpeed(value / 100f);
                    break;
                case EditorMenuCommand.SelectNoteSound:
                    optionSetter.SelectNoteSoundMode(value == 1);
                    break;
                case EditorMenuCommand.StepNoteSpeed:
                    noteSpawnDataOptionSetter.SetNoteSpeed(noteSpawnDataOptionGetter.NoteSpeed.Value + value);
                    break;
                case EditorMenuCommand.StepHitVolume:
                    optionSetter.SetJudgementSEVolume(Step(optionGetter.JudgementSEVolume.Value, value));
                    break;

                // ---- 設定
                case EditorMenuCommand.StepScrollSensitivity:
                    optionSetter.SetScrollSensitivity(Step(optionGetter.ScrollSensitivity.Value, value));
                    break;
                case EditorMenuCommand.StepMusicVolume:
                    optionSetter.SetBGMVolume(Step(optionGetter.BGMVolume.Value, value));
                    break;

                // ---- 表示・クイックメニュー: 画面部品を、表示する / しない
                case EditorMenuCommand.TogglePanel:
                    if (Enum.IsDefined(typeof(EditorPanel), value))
                    {
                        var panel = (EditorPanel)value;
                        optionSetter.SetPanelVisible(panel, !optionGetter.GetPanelVisibility(panel).Value);
                    }
                    break;
            }
        }

        /// <summary>
        /// 0〜1 の値を、10分の1刻みで steps 段階ぶん動かす
        /// (半端な値になっていても、ボタンを押すと 0.1 刻みの値にそろう)
        /// </summary>
        private static float Step(float current, int steps)
        {
            float stepped = Mathf.Round(current / STEP_UNIT) + steps;
            return Mathf.Clamp01(stepped * STEP_UNIT);
        }

        /// <summary>
        /// 項目を、今の状態に合わせる
        /// </summary>
        private void RefreshItems(EditorMenuItem[] items)
        {
            if (items == null || dataGetter == null) { return; }

            bool isPlaying = dataGetter.PlayMode.Value == PlayMode.Play;
            bool isInPreview = IsInFullScreenPreview;

            foreach (var item in items)
            {
                if (item == null) { continue; }

                item.SetEnabled(IsEnabled(item));
                item.SetChecked(IsChecked(item));

                // 状態で名前が変わる項目
                switch (item.Command)
                {
                    case EditorMenuCommand.TogglePlay:
                        item.SetLabel(isPlaying ? "停止" : "再生");
                        break;
                    case EditorMenuCommand.StartPreview:
                        item.SetLabel(isInPreview ? "編集画面へ戻る" : "全画面プレビュー");
                        break;
                    case EditorMenuCommand.ExtendChart:
                        item.SetLabel(GetExtendChartLabel());
                        break;
                }
            }
        }

        /// <summary>
        /// 「譜面を延長」の項目の名前 (延長する小節数を書き添える。小節数が変わったときだけ、文字を作り直す)
        /// </summary>
        private string GetExtendChartLabel()
        {
            int barCount = optionGetter.ExtendBarCount.Value;
            if (barCount != displayedExtendBarCount || extendChartLabel == null)
            {
                displayedExtendBarCount = barCount;
                extendChartLabel = $"譜面を延長 ({barCount} 小節)";
            }

            return extendChartLabel;
        }

        /// <summary>
        /// 設定の今の値の表示を、値が変わっていたら書き直す
        /// </summary>
        private void RefreshValueLabels()
        {
            if (valueLabels_view == null || optionGetter == null) { return; }

            if (displayedValues == null || displayedValues.Length != valueLabels_view.Length)
            {
                displayedValues = new float[valueLabels_view.Length];
                for (int i = 0; i < displayedValues.Length; i++) { displayedValues[i] = float.NaN; }
            }

            for (int i = 0; i < valueLabels_view.Length; i++)
            {
                var entry = valueLabels_view[i];
                if (entry == null || entry.label == null) { continue; }

                float value = GetValue(entry.value);
                if (value == displayedValues[i]) { continue; }

                displayedValues[i] = value;
                entry.label.text = FormatValue(entry.value, value);
            }
        }

        private float GetValue(EditorMenuValue value)
        {
            switch (value)
            {
                case EditorMenuValue.NoteSpeed: return noteSpawnDataOptionGetter.NoteSpeed.Value;
                case EditorMenuValue.ScrollSensitivity: return optionGetter.ScrollSensitivity.Value;
                case EditorMenuValue.HitVolume: return optionGetter.JudgementSEVolume.Value;
                case EditorMenuValue.MusicVolume: return optionGetter.BGMVolume.Value;
                default: return 0f;
            }
        }

        private static string FormatValue(EditorMenuValue kind, float value)
        {
            switch (kind)
            {
                case EditorMenuValue.NoteSpeed: return value.ToString("0");
                case EditorMenuValue.ScrollSensitivity: return value.ToString("0.0");
                // 音量は百分率で表示する
                default: return Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        /// <summary>
        /// 項目を押せる状態か (キー操作と同じ条件にする)
        /// </summary>
        private bool IsEnabled(EditorMenuItem item)
        {
            bool isPlaying = dataGetter.PlayMode.Value == PlayMode.Play;
            bool isInPreview = IsInFullScreenPreview;
            var chartData = dataGetter.ChartData.Value;

            switch (item.Command)
            {
                // 再生中は、譜面や楽曲を入れ替えたり、譜面の長さを変えたりできない
                // 全画面プレビューの中でも受け付けない (読み込みが始まると、プレビューの操作ができなくなるため)
                case EditorMenuCommand.OpenChart:
                case EditorMenuCommand.SelectMusic:
                case EditorMenuCommand.ExtendChart:
                    return !isPlaying && !isInPreview;
                case EditorMenuCommand.RemoveLastBar:
                    return !isPlaying && !isInPreview && chartData != null && chartData.BarDatas.Count > 0;
                case EditorMenuCommand.SaveChartAs:
                    return !isPlaying;

                case EditorMenuCommand.Undo:
                    return inputHandler_model != null && inputHandler_model.CanUndoNow;
                case EditorMenuCommand.Redo:
                    return inputHandler_model != null && inputHandler_model.CanRedoNow;
                case EditorMenuCommand.CopyNotes:
                    return notesCopier_model != null && notesCopier_model.CanCopySelectedNotes;
                case EditorMenuCommand.DeleteNotes:
                    return noteDestroyer_model != null && noteDestroyer_model.CanDestroySelectedNotes;

                // プレビューは、譜面があるときだけ
                case EditorMenuCommand.StartPreview:
                case EditorMenuCommand.RebuildPreview:
                    return chartData != null;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 項目の値が、今選ばれている値か (いくつかの値から1つを選ぶ項目)、チェックが入っているか (表示する / しないを切り替える項目)
        /// </summary>
        private bool IsChecked(EditorMenuItem item)
        {
            switch (item.Command)
            {
                case EditorMenuCommand.SelectLaneDivision:
                    return optionGetter.LaneDivisionNum.Value == item.Value;
                case EditorMenuCommand.SelectWindowSize:
                    return (int)optionGetter.Resolution.Value == item.Value;
                case EditorMenuCommand.SelectPlaybackSpeed:
                    return Mathf.RoundToInt(optionGetter.PlaybackSpeed.Value * 100f) == item.Value;
                case EditorMenuCommand.SelectNoteSound:
                    return (optionGetter.IsSingleNoteSound.Value ? 1 : 0) == item.Value;
                case EditorMenuCommand.TogglePanel:
                    return Enum.IsDefined(typeof(EditorPanel), item.Value) &&
                           optionGetter.GetPanelVisibility((EditorPanel)item.Value).Value;
                default:
                    return false;
            }
        }
    }
}

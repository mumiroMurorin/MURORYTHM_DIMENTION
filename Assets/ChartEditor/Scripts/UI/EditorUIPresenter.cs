using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using System;

namespace ChartEditor
{
    /// <summary>
    /// 編集画面の画面部品と、譜面エディタの機能をつなぐ
    ///
    /// ここで扱うのは、キャンバスの切り替え、小節・分線の設定ダイアログ (どの線の設定かの表示も)、操作の説明、レイヤーの切り替え、
    /// オフセットの入力欄、楽曲名の表示。
    /// メニューバーとクイックメニュー (左下の設定欄) の項目は、EditorMenuPresenter が扱う
    /// (そこから、ここの BrowseAudioFile・ExtendChart を呼ぶ)。
    ///
    /// レイヤーのボタン (と Alt+L) で回る順番:
    /// ・ふだん: グラウンド → スペース → グラウンド …
    /// ・SpaceHold を選んでいるとき: グラウンド → スペース → 頂点 → 3D → グラウンド …
    ///   スペースで SpaceHold を選んでいれば、頂点のレイヤーでその節の形を編集する。
    ///   頂点のレイヤーで SpaceHold の形を編集していれば、その SpaceHold を3D編集で開く。
    ///   3D編集の画面のレイヤーのボタンで、グラウンドに戻る (SpaceHold3DEditor.SwitchLayerFromButton)
    /// </summary>
    public class EditorUIPresenter : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] GameObject editorCanvas;
        [SerializeField] GameObject previewCanvas;

        [Header("Views")]
        [SerializeField] OffsetInputFieldView offsetInputField_view;
        [SerializeField] MusicNameView musicName_view;
        [SerializeField] RhythmConfigBarView rhythmConfigBar_view;
        [SerializeField] RhythmConfigSubView rhythmConfigSubDivision_view;
        [Tooltip("設定を開いている線の、レーンの上の表示を囲む枠")]
        [SerializeField] RhythmConfigTargetFrameView rhythmConfigTargetFrame_view;
        [SerializeField] OperationDescriptionView description_view;
        [SerializeField] ExplanationButtonView explanationButton_view;
        [SerializeField] ExplanationView explanation_view;
        [SerializeField] SwitchLayerButtonView switchLayerButton_view;

        [Header("Models")]
        [SerializeField] ConfigEditor configEditor_model;
        [SerializeField] LaneExtender laneExtender_model;

        IChartEditorDataSetter dataSetter;
        IChartEditorDataGetter dataGetter_model;
        IChartEditorOptionGetter optionGetter;
        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;

        CancellationTokenSource soundLoadCts;

        [Inject]
        public void Construct(
            IChartEditorDataSetter chartEditorDataSetter,
            IChartEditorDataGetter chartEditorDataGetter,
            IChartEditorOptionGetter optionDataGetter,
            INotesDataGetter notesDataGetter,
            INotesDataSetter notesDataSetter)
        {
            dataSetter = chartEditorDataSetter;
            dataGetter_model = chartEditorDataGetter;
            optionGetter = optionDataGetter;
            notesGetter = notesDataGetter;
            notesSetter = notesDataSetter;
        }

        /// <summary>
        /// レイヤーを切り替えるショートカットキー (Alt と一緒に押す。レイヤーのボタンに「Alt+L」と表示する)
        /// (3D編集の画面のレイヤーのボタンにも、同じキーを表示する: SpaceHold3DEditor)
        /// </summary>
        public const KeyCode SWITCH_LAYER_KEY = KeyCode.L;

        // レイヤーの切り替えを、キーでは受け付けないモード (ほかの操作の途中、ダイアログや説明書を開いている間、全画面プレビュー)
        static readonly EditMode[] switchLayerIgnoreEditModes =
        {
            EditMode.Connecting,
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.SpaceMoving,
            EditMode.CurveHandling,
            EditMode.VertexMoving,
            EditMode.VerticesRotating,
            EditMode.VerticesScaling,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
            EditMode.Preview,
        };

        void Start()
        {
            BindForRhythmConfig();
            BindForOther();

            SetEvent();

            switchLayerButton_view.SetShortcutText(ShortcutBadge.FormatAltKey(SWITCH_LAYER_KEY));
        }

        void Update()
        {
            // Alt+L: レイヤーを切り替える (レイヤーのボタンと同じ)
            if (!ModifierKey.Alt || !Input.GetKeyDown(SWITCH_LAYER_KEY)) { return; }

            // 読み込み中・文字の入力中・ドラッグの途中・メニューを開いている間・ほかの操作の途中は受け付けない
            if (dataGetter_model.IsLoadingChart.Value || InputFieldFocus.IsTyping || Input.GetMouseButton(0)) { return; }

            // SpaceHold の3D編集を開いている間: 3D編集の画面の、レイヤーのボタンと同じ (グラウンドに戻る)
            // (3D編集は画面を覆っているので、下の IsBlockingChartInput で止まる。その前に見る。
            //  キーを見るのはここだけにして、同じフレームに2回切り替わらないようにする)
            // (メニュー・右クリックのメニュー・ショートカット一覧・説明書を開いている間は受け付けない)
            if (SpaceHold3DEditor.IsOpen)
            {
                if (EditorMenuBar.IsMenuOpenOrJustClosed) { return; }
                if (dataGetter_model.CurrentEditMode.Value == EditMode.Explanation) { return; }
                SwitchLayer();
                return;
            }

            if (EditorMenuBar.IsBlockingChartInput) { return; }
            if (dataGetter_model.EditNoteType.Value == EditNoteType.Preview) { return; }
            if (dataGetter_model.CurrentEditMode.Value.IsInEditModeList(switchLayerIgnoreEditModes)) { return; }

            SwitchLayer();
        }

        /// <summary>
        /// レイヤーのボタン (と Alt+L): 次のレイヤーへ
        /// SpaceHold を選んでいるときは、スペース → 頂点 → 3D と進む。それ以外は、今までどおり
        /// </summary>
        private void SwitchLayer()
        {
            // 3D編集を開いているとき: 3D → グラウンド (3D編集の画面のレイヤーのボタンと同じ)
            // (ふつうは3D編集の画面が、このボタンを覆っている。押せたときに、裏の頂点のレイヤーから3D編集を開き直さないように)
            if (SpaceHold3DEditor.IsOpen)
            {
                SpaceHold3DEditor.Instance.SwitchLayerFromButton();
                return;
            }

            if (TrySwitchLayerForSpaceHold()) { return; }

            dataSetter.SwitchEditNoteType();
        }

        /// <summary>
        /// SpaceHold を選んでいるときの、次のレイヤーへ進む
        /// ・スペース → 頂点: 選んでいる SpaceHold の節の形を、頂点のレイヤーで編集する
        /// ・頂点 → 3D: 形を編集している SpaceHold を、3D編集で開く (頂点のレイヤーは、開いたまま裏に残る)
        /// </summary>
        /// <returns>進んだとき true。SpaceHold を選んでいないときなどは false (今までどおりに切り替える)</returns>
        private bool TrySwitchLayerForSpaceHold()
        {
            switch (dataGetter_model.EditNoteType.Value)
            {
                case EditNoteType.Space:
                    {
                        var target = FindSelectedSpaceHold(out bool hasLockedOnly);
                        if (target == null)
                        {
                            if (hasLockedOnly) { Debug.Log("【ロック】ロックした SpaceHold の形は、頂点のレイヤーで編集できません (外すには、右クリックのメニューの「ロックを外す」)"); }
                            return false;
                        }

                        // スペースのレイヤーでダブルクリックしたときと同じ (SpaceEditor)
                        dataSetter.SetEditNoteType(EditNoteType.Vertices);
                        notesSetter.SetEditingVertices(target);
                        return true;
                    }

                case EditNoteType.Vertices:
                    {
                        var editing = notesGetter.EditingVertices.Value as NoteData_SpaceHold;
                        var editor = SpaceHold3DEditor.Instance;
                        if (editing == null || editor == null || notesGetter.GetNoteObject(editing) == null) { return false; }

                        editor.Open(editing);
                        // 開けなかったとき (3D編集の画面が作れなかったときなど) は、今までどおりスペースへ戻る
                        return SpaceHold3DEditor.IsOpen;
                    }

                default:
                    return false;
            }
        }

        /// <summary>
        /// 選んでいるノーツのうち、頂点のレイヤーで形を編集できる、最初の SpaceHold
        /// (3D編集のメニューと同じく、選んだ順で最初のもの。ロックした SpaceHold は飛ばす)
        /// </summary>
        /// <param name="hasLockedOnly">SpaceHold を選んでいるが、どれもロックしているとき true</param>
        private NoteData_SpaceHold FindSelectedSpaceHold(out bool hasLockedOnly)
        {
            hasLockedOnly = false;
            foreach (var selecting in notesGetter.SelectingNotes)
            {
                if (selecting is not NoteData_SpaceHold spaceHold || notesGetter.GetNoteObject(spaceHold) == null) { continue; }
                if (NoteLock.IsLocked(spaceHold))
                {
                    hasLockedOnly = true;
                    continue;
                }

                hasLockedOnly = false;
                return spaceHold;
            }
            return null;
        }

        private void BindForOther()
        {
            // オフセットの入力欄・説明書ボタンは、再生中は押せない
            dataGetter_model?.PlayMode
                .Subscribe(value =>
                {
                    offsetInputField_view?.OnChangePlayMode(value);
                    explanationButton_view?.OnChangePlayMode(value);
                })
                .AddTo(this.gameObject);

            // オフセットの変更
            dataGetter_model?.Offset
                .Subscribe(offsetInputField_view.OnChangeFloatValue)
                .AddTo(this.gameObject);

            // 楽曲の変更
            dataGetter_model?.Music
                .Subscribe(musicName_view.OnChangeMusic)
                .AddTo(this.gameObject);

            // 操作説明の表示 (エディットモードとレイヤーで、出す行が決まる)
            dataGetter_model?.CurrentEditMode
                .Subscribe(description_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            dataGetter_model?.EditNoteType
                .Subscribe(description_view.OnChangeEditNoteType)
                .AddTo(this.gameObject);

            // 説明書の表示、非表示
            dataGetter_model?.CurrentEditMode
                .Subscribe(explanation_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            // レイヤー変更ボタンのインタラクト可不可
            dataGetter_model?.CurrentEditMode
                .Subscribe(switchLayerButton_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            // レイヤー変更ボタンの更新
            dataGetter_model?.EditNoteType
                .Subscribe(switchLayerButton_view.OnChangeEditNoteType)
                .AddTo(this.gameObject);

            // キャンバスの更新
            dataGetter_model?.EditNoteType
                .Subscribe(ActivateCanvas)
                .AddTo(this.gameObject);
        }

        private void BindForRhythmConfig()
        {
            // リズムコンフィグ(小節線)のクリック
            dataGetter_model?.CurrentEditMode
                .Where(mode => mode == EditMode.EditingBarConfig)
                .Subscribe(value =>
                {
                    var config = configEditor_model.EditingBarConfig;

                    rhythmConfigBar_view.SetDataOnUI(config);
                    // どの小節の設定かを、見出しと、レーンの上の枠で示す
                    rhythmConfigBar_view.SetTarget(configEditor_model.EditingBarIndex);
                    rhythmConfigTargetFrame_view?.Show(configEditor_model.EditingTargetCollider);
                    rhythmConfigBar_view.SetActive(true);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(分線)のクリック
            dataGetter_model?.CurrentEditMode
                .Where(mode => mode == EditMode.EditingSubDivisionConfig)
                .Subscribe(value =>
                {
                    var config = configEditor_model.EditingSubdivisionConfig;

                    rhythmConfigSubDivision_view.SetDataOnUI(config);
                    // どの分線の設定かを、見出しと、レーンの上の枠で示す
                    rhythmConfigSubDivision_view.SetTarget(configEditor_model.EditingBarIndex, configEditor_model.EditingSubDivisionIndex);
                    rhythmConfigTargetFrame_view?.Show(configEditor_model.EditingTargetCollider);
                    rhythmConfigSubDivision_view.SetActive(true);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(小節線)を閉じる
            dataGetter_model?.CurrentEditMode
                .Pairwise()
                .Where(pair => pair.Previous == EditMode.EditingBarConfig)
                .Subscribe(value =>
                {
                    // キャンセルされたときは適用せずに閉じる
                    if (!configEditor_model.ConsumeCanceled())
                    {
                        rhythmConfigBar_view.SetData(configEditor_model.ChangeBarConfig);
                    }
                    rhythmConfigBar_view.SetActive(false);
                    rhythmConfigTargetFrame_view?.Hide();
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(分線)を閉じる
            dataGetter_model?.CurrentEditMode
                .Pairwise()
                .Where(pair => pair.Previous == EditMode.EditingSubDivisionConfig)
                .Subscribe(value =>
                {
                    // キャンセルされたときは適用せずに閉じる
                    if (!configEditor_model.ConsumeCanceled())
                    {
                        rhythmConfigSubDivision_view.SetData(configEditor_model.ChangeSubDivisionConfig);
                    }
                    rhythmConfigSubDivision_view.SetActive(false);
                    rhythmConfigTargetFrame_view?.Hide();
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // オフセットフィールド
            offsetInputField_view.OnFloatValueChangedListner += dataSetter.SetOffset;

            // リズムコンフィグ
            rhythmConfigBar_view.OnClickedApplyButtonListner += () => configEditor_model.CloseConfig();
            rhythmConfigSubDivision_view.OnClickedApplyButtonListner += () => configEditor_model.CloseConfig();
            // Cancel: 入力した内容を適用せずに閉じる (Esc キーと同じ)
            rhythmConfigBar_view.OnClickedCancelButtonListner += () => configEditor_model.CancelConfig();
            rhythmConfigSubDivision_view.OnClickedCancelButtonListner += () => configEditor_model.CancelConfig();

            // レイヤー変更ボタン
            switchLayerButton_view.OnClickCloseButtonListner += SwitchLayer;

            explanationButton_view.OnClickedListner += () => dataSetter.SetEditMode(EditMode.Explanation);

            // 説明書を閉じる
            explanation_view.OnClickCloseButtonListner += () => dataSetter.SetEditMode(EditMode.None);
        }

        /// <summary>
        /// 譜面を延長する。延長する小節数と、延長する小節の拍子・分割数は、延長の設定の値を使う
        /// (メニューとクイックメニューの「譜面を延長」から呼ばれる)
        /// </summary>
        public void ExtendChart()
        {
            // 延長できない組み合わせのときは、理由が表示される
            if (!ChartExtendSettings.TryGetValues(optionGetter, out int barCount, out BarConfig barConfig)) { return; }

            laneExtender_model.ExtendChart(barCount, barConfig);
        }

        /// <summary>
        /// 楽曲ファイルをセット (メニューとクイックメニューの「楽曲を選ぶ」から呼ばれる)
        /// </summary>
        public async void BrowseAudioFile()
        {
            if (soundLoadCts != null)
            {
                soundLoadCts.Cancel();
                soundLoadCts.Dispose();
            }

            soundLoadCts = new CancellationTokenSource();

            AudioClip clip = await AudioFileSelector.SelectAudioFile(soundLoadCts.Token);

            // キャンセルされた、または読み込めなかったときは現在の楽曲を残す
            if (clip == null) { return; }

            dataSetter.SetMusic(clip);
        }

        private void ActivateCanvas(EditNoteType noteType)
        {
            previewCanvas.SetActive(noteType == EditNoteType.Preview);
            editorCanvas.SetActive(noteType != EditNoteType.Preview);
        }

        private void OnDestroy()
        {
            soundLoadCts?.CancelAndDispose();
        }
    }
}

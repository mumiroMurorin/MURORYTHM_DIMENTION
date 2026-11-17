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
    public class NoteButtonPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] NotesViewportView notesViewport_view;
        [SerializeField] List<NoteButtonToEditMode> noteButtons_view;

        IChartEditorDataSetter editorDataSetter_model;
        IChartEditorDataGetter editorDataGetter_model;
        IChartEditorOptionSetter optionDataSetter_model;
        IChartEditorOptionGetter optionDataGetter_model;

        // ショートカットキーで選べる配置ノーツの数の上限 (Alt + 1 〜 Alt + 9)
        const int MAX_SHORTCUT_COUNT = 9;

        // 操作の途中なので、配置ノーツのショートカットキーを受け付けないモード
        static readonly EditMode[] shortcutIgnoreModes =
        {
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.Connecting,
            EditMode.SpaceMoving,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
        };

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionSetter optionDataSetter, IChartEditorOptionGetter optionDataGetter)
        {
            editorDataSetter_model = chartEditorDataSetter;
            optionDataSetter_model = optionDataSetter;
            editorDataGetter_model = chartEditorDataGetter;
            optionDataGetter_model = optionDataGetter;
        }

        void Start()
        {
            BindForNoteView();

            SetEvent();

            SetShortcutTexts();
        }

        private void Update()
        {
            CheckShortcutKey();
        }

        /// <summary>
        /// 配置ノーツのショートカットキー
        /// Alt を押しながら数字キーを押すと、今のレイヤーの一覧の、上からその番号のノーツを選ぶ
        /// (Alt + 1 で1番目、Alt + 2 で2番目。ボタンをクリックしたときと同じ)
        /// </summary>
        private void CheckShortcutKey()
        {
            if (!ModifierKey.Alt) { return; }

            // 譜面の読み込み中、入力欄に文字を打っている間、ドラッグの途中は受け付けない
            if (editorDataGetter_model.IsLoadingChart.Value) { return; }
            if (InputFieldFocus.IsTyping) { return; }
            if (Input.GetMouseButton(0)) { return; }
            if (editorDataGetter_model.CurrentEditMode.Value.IsInEditModeList(shortcutIgnoreModes)) { return; }

            int number = GetPressedNumber();
            if (number <= 0) { return; }

            // 今のレイヤーに出ているボタンのうち、上から number 番目
            var layer = editorDataGetter_model.EditNoteType.Value;
            int order = 0;
            foreach (var button in noteButtons_view)
            {
                if (button.EditNoteType != layer) { continue; }

                order++;
                if (order != number) { continue; }

                DeploymentNoteChoice.SetNone(false);
                editorDataSetter_model.SetNoteType(button.NoteType);
                return;
            }
        }

        /// <summary>
        /// 今のフレームで押された数字キーの番号 (1〜9)。押されていなければ 0
        /// 文字キーの上の数字でも、テンキーの数字でも反応する
        /// </summary>
        private static int GetPressedNumber()
        {
            for (int i = 0; i < MAX_SHORTCUT_COUNT; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) { return i + 1; }
            }

            return 0;
        }

        /// <summary>
        /// それぞれのボタンに、ショートカットキーを表示する (レイヤーごとに、上から Alt+1、Alt+2…)
        /// </summary>
        private void SetShortcutTexts()
        {
            var orders = new Dictionary<EditNoteType, int>();
            foreach (var button in noteButtons_view)
            {
                orders.TryGetValue(button.EditNoteType, out int order);
                order++;
                orders[button.EditNoteType] = order;

                if (order > MAX_SHORTCUT_COUNT || button.NoteButton_view == null) { continue; }

                button.NoteButton_view.SetShortcutText($"Alt+{order}");
            }
        }

        private void BindForNoteView()
        {
            // 配置ノーツボタン
            foreach (var button in noteButtons_view)
            {
                button.BindForDeploymentNoteType(editorDataGetter_model.DeploymentNoteType, this.gameObject);
                button.BindForEditNoteType(editorDataGetter_model.EditNoteType, this.gameObject);
            }

            // 置くノーツを選んでいない間は、どのボタンも選んでいない見た目 (録音 20261118_010533)
            DeploymentNoteChoice.OnChanged += RefreshChosenLook;

            // ノーツビューの可視不可視
            editorDataGetter_model?.CurrentEditMode
                .Subscribe(mode => notesViewport_view.OnChangeEditMode(mode, editorDataGetter_model.EditNoteType.Value))
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // 配置ノーツボタン (置くノーツを選んでいない状態から、また選んだ状態にする)
            foreach (var button in noteButtons_view)
            {
                button.SetEvent(() =>
                {
                    DeploymentNoteChoice.SetNone(false);
                    editorDataSetter_model.SetNoteType(button.NoteType);
                });
            }
        }

        private void RefreshChosenLook()
        {
            if (this == null) { return; }
            var chosen = editorDataGetter_model.DeploymentNoteType.Value;
            foreach (var button in noteButtons_view)
            {
                button.NoteButton_view?.OnChangeDeploymentNote(!DeploymentNoteChoice.IsNone && chosen == button.NoteType);
            }
        }

        private void OnDestroy()
        {
            DeploymentNoteChoice.OnChanged -= RefreshChosenLook;
        }

        /// <summary>
        /// ノートボタンに対するアクション他
        /// </summary>
        [Serializable]
        public class NoteButtonToEditMode
        {
            [SerializeField] ChangeDeploymentNoteButtonView noteButton_view;
            [SerializeField] DeploymentNoteType noteType;
            [SerializeField] EditNoteType editNoteType;

            public ChangeDeploymentNoteButtonView NoteButton_view { get { return noteButton_view; } }

            public DeploymentNoteType NoteType { get { return noteType; } }

            public EditNoteType EditNoteType { get { return editNoteType; } }

            public void BindForDeploymentNoteType(IReadOnlyReactiveProperty<DeploymentNoteType> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(noteType => NoteButton_view.OnChangeDeploymentNote(!DeploymentNoteChoice.IsNone && noteType == this.noteType))
                    .AddTo(gameObject);
            }

            public void BindForEditNoteType(IReadOnlyReactiveProperty<EditNoteType> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(editNoteType => NoteButton_view.OnChangeEditNoteType(editNoteType == this.editNoteType))
                    .AddTo(gameObject);
            }

            public void SetEvent(Action action)
            {
                noteButton_view.OnClickedListner += action;
            }
        }   
    }
}
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 画面上のメニューから実行できる操作
    ///
    /// 番号はシーン(ChartEditor.unity)の各項目に保存されているので、後から変えない。
    /// 項目を足すときは、空いている番号を使う。
    /// 「値」と書いてある操作は、項目ごとに持っている値(EditorMenuItem.Value)を一緒に使う。
    /// </summary>
    public enum EditorMenuCommand
    {
        None = 0,

        // ファイル
        OpenChart = 100,
        SaveChart = 101,
        SaveChartAs = 102,
        /// <summary>新しい譜面を作る (保存していない変更があれば、確かめてから)</summary>
        NewChart = 103,
        SelectMusic = 110,
        /// <summary>値 = バックアップの一覧の何番目か (0 が、いちばん新しいもの)</summary>
        RestoreBackup = 120,
        /// <summary>自動保存のフォルダを開いて、復元するファイルを選ぶ</summary>
        RestoreBackupFromFolder = 121,
        /// <summary>「ファイル」の一覧の横に、バックアップの一覧を開く / 閉じる</summary>
        ToggleBackupList = 122,

        // 編集
        Undo = 200,
        Redo = 201,
        CopyNotes = 210,
        DeleteNotes = 211,
        /// <summary>コピーしたノーツを、今の再生位置の分線に貼り付ける</summary>
        PasteNotes = 212,
        /// <summary>選択中のノーツを切り取る (コピーして消す)</summary>
        CutNotes = 213,
        /// <summary>選択中のノーツを接続する (Ctrl+J と同じ)</summary>
        ConnectNotes = 214,
        /// <summary>選択中のノーツを反転する (Alt+M と同じ)</summary>
        MirrorNotes = 215,
        /// <summary>選択中のノーツをロックする</summary>
        LockNotes = 216,
        /// <summary>選択中のノーツのロックを外す</summary>
        UnlockNotes = 217,

        // 譜面
        RemoveLastBar = 301,
        /// <summary>延長の設定 (小節数、拍子、分割数) のとおりに、譜面を延長する</summary>
        ExtendChart = 302,
        /// <summary>ロックしたノーツのロックを、すべて外す (NoteLock)</summary>
        UnlockAllNotes = 303,

        // 表示
        /// <summary>値 = レーン分割数</summary>
        SelectLaneDivision = 400,
        /// <summary>値 = ノートスピードを増やす量 (負の数で減らす)</summary>
        StepNoteSpeed = 410,
        /// <summary>値 = スクロール感度を増やす量 (10分の1単位。1 で +0.1)</summary>
        StepScrollSensitivity = 411,
        /// <summary>値 = バックアップ (自動保存) の間隔 (分)。0 = なし</summary>
        SelectBackupInterval = 412,
        /// <summary>値 = プレビューで表示する奥行きを増やす量</summary>
        StepVisibleDistance = 413,
        /// <summary>値 = カーブの半径を増やす量</summary>
        StepCurveRadius = 414,
        /// <summary>値 = SpaceHold の3D編集の選択モード「Nつおき」の N (1〜4。録音 20261117_205010)</summary>
        SelectAlternateInterval = 415,
        /// <summary>値 = ウィンドウサイズ (Resolution の番号)</summary>
        SelectWindowSize = 420,
        StartPreview = 430,
        RebuildPreview = 431,
        /// <summary>選んでいる (頂点を編集している) SpaceHold を、3D編集で開く / 閉じる</summary>
        ToggleSpaceHold3DView = 432,
        /// <summary>値 = 頂点レイヤーのオニオンスキンの枚数 (前後それぞれ。0 で表示しない)</summary>
        SelectOnionSkinCount = 440,
        /// <summary>譜面を拡大する (Ctrl+ホイールと同じ)</summary>
        ZoomIn = 450,
        /// <summary>譜面を縮小する</summary>
        ZoomOut = 451,
        /// <summary>譜面の拡大率を、最初の大きさに戻す</summary>
        ZoomReset = 452,

        // 再生
        TogglePlay = 500,
        /// <summary>値 = 再生速度 (百分率。50 で ×0.5)</summary>
        SelectPlaybackSpeed = 510,
        /// <summary>値 = ノーツ音 (0: 種類別、1: 単一)</summary>
        SelectNoteSound = 520,
        /// <summary>値 = 判定音量を増やす量 (10分の1単位。1 で +10%)</summary>
        StepHitVolume = 530,
        /// <summary>値 = 楽曲の音量を増やす量 (10分の1単位。1 で +10%)</summary>
        StepMusicVolume = 531,

        // 表示・クイックメニュー
        /// <summary>値 = 表示する / しないを切り替える画面部品 (EditorPanel の番号)</summary>
        TogglePanel = 600,
        // ヘルプ
        /// <summary>説明書を開く</summary>
        OpenManual = 700,
        /// <summary>ショートカット一覧を開く</summary>
        ShowShortcuts = 701,
    }

    /// <summary>
    /// 画面上のメニューと、クイックメニュー (左下の設定欄) の、押せる項目1つぶん
    ///
    /// 2つの見た目がある。
    /// ・行: メニューの横幅いっぱいの1行 (「上書き保存」など)。カーソルを乗せると色が付く
    ///       横にもう1つ一覧を開く行 (「バックアップから復元」) は、その一覧を開いている間も、色を付けたままにする
    /// ・ボタン: 行の中に並ぶ小さな押しどころ (レーン分割数の「4」「8」、ノートスピードの「+」など)。
    ///           今選ばれている値のボタンは、色を変えて示す
    /// どの操作をするかは、項目ごとの「操作」と「値」で決まる。押されたことを知らせるだけで、操作そのものは EditorMenuPresenter が行う。
    /// ボタン(Button)は使わない (クリックした後も選択中のまま残り、Space キーでもう一度押されてしまうため)。
    /// </summary>
    public class EditorMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("押したときに行う操作")]
        [SerializeField] EditorMenuCommand command;
        [Tooltip("操作と一緒に使う値 (分割数、増やす量など。使わない操作では 0)")]
        [SerializeField] int value;
        [Tooltip("押したらメニューを閉じるか (値を続けて変えたい項目では、閉じない)")]
        [SerializeField] bool closesMenu = true;
        [Tooltip("行の中に並ぶ小さなボタンか (false のときは、横幅いっぱいの行)")]
        [SerializeField] bool isButton;
        [SerializeField] Image background;
        [SerializeField] TextMeshProUGUI label;
        [Tooltip("ショートカットキーの表示 (無ければ空)")]
        [SerializeField] TextMeshProUGUI shortcutLabel;
        [Tooltip("チェックの印 (チェックを入れたり外したりする行だけ。無ければ空)")]
        [SerializeField] GameObject checkMark;

        // 行の色
        static readonly Color rowColor = new Color(0f, 0f, 0f, 0f);
        static readonly Color rowHoverColor = new Color(0.22f, 0.42f, 0.78f, 1f);
        // ボタンの色
        static readonly Color buttonColor = new Color(0.30f, 0.30f, 0.34f, 1f);
        static readonly Color buttonHoverColor = new Color(0.42f, 0.42f, 0.48f, 1f);
        static readonly Color buttonCheckedColor = new Color(0.22f, 0.46f, 0.85f, 1f);
        static readonly Color buttonCheckedHoverColor = new Color(0.32f, 0.56f, 0.93f, 1f);
        static readonly Color buttonDisabledColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        // 文字の色
        static readonly Color textColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        static readonly Color textDisabledColor = new Color(0.50f, 0.50f, 0.53f, 1f);
        static readonly Color shortcutColor = new Color(0.72f, 0.72f, 0.76f, 1f);
        static readonly Color shortcutDisabledColor = new Color(0.42f, 0.42f, 0.45f, 1f);

        bool isEnabled = true;
        bool isChecked;
        bool isHovered;

        public EditorMenuCommand Command => command;
        public int Value => value;
        public bool ClosesMenu => closesMenu;

        /// <summary>
        /// 押されたとき (押せない状態のときは呼ばれない)
        /// </summary>
        public Action<EditorMenuItem> OnClickedListener { get; set; }

        /// <summary>
        /// 押せるかどうかを切り替える (押せないときは、文字を薄くする)
        /// </summary>
        public void SetEnabled(bool isEnabled)
        {
            if (this.isEnabled == isEnabled) { return; }

            this.isEnabled = isEnabled;
            Refresh();
        }

        /// <summary>
        /// 今選ばれている値か・チェックが入っているかを切り替える (ボタンは色で、行はチェックの印で示す)
        /// </summary>
        public void SetChecked(bool isChecked)
        {
            if (this.isChecked == isChecked) { return; }

            this.isChecked = isChecked;
            Refresh();
        }

        /// <summary>
        /// 項目の文字を変える (「再生」と「停止」のように、状態で名前が変わる項目用)
        /// </summary>
        public void SetLabel(string text)
        {
            if (label != null && label.text != text) { label.text = text; }
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            // メニューが閉じると、カーソルが外れた知らせは来ない。次に開いたときに色が残らないようにする
            isHovered = false;
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            Refresh();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            Refresh();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            // 左クリックだけ
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            if (!isEnabled) { return; }

            OnClickedListener?.Invoke(this);
        }

        /// <summary>
        /// 今の状態 (押せるか、選ばれているか、カーソルが乗っているか) に合わせて、色を付け直す
        /// </summary>
        private void Refresh()
        {
            if (background != null) { background.color = GetBackgroundColor(); }
            if (label != null) { label.color = isEnabled ? textColor : textDisabledColor; }
            if (shortcutLabel != null) { shortcutLabel.color = isEnabled ? shortcutColor : shortcutDisabledColor; }
            if (checkMark != null && checkMark.activeSelf != isChecked) { checkMark.SetActive(isChecked); }
        }

        private Color GetBackgroundColor()
        {
            bool isHighlighted = isHovered && isEnabled;

            if (!isButton)
            {
                // チェックの印を持たない行が「選ばれている」のは、その行から開いた一覧が出ている間
                bool isListOpen = isChecked && checkMark == null && isEnabled;
                return isHighlighted || isListOpen ? rowHoverColor : rowColor;
            }

            if (isChecked) { return isHighlighted ? buttonCheckedHoverColor : buttonCheckedColor; }
            if (!isEnabled) { return buttonDisabledColor; }
            return isHighlighted ? buttonHoverColor : buttonColor;
        }
    }
}

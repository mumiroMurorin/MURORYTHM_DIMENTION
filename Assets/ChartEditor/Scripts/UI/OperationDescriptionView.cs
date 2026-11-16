using System;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 操作説明の行を出すレイヤー (いくつでも選べる)
    /// </summary>
    [Flags]
    public enum DescriptionLayers
    {
        Ground = 1,
        Space = 2,
        Vertices = 4,
        /// <summary>全画面プレビュー</summary>
        Preview = 8,
    }

    /// <summary>
    /// 全画面プレビューの状態 (操作説明の行を、どの状態で出すか。いくつでも選べる)
    /// </summary>
    [Flags]
    public enum DescriptionPreviewStates
    {
        /// <summary>頂点の道具の列が出ていない (頂点を持つノーツを、1つだけ選んではいない)</summary>
        NoVertexNote = 1,
        /// <summary>頂点の道具の列が出ていて、道具は選んでいない</summary>
        VertexNote = 2,
        /// <summary>頂点の道具の「編集」を選んでいる</summary>
        EditTool = 4,
        /// <summary>頂点の道具の「回転」を選んでいる</summary>
        RotateTool = 8,
        /// <summary>頂点の道具の「拡大縮小」を選んでいる</summary>
        ScaleTool = 16,
    }

    /// <summary>
    /// 操作説明の行を出す、追加の条件
    /// </summary>
    public enum DescriptionCondition
    {
        Always = 0,
        /// <summary>ホールドを1本だけ編集している間だけ</summary>
        OnlyInHoldIsolation = 1,
        /// <summary>ホールドを1本だけ編集している間は出さない</summary>
        NotInHoldIsolation = 2,
        /// <summary>全画面プレビューで、頂点を選んでいる間だけ</summary>
        OnlyWithPreviewVertexSelection = 3,
        /// <summary>全画面プレビューで、頂点を選んでいる間は出さない</summary>
        NotWithPreviewVertexSelection = 4,
    }

    /// <summary>
    /// 操作説明 (画面の右側の、今できる操作の一覧)
    ///
    /// 1行が1つの操作で、「押すもの」と「起こること」が並ぶ (例: Shift + ドラッグ → 範囲選択)。
    /// 行は、はじめからシーンに置いてある。ここでは、今の状態で使える行だけを出す。
    /// どの行をいつ出すかは、行ごとの「エディットモード」「レイヤー」「条件」で決まる。
    /// 全画面プレビューの行は、さらに「プレビューの状態」(頂点の道具の列が出ているか、どの道具を選んでいるか) でも決まる。
    /// (この欄は、全画面プレビューでも出しておくために、いつも手前に出すキャンバス (MenuCanvas) に置いてある)
    /// 出ている行は、枠の中で上から詰めて並ぶ (枠に付いている縦並びの部品が並べる)。1行も出ないときは、枠ごと隠す。
    ///
    /// 操作を増やしたとき・変えたときは、ここにも行を足す (どんなに細かい操作でも載せる、という方針)。
    /// </summary>
    public class OperationDescriptionView : MonoBehaviour
    {
        [Tooltip("行を並べる枠 (1行も出ていないときは隠す)")]
        [SerializeField] GameObject panel;
        [SerializeField] DescriptionRow[] rows;

        EditMode editMode = EditMode.None;
        EditNoteType layer = EditNoteType.Ground;
        bool isHoldIsolation;
        DescriptionPreviewStates previewState = DescriptionPreviewStates.NoVertexNote;
        bool hasVertexSelection;

        private void Start()
        {
            Refresh();
        }

        public void OnChangeEditMode(EditMode editMode)
        {
            this.editMode = editMode;
            Refresh();
        }

        public void OnChangeEditNoteType(EditNoteType layer)
        {
            this.layer = layer;
            Refresh();
        }

        private void Update()
        {
            // ホールドを1本だけ編集しているかどうかと、全画面プレビューの状態は、知らせが来ないので、ここで見る
            bool isIsolation = HoldIsolation.IsActive;
            DescriptionPreviewStates state = GetPreviewState();
            bool hasSelection = PreviewVertexTools.HasSelectedVertices;
            if (isIsolation == isHoldIsolation && state == previewState && hasSelection == hasVertexSelection) { return; }

            isHoldIsolation = isIsolation;
            previewState = state;
            hasVertexSelection = hasSelection;
            Refresh();
        }

        /// <summary>
        /// 全画面プレビューの、今の状態 (頂点の道具の列が出ているか、どの道具を選んでいるか)
        /// </summary>
        private static DescriptionPreviewStates GetPreviewState()
        {
            if (!PreviewVertexTools.IsListShown) { return DescriptionPreviewStates.NoVertexNote; }

            switch (PreviewVertexTools.Current)
            {
                case PreviewVertexTool.Edit: return DescriptionPreviewStates.EditTool;
                case PreviewVertexTool.Rotate: return DescriptionPreviewStates.RotateTool;
                case PreviewVertexTool.Scale: return DescriptionPreviewStates.ScaleTool;
                default: return DescriptionPreviewStates.VertexNote;
            }
        }

        private void Refresh()
        {
            if (rows == null) { return; }

            bool isAnyShown = false;
            foreach (var row in rows)
            {
                if (row == null) { continue; }

                bool isShown = row.IsShown(editMode, layer, isHoldIsolation, previewState, hasVertexSelection);
                row.SetActive(isShown);
                isAnyShown |= isShown;
            }

            if (panel != null && panel.activeSelf != isAnyShown) { panel.SetActive(isAnyShown); }
        }

        /// <summary>
        /// その行を、今の状態で出すか
        /// </summary>
        /// <param name="rowEditModes">行を出すエディットモード</param>
        /// <param name="rowLayers">行を出すレイヤー</param>
        /// <param name="rowCondition">行を出す、追加の条件</param>
        /// <param name="rowPreviewStates">行を出す、全画面プレビューの状態 (0 のときは、どの状態でも出す)</param>
        /// <param name="hasVertexSelection">全画面プレビューで、頂点を選んでいるか</param>
        public static bool IsRowShown(
            EditMode[] rowEditModes, DescriptionLayers rowLayers, DescriptionCondition rowCondition, DescriptionPreviewStates rowPreviewStates,
            EditMode editMode, EditNoteType layer, bool isHoldIsolation, DescriptionPreviewStates previewState, bool hasVertexSelection)
        {
            if (rowEditModes == null || !editMode.IsInEditModeList(rowEditModes)) { return false; }
            if ((rowLayers & ToDescriptionLayer(layer)) == 0) { return false; }
            if (rowPreviewStates != 0 && (rowPreviewStates & previewState) == 0) { return false; }

            switch (rowCondition)
            {
                case DescriptionCondition.OnlyInHoldIsolation: return isHoldIsolation;
                case DescriptionCondition.NotInHoldIsolation: return !isHoldIsolation;
                case DescriptionCondition.OnlyWithPreviewVertexSelection: return hasVertexSelection;
                case DescriptionCondition.NotWithPreviewVertexSelection: return !hasVertexSelection;
                default: return true;
            }
        }

        /// <summary>
        /// レイヤーを、操作説明の行の「出すレイヤー」の形にする
        /// </summary>
        public static DescriptionLayers ToDescriptionLayer(EditNoteType layer)
        {
            switch (layer)
            {
                case EditNoteType.Ground: return DescriptionLayers.Ground;
                case EditNoteType.Space: return DescriptionLayers.Space;
                case EditNoteType.Vertices: return DescriptionLayers.Vertices;
                case EditNoteType.Preview: return DescriptionLayers.Preview;
                default: return 0;
            }
        }

        #region 実行時に足す行

        // 行の中の部品の名前 (シーンの行と同じ)
        const string ROW_KEY_NAME = "Key0";
        const string ROW_TEXT_NAME = "Text";
        // キーの四角の、文字の左右の余白と、いちばん小さい幅
        const float KEY_PADDING = 12f;
        const float KEY_MIN_WIDTH = 24f;

        /// <summary>
        /// シーンにある行 (template) を複製して、行を足す (実行時に足す画面の操作の説明用。SpaceHold の3D編集など)
        /// 複製した行は、キーの四角 (Key0) と説明 (Text) だけを残し、ほかの絵は消す
        /// </summary>
        /// <param name="templateRowName">複製する行の名前 (キーの四角と説明だけの行。例: Undo)</param>
        /// <param name="rowName">足す行の名前</param>
        /// <param name="keyText">キーの四角に書く文字</param>
        /// <param name="description">説明</param>
        /// <param name="editModes">この行を出すエディットモード</param>
        /// <param name="layers">この行を出すレイヤー</param>
        /// <returns>足せたか (複製する行が無いときは false)</returns>
        public bool AddRuntimeRow(string templateRowName, string rowName, string keyText, string description, EditMode[] editModes, DescriptionLayers layers)
        {
            if (rows == null) { return false; }

            DescriptionRow template = Array.Find(rows, row => row != null && row.Name == templateRowName);
            if (template == null || template.Obj == null) { return false; }

            GameObject clone = Instantiate(template.Obj, template.Obj.transform.parent);
            clone.name = rowName;
            clone.transform.SetAsLastSibling();

            Transform key = clone.transform.Find(ROW_KEY_NAME);
            Transform text = clone.transform.Find(ROW_TEXT_NAME);

            // キーの四角と説明のほかの絵 (マウスの絵、×2 など) は消す
            foreach (Transform child in clone.transform)
            {
                if (child != key && child != text) { Destroy(child.gameObject); }
            }

            if (text != null && text.TryGetComponent(out TMPro.TMP_Text descriptionLabel)) { descriptionLabel.text = description; }

            if (key != null)
            {
                var keyLabel = key.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (keyLabel != null)
                {
                    keyLabel.text = keyText;
                    // 文字が収まるように、キーの四角の幅を合わせる
                    if (key is RectTransform keyRect)
                    {
                        float width = Mathf.Max(KEY_MIN_WIDTH, keyLabel.GetPreferredValues(keyText).x + KEY_PADDING);
                        keyRect.sizeDelta = new Vector2(width, keyRect.sizeDelta.y);
                    }
                }
            }

            var added = new DescriptionRow(rowName, clone, editModes, layers);
            var list = new System.Collections.Generic.List<DescriptionRow>(rows) { added };
            rows = list.ToArray();

            clone.SetActive(false);
            Refresh();
            return true;
        }

        /// <summary>
        /// シーンにある行を、ほかのエディットモードでも出す (同じ操作を、別の画面でも使えるとき。元に戻す、再生など)
        /// </summary>
        public void AddEditModeToRows(EditMode editMode, params string[] rowNames)
        {
            if (rows == null || rowNames == null) { return; }

            foreach (var row in rows)
            {
                if (row != null && Array.IndexOf(rowNames, row.Name) >= 0) { row.AddEditMode(editMode); }
            }

            Refresh();
        }

        #endregion

        [Serializable]
        class DescriptionRow
        {
            public DescriptionRow() { }

            public DescriptionRow(string name, GameObject obj, EditMode[] editModes, DescriptionLayers layers)
            {
                this.name = name;
                this.obj = obj;
                this.editModes = editModes;
                this.layers = layers;
                condition = DescriptionCondition.Always;
            }

            public string Name => name;
            public GameObject Obj => obj;

            public void AddEditMode(EditMode editMode)
            {
                if (editModes != null && Array.IndexOf(editModes, editMode) >= 0) { return; }

                var list = new System.Collections.Generic.List<EditMode>(editModes ?? Array.Empty<EditMode>()) { editMode };
                editModes = list.ToArray();
            }

            [SerializeField] string name = "row";
            [SerializeField] GameObject obj;
            [Tooltip("この行を出すエディットモード")]
            [SerializeField] EditMode[] editModes;
            [Tooltip("この行を出すレイヤー")]
            [SerializeField] DescriptionLayers layers = DescriptionLayers.Ground | DescriptionLayers.Space;
            [Tooltip("この行を出す、追加の条件")]
            [SerializeField] DescriptionCondition condition;
            [Tooltip("この行を出す、全画面プレビューの状態 (何も選ばなければ、どの状態でも出す)")]
            [SerializeField] DescriptionPreviewStates previewStates;

            public bool IsShown(EditMode editMode, EditNoteType layer, bool isHoldIsolation, DescriptionPreviewStates previewState, bool hasVertexSelection)
            {
                return IsRowShown(editModes, layers, condition, previewStates, editMode, layer, isHoldIsolation, previewState, hasVertexSelection);
            }

            public void SetActive(bool isActive)
            {
                if (obj != null && obj.activeSelf != isActive) { obj.SetActive(isActive); }
            }
        }
    }

}

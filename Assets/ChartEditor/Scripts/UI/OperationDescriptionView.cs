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
    /// 行は、はじめからシーンに置いてある (SpaceHold の3D編集の行も: エディットモード SpaceHoldView の行)。ここでは、今の状態で使える行だけを出す。
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

        [Serializable]
        class DescriptionRow
        {
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

            /// <summary>行の名前 (インスペクターで見分けるため)</summary>
            public string Name => name;

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

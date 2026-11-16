using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold の3D編集の、見た目 (カメラ・帯の面・線・つまみ・欄) を作って持っておく
    ///
    /// ・3Dのものは、ほかのカメラに映らないよう、遠く離れた場所 (ROOT_POSITION) に、専用のレイヤーで置く。
    ///   専用のカメラで映し、その絵を、画面いっぱいの画面部品 (RawImage) に貼る (編集画面の部品の上に重ねるため)
    /// ・つまみ (頂点・中心・矢印・制御点の印) は、3Dの中ではなく、画面に重ねた板として描く (遠くても同じ大きさで見えるように)
    /// ・線・つまみ・文字は、フレームごとに BeginFrame → 使う分だけ取り出す → EndFrame (余りを隠す)
    /// 画面部品は、シーンには置かず、ここでコードで作る (EditorRuntimeUi)。
    /// </summary>
    public class SpaceHold3DView : IDisposable
    {
        /// <summary>3Dのものを置く場所 (ほかのカメラに映らない、遠く離れた場所)</summary>
        public static readonly Vector3 ROOT_POSITION = new Vector3(0f, -20000f, 0f);
        /// <summary>3Dのもののレイヤー (TagManager の 20番。名前が無くても使える)</summary>
        const int VIEW_LAYER = 20;
        // 画面部品の重なり順 (EditorCanvas 0、フィルムストリップ 1 より手前、メニューバー 10 より奥)
        const int SORTING_ORDER = 5;
        // メニューバーの高さ (メニューバーは、いつも見えるようにしておく)
        const float MENU_BAR_HEIGHT = 34f;

        static readonly Color backgroundColor = new Color(0.075f, 0.08f, 0.095f, 1f);

        public Transform Root { get; private set; }
        public Camera Camera { get; private set; }
        public RectTransform Viewport { get; private set; }
        public Canvas Canvas { get; private set; }

        public TextMeshProUGUI InfoLabel { get; private set; }
        public TextMeshProUGUI StatusLabel { get; private set; }

        RawImage viewportImage;
        // 画面部品の入れ物 (下地。欄やつまみは、この中に置く)
        RectTransform uiRoot;
        RectTransform handleLayer;
        RenderTexture renderTexture;

        MeshFilter tunnelFilter;
        MeshFilter contextFilter;
        Mesh tunnelMesh;
        Mesh contextMesh;
        Material tunnelMaterial;
        Material contextMaterial;
        Material lineMaterial;

        readonly List<LineRenderer> lines = new List<LineRenderer>();
        int usedLineCount;
        readonly List<Image> handles = new List<Image>();
        int usedHandleCount;
        readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        int usedLabelCount;

        PointerEventData pointerData;
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        public bool IsBuilt => Canvas != null;

        /// <summary>
        /// 映している範囲の大きさ (画面部品の単位。1920×1080 基準)
        /// </summary>
        public Vector2 ViewportSize => Viewport != null ? Viewport.rect.size : Vector2.one;

        #region 作る・消す

        public void Build()
        {
            // 3Dのもの
            var rootObject = new GameObject("SpaceHold3DViewRoot");
            rootObject.layer = VIEW_LAYER;
            Root = rootObject.transform;
            Root.position = ROOT_POSITION;

            tunnelMaterial = EditorLineMaterials.Create(3000, false, new Color(0.35f, 0.72f, 1f, 0.22f));
            contextMaterial = EditorLineMaterials.Create(2990, false, new Color(0.75f, 0.75f, 0.78f, 0.10f));
            lineMaterial = EditorLineMaterials.Create(3100, false);

            tunnelFilter = CreateMeshObject("Tunnel", tunnelMaterial, out tunnelMesh);
            contextFilter = CreateMeshObject("Others", contextMaterial, out contextMesh);

            var cameraObject = new GameObject("SpaceHold3DCamera");
            cameraObject.layer = VIEW_LAYER;
            Camera = cameraObject.AddComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = backgroundColor;
            Camera.cullingMask = 1 << VIEW_LAYER;
            Camera.depth = -50f;
            Camera.allowHDR = false;
            Camera.allowMSAA = true;
            Camera.enabled = false;

            // 画面部品
            Canvas = EditorRuntimeUi.CreateOverlayCanvas("SpaceHold3DCanvas", SORTING_ORDER);

            // 下地 (不透明)。3Dの絵には、半透明の面や線のところに透けた部分ができるので、
            // 下地を敷いておかないと、裏の編集画面 (頂点レイヤーなど) が透けて見える
            var root = EditorRuntimeUi.CreateImage("Root", Canvas.transform, backgroundColor, true).rectTransform;
            EditorRuntimeUi.Stretch(root, 0f, 0f, 0f, MENU_BAR_HEIGHT);
            uiRoot = root;

            var viewportRect = EditorRuntimeUi.CreateRect("Viewport", root);
            EditorRuntimeUi.Stretch(viewportRect);
            viewportImage = viewportRect.gameObject.AddComponent<RawImage>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;
            Viewport = viewportRect;

            handleLayer = EditorRuntimeUi.CreateRect("Handles", root);
            EditorRuntimeUi.Stretch(handleLayer, 0f, 0f, 0f, 0f);

            // 文字 (下のまんなか: 選んでいる節と、その上に、最後の操作)
            // (左下にはクイックメニュー、右には操作説明の欄が、手前に出る)
            InfoLabel = EditorRuntimeUi.CreateLabel("Info", root, string.Empty, 15f, TextAlignmentOptions.Bottom);
            EditorRuntimeUi.Place(InfoLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(1000f, 24f));

            StatusLabel = EditorRuntimeUi.CreateLabel("Status", root, string.Empty, 14f, TextAlignmentOptions.Bottom, new Color(1f, 0.85f, 0.4f, 1f));
            EditorRuntimeUi.Place(StatusLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(1000f, 22f));

            var banner = EditorRuntimeUi.CreateLabel("Banner", root, "SpaceHold を1本だけ編集中 (ほかのノーツは灰色)   Esc: 閉じる", 14f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.75f));
            EditorRuntimeUi.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(900f, 22f));

            SetVisible(false);
        }

        private MeshFilter CreateMeshObject(string name, Material material, out Mesh mesh)
        {
            var obj = new GameObject(name);
            obj.layer = VIEW_LAYER;
            obj.transform.SetParent(Root, false);

            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            var filter = obj.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return filter;
        }

        public void Dispose()
        {
            if (Root != null) { UnityEngine.Object.Destroy(Root.gameObject); }
            if (Camera != null) { UnityEngine.Object.Destroy(Camera.gameObject); }
            if (Canvas != null) { UnityEngine.Object.Destroy(Canvas.gameObject); }
            if (renderTexture != null) { renderTexture.Release(); UnityEngine.Object.Destroy(renderTexture); }
            if (tunnelMesh != null) { UnityEngine.Object.Destroy(tunnelMesh); }
            if (contextMesh != null) { UnityEngine.Object.Destroy(contextMesh); }
            if (tunnelMaterial != null) { UnityEngine.Object.Destroy(tunnelMaterial); }
            if (contextMaterial != null) { UnityEngine.Object.Destroy(contextMaterial); }
            if (lineMaterial != null) { UnityEngine.Object.Destroy(lineMaterial); }

            Root = null;
            Camera = null;
            Canvas = null;
            renderTexture = null;
        }

        public void SetVisible(bool isVisible)
        {
            if (Canvas != null && Canvas.gameObject.activeSelf != isVisible) { Canvas.gameObject.SetActive(isVisible); }
            if (Root != null && Root.gameObject.activeSelf != isVisible) { Root.gameObject.SetActive(isVisible); }
            if (Camera != null) { Camera.enabled = isVisible; }

            // 出したフレームのうちに、画面部品の大きさを決めておく (映す範囲の大きさを、すぐに使うため)
            if (isVisible) { UnityEngine.Canvas.ForceUpdateCanvases(); }
        }

        #endregion

        #region 欄 (視点の欄・節の欄)

        const float PANEL_PADDING = 8f;
        const float PANEL_SPACING = 5f;
        public const float BUTTON_HEIGHT = 28f;

        /// <summary>
        /// 縦に部品を並べる欄を作る (高さは、中身に合わせて伸びる)
        /// </summary>
        /// <param name="anchor">欄を寄せる角 (左上なら (0, 1))</param>
        /// <param name="position">角からの位置</param>
        /// <param name="width">欄の幅</param>
        public RectTransform AddPanel(string name, Vector2 anchor, Vector2 position, float width)
        {
            var panel = EditorRuntimeUi.CreatePanel(name, uiRoot).rectTransform;
            EditorRuntimeUi.Place(panel, anchor, position, new Vector2(width, 0f));

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)PANEL_PADDING, (int)PANEL_PADDING, (int)PANEL_PADDING, (int)PANEL_PADDING);
            layout.spacing = PANEL_SPACING;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // 下地の内側の部品 (EditorRuntimeUi.CreatePanel が作る) は、並べる対象から外す
            foreach (Transform child in panel)
            {
                var ignore = child.gameObject.AddComponent<LayoutElement>();
                ignore.ignoreLayout = true;
            }

            return panel;
        }

        /// <summary>
        /// 欄の見出し
        /// </summary>
        public TextMeshProUGUI AddPanelTitle(RectTransform panel, string text)
        {
            var label = EditorRuntimeUi.CreateLabel("Title", panel, text, 14f, TextAlignmentOptions.MidlineLeft, EditorRuntimeUi.SubTextColor);
            SetHeight(label.gameObject, 20f);
            return label;
        }

        /// <summary>
        /// 欄の中の、横に部品を並べる行
        /// </summary>
        public RectTransform AddPanelRow(RectTransform panel, float height = BUTTON_HEIGHT)
        {
            var row = EditorRuntimeUi.CreateRect("Row", panel);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = PANEL_SPACING;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            SetHeight(row.gameObject, height);
            return row;
        }

        /// <summary>
        /// 行にボタンを足す (行の中のボタンは、同じ幅に並ぶ。flexible を大きくすると、そのボタンが広くなる)
        /// </summary>
        public EditorUiButton AddRowButton(RectTransform row, string text, Action onClick, float flexible = 1f)
        {
            var button = EditorRuntimeUi.CreateButton(text, row, text, new Vector2(10f, BUTTON_HEIGHT), onClick, 13.5f);
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.minWidth = 10f;
            element.flexibleWidth = flexible;
            element.preferredHeight = BUTTON_HEIGHT;
            return button;
        }

        /// <summary>
        /// 行に文字を足す (幅を決めないときは、残りの幅に広がる)
        /// </summary>
        public TextMeshProUGUI AddRowLabel(RectTransform row, string text, float width = -1f)
        {
            var label = EditorRuntimeUi.CreateLabel("Label", row, text, 13.5f, TextAlignmentOptions.Center, EditorRuntimeUi.SubTextColor);
            var element = label.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                element.preferredWidth = width;
                element.flexibleWidth = 0f;
            }
            else
            {
                element.flexibleWidth = 1f;
            }
            return label;
        }

        /// <summary>
        /// 欄の中に、決まった高さの空きを作る (向きの印などを置く場所)
        /// </summary>
        public RectTransform AddPanelSpace(RectTransform panel, float height)
        {
            var space = EditorRuntimeUi.CreateRect("Space", panel);
            SetHeight(space.gameObject, height);
            return space;
        }

        private static void SetHeight(GameObject target, float height)
        {
            // (Unity の部品は ?? で比べられないので、if で確かめる)
            var element = target.GetComponent<LayoutElement>();
            if (element == null) { element = target.AddComponent<LayoutElement>(); }
            element.preferredHeight = height;
            element.minHeight = height;
        }

        #endregion

        #region 画面の位置

        /// <summary>
        /// 絵を描く先 (RenderTexture) を、映している範囲の大きさに合わせる
        /// </summary>
        public void UpdateRenderTarget()
        {
            if (Viewport == null || Camera == null) { return; }

            float scale = Canvas != null && Canvas.scaleFactor > 0f ? Canvas.scaleFactor : 1f;
            Vector2 size = Viewport.rect.size * scale;
            int width = Mathf.Max(16, Mathf.RoundToInt(size.x));
            int height = Mathf.Max(16, Mathf.RoundToInt(size.y));

            if (renderTexture != null && renderTexture.width == width && renderTexture.height == height) { return; }

            if (renderTexture != null)
            {
                Camera.targetTexture = null;
                renderTexture.Release();
                UnityEngine.Object.Destroy(renderTexture);
            }

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "SpaceHold3DView",
                antiAliasing = 4,
                hideFlags = HideFlags.HideAndDontSave,
            };
            renderTexture.Create();
            Camera.targetTexture = renderTexture;
            viewportImage.texture = renderTexture;
        }

        /// <summary>
        /// 画面上の位置 (ピクセル) を、映している範囲の中の位置 (左下が原点。画面部品の単位) に直す
        /// </summary>
        public bool TryGetViewportLocal(Vector2 screenPosition, out Vector2 local)
        {
            local = default;
            if (Viewport == null) { return false; }
            if (!EditorRuntimeUi.TryGetLocalPoint(Viewport, screenPosition, out Vector2 point)) { return false; }

            Rect rect = Viewport.rect;
            local = point - rect.min;
            return local.x >= 0f && local.y >= 0f && local.x <= rect.width && local.y <= rect.height;
        }

        /// <summary>
        /// 映している範囲の中の位置を通る、3Dの光線
        /// </summary>
        public Ray GetRay(Vector2 local)
        {
            Vector2 size = ViewportSize;
            return Camera.ViewportPointToRay(new Vector3(local.x / Mathf.Max(1f, size.x), local.y / Mathf.Max(1f, size.y), 0f));
        }

        /// <summary>
        /// 3Dの位置が、映している範囲のどこに映るか (カメラの後ろにあるときは false)
        /// </summary>
        public bool TryProject(Vector3 world, out Vector2 local)
        {
            Vector3 viewportPoint = Camera.WorldToViewportPoint(world);
            Vector2 size = ViewportSize;
            local = new Vector2(viewportPoint.x * size.x, viewportPoint.y * size.y);
            return viewportPoint.z > 0f;
        }

        /// <summary>
        /// カーソルが、ツールバーなどの部品ではなく、3Dを映している所の上にあるか (メニューの一覧などが上に重なっていないか)
        /// </summary>
        public bool IsPointerOnViewport(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || viewportImage == null) { return true; }

            pointerData ??= new PointerEventData(eventSystem);
            pointerData.position = screenPosition;
            raycastResults.Clear();
            eventSystem.RaycastAll(pointerData, raycastResults);
            return raycastResults.Count > 0 && raycastResults[0].gameObject == viewportImage.gameObject;
        }

        #endregion

        #region 面

        public void SetTunnelMesh(List<Vector3> vertices, List<int> triangles) => SetMesh(tunnelMesh, vertices, triangles);

        public void SetContextMesh(List<Vector3> vertices, List<int> triangles) => SetMesh(contextMesh, vertices, triangles);

        readonly List<Color32> meshColors = new List<Color32>();

        private void SetMesh(Mesh mesh, List<Vector3> vertices, List<int> triangles)
        {
            if (mesh == null) { return; }

            mesh.Clear();
            if (vertices == null || triangles == null || vertices.Count == 0 || triangles.Count == 0) { return; }

            // 色はマテリアルで付ける (頂点の色は白にしておく)
            meshColors.Clear();
            for (int i = 0; i < vertices.Count; i++) { meshColors.Add(new Color32(255, 255, 255, 255)); }

            mesh.SetVertices(vertices);
            mesh.SetColors(meshColors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        #endregion

        #region 線・つまみ・文字 (フレームごとに、使う分だけ取り出す)

        public void BeginFrame()
        {
            usedLineCount = 0;
            usedHandleCount = 0;
            usedLabelCount = 0;
        }

        public void EndFrame()
        {
            for (int i = usedLineCount; i < lines.Count; i++)
            {
                if (lines[i] != null && lines[i].enabled) { lines[i].enabled = false; }
            }
            for (int i = usedHandleCount; i < handles.Count; i++)
            {
                if (handles[i] != null && handles[i].enabled) { handles[i].enabled = false; }
            }
            for (int i = usedLabelCount; i < labels.Count; i++)
            {
                if (labels[i] != null && labels[i].enabled) { labels[i].enabled = false; }
            }
        }

        /// <summary>
        /// 線を1本描く (位置は、3Dのものを置く場所 (Root) の中の位置)
        /// </summary>
        public void DrawLine(IReadOnlyList<Vector3> points, Color color, float width, bool isLoop)
        {
            if (points == null || points.Count < 2) { return; }

            LineRenderer line = GetLine();
            line.loop = isLoop;
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) { line.SetPosition(i, points[i]); }
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
            line.enabled = true;
        }

        private LineRenderer GetLine()
        {
            if (usedLineCount < lines.Count) { return lines[usedLineCount++]; }

            var obj = new GameObject("Line");
            obj.layer = VIEW_LAYER;
            obj.transform.SetParent(Root, false);

            var line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = lineMaterial;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            lines.Add(line);
            usedLineCount++;
            return line;
        }

        /// <summary>
        /// つまみを1つ描く (位置は、映している範囲の中の位置)
        /// </summary>
        /// <param name="isRound">丸 (false のときは四角)</param>
        /// <param name="rotation">回す角度 (度。45 でひし形)</param>
        public void DrawHandle(Vector2 local, float size, Color color, bool isRound, float rotation = 0f)
        {
            Image handle;
            if (usedHandleCount < handles.Count)
            {
                handle = handles[usedHandleCount];
            }
            else
            {
                handle = EditorRuntimeUi.CreateImage("Handle", handleLayer, Color.white);
                var rect = handle.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                handles.Add(handle);
            }
            usedHandleCount++;

            Sprite sprite = isRound ? EditorRuntimeUi.CircleSprite : null;
            if (handle.sprite != sprite) { handle.sprite = sprite; }
            handle.color = color;
            handle.rectTransform.anchoredPosition = local;
            handle.rectTransform.sizeDelta = new Vector2(size, size);
            handle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            if (!handle.enabled) { handle.enabled = true; }
        }

        /// <summary>
        /// 文字を1つ描く (位置は、映している範囲の中の位置。文字の左下)
        /// </summary>
        public void DrawLabel(Vector2 local, string text, float fontSize, Color color)
        {
            TextMeshProUGUI label;
            if (usedLabelCount < labels.Count)
            {
                label = labels[usedLabelCount];
            }
            else
            {
                label = EditorRuntimeUi.CreateLabel("Label", handleLayer, string.Empty, fontSize, TextAlignmentOptions.BottomLeft);
                var rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(220f, 20f);
                labels.Add(label);
            }
            usedLabelCount++;

            if (label.text != text) { label.text = text; }
            label.fontSize = fontSize;
            label.color = color;
            label.rectTransform.anchoredPosition = local;
            if (!label.enabled) { label.enabled = true; }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 頂点レイヤーのオニオンスキン
    ///
    /// SpaceHold の節の頂点を編集している間、同じ SpaceHold の前後の節の形 (輪郭) を、薄い線で重ねて表示する。
    /// 今の節の形が、前後とくらべてどれくらい変わっているかが分かる。
    /// ・前の節は青、後ろの節は橙。今の節から離れるほど薄くなる
    /// ・前後それぞれ何枚まで出すかは、メニューの「表示」→「オニオンスキン」で選ぶ (0 で出さない。最初は3枚)
    /// ・線は、ほかのものに隠れないように、いちばん手前に描く。クリックは受け取らない
    ///
    /// 線は、頂点と同じ親 (頂点レイヤーの頂点を並べる場所) の下に置き、頂点と同じ式で位置を求める。
    /// </summary>
    public class VerticesOnionSkin : IDisposable
    {
        static readonly Color previousColor = new Color(0.35f, 0.75f, 1f, 1f);
        static readonly Color nextColor = new Color(1f, 0.62f, 0.25f, 1f);
        // (録音 20261118_202528: 濃すぎたので、半分ほどに)
        const float NEAREST_ALPHA = 0.4f;
        const float FARTHEST_ALPHA = 0.1f;
        // 線の太さ (頂点を置ける範囲の横幅に対する割合)
        const float LINE_WIDTH_RATIO = 0.006f;
        // ほかのもの (頂点・面) よりも後に描く
        const int RENDER_QUEUE = 3500;

        readonly Transform parent;
        readonly Func<Vector2, Vector2> toLocalPosition;
        readonly float lineWidth;
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        readonly List<NoteData_SpaceHold> chainBuffer = new List<NoteData_SpaceHold>();
        Material material;
        int usedCount;

        /// <param name="parent">線を置く親 (頂点と同じ親)</param>
        /// <param name="toLocalPosition">頂点の座標 (単位円の中の値) から、親の中での位置を求める処理</param>
        /// <param name="areaWidth">頂点を置ける範囲の横幅 (線の太さを決めるのに使う)</param>
        public VerticesOnionSkin(Transform parent, Func<Vector2, Vector2> toLocalPosition, float areaWidth)
        {
            this.parent = parent;
            this.toLocalPosition = toLocalPosition;
            lineWidth = Mathf.Max(0.001f, Mathf.Abs(areaWidth) * LINE_WIDTH_RATIO);
        }

        /// <summary>
        /// 編集中の節に合わせて、前後の節の輪郭を描き直す (毎フレーム呼ぶ)
        /// </summary>
        /// <param name="editing">頂点を編集しているノーツ</param>
        /// <param name="count">前後それぞれ何枚まで出すか</param>
        /// <param name="isVisible">頂点レイヤーを表示しているか</param>
        public void Refresh(INotesDataGetter notesGetter, IVerticesControlableNoteData editing, int count, bool isVisible)
        {
            usedCount = 0;

            if (isVisible && count > 0 && parent != null && editing is NoteData_SpaceHold spaceHold)
            {
                SpaceHoldSections.GetChain(notesGetter, spaceHold, chainBuffer);
                int index = chainBuffer.IndexOf(spaceHold);
                if (index >= 0)
                {
                    // 遠いものから描く (近いものが上に重なるように)
                    for (int distance = count; distance >= 1; distance--)
                    {
                        float alpha = count <= 1 ? NEAREST_ALPHA : Mathf.Lerp(NEAREST_ALPHA, FARTHEST_ALPHA, (distance - 1) / (float)(count - 1));
                        if (index - distance >= 0) { Draw(chainBuffer[index - distance], WithAlpha(previousColor, alpha)); }
                        if (index + distance < chainBuffer.Count) { Draw(chainBuffer[index + distance], WithAlpha(nextColor, alpha)); }
                    }
                }
                chainBuffer.Clear();
            }

            // 使わなかった線を隠す
            for (int i = usedCount; i < lines.Count; i++)
            {
                if (lines[i] != null && lines[i].enabled) { lines[i].enabled = false; }
            }
        }

        private void Draw(NoteData_SpaceHold note, Color color)
        {
            var vertices = note.SpaceVertices.Vertices;
            if (vertices.Count < 2) { return; }

            LineRenderer line = GetLine();
            line.positionCount = vertices.Count;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 local = toLocalPosition(vertices[i].Position.Value);
                line.SetPosition(i, new Vector3(local.x, local.y, 0f));
            }

            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = lineWidth;
            line.enabled = true;
        }

        private LineRenderer GetLine()
        {
            if (usedCount < lines.Count && lines[usedCount] != null)
            {
                return lines[usedCount++];
            }

            material ??= EditorLineMaterials.Create(RENDER_QUEUE, true);

            var obj = new GameObject("OnionSkinLine");
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            var line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.sharedMaterial = material;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            if (usedCount < lines.Count) { lines[usedCount] = line; }
            else { lines.Add(line); }

            usedCount++;
            return line;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public void Dispose()
        {
            foreach (var line in lines)
            {
                if (line != null) { UnityEngine.Object.Destroy(line.gameObject); }
            }
            lines.Clear();

            if (material != null) { UnityEngine.Object.Destroy(material); }
            material = null;
        }
    }

    /// <summary>
    /// 譜面エディタで、線や半透明の面を描くためのマテリアル (光の当たり方を計算しない、頂点の色そのままの見た目)
    /// </summary>
    public static class EditorLineMaterials
    {
        static Shader shader;

        /// <param name="renderQueue">描く順番 (大きいほど後に描く)</param>
        /// <param name="isAlwaysOnTop">ほかのものの奥にあっても、隠れずに描くか</param>
        /// <param name="color">頂点の色に掛ける色 (指定しないときは白)</param>
        public static Material Create(int renderQueue, bool isAlwaysOnTop, Color? color = null)
        {
            // Unity に最初から入っている、色だけを描くシェーダー (ビルドにも必ず含まれる)
            shader ??= Shader.Find("Hidden/Internal-Colored");

            var material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = renderQueue,
            };
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_ZTest", (int)(isAlwaysOnTop ? UnityEngine.Rendering.CompareFunction.Always : UnityEngine.Rendering.CompareFunction.LessEqual));
            material.SetColor("_Color", color ?? Color.white);
            return material;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    /// <summary>
    /// スペースホールドの頂点リスト
    /// </summary>
    public class SpaceHoldVertices
    {
        Vector2[] defaultPositions = new Vector2[]
        {
            new Vector2(-0.25f, -0.5f),
            new Vector2(-0.25f, 0f),
            new Vector2(0.25f, 0f),
            new Vector2(0.25f, -0.5f)
        };

        // 頂点リスト
        ReactiveCollection<VertexData> vertices = new ReactiveCollection<VertexData>();
        public IReadOnlyReactiveCollection<VertexData> Vertices => vertices;

        public SpaceHoldVertices()
        {
            SetVertices(defaultPositions);
        }

        public void AddVertex(VertexData addVertex)
        {
            // 新規追加
            if(addVertex.VertexIndex == -1)
            {
                InsertVertexNearByNearestIndex(addVertex);
            }
            // Undoで元のデータが存在するとき
            else
            {
                InsertVertexAfterSpecific(addVertex, addVertex.VertexIndex);
            }
        }

        /// <summary>
        /// 頂点の新規追加
        /// </summary>
        /// <param name="addVertex"></param>
        private void InsertVertexNearByNearestIndex(VertexData addVertex)
        {
            // 1番目に近い頂点を見つける
            int nearestIndex = 0;
            float nearestSqrMagnitude = addVertex.GetSqrMagnitude(vertices[0]);

            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                float magnitude = addVertex.GetSqrMagnitude(v);
                // 距離短いの発見！更新！
                if (magnitude < nearestSqrMagnitude)
                {
                    nearestIndex = i;
                    nearestSqrMagnitude = magnitude;
                }
            }

            // 最も近い頂点の前後で、より追加頂点と近い頂点を調べる
            var backVertex = vertices[(nearestIndex + vertices.Count - 1) % vertices.Count];
            var nextVertex = vertices[(nearestIndex + 1) % vertices.Count];

            // 頂点の追加 
            if (addVertex.GetSqrMagnitude(nextVertex) < addVertex.GetSqrMagnitude(backVertex))
            {
                vertices.Insert(nearestIndex + 1, addVertex);
            }
            else
            {
                vertices.Insert(nearestIndex, addVertex);
            }

            UpdateVertexIndex();
        }

        /// <summary>
        /// 頂点の再追加
        /// </summary>
        /// <param name="addVertex"></param>
        /// <param name="backVertex"></param>
        private void InsertVertexAfterSpecific(VertexData addVertex, int index)
        {
            if(index < 0 || index > vertices.Count) 
            {
                Debug.LogWarning($"【Vertices】indexが範囲外のため新規追加します: {index}/{vertices.Count}");
                InsertVertexNearByNearestIndex(addVertex);
                return;
            }

            vertices.Insert(index, addVertex);
            UpdateVertexIndex();
        }

        /// <summary>
        /// 頂点インデックスを更新
        /// </summary>
        private void UpdateVertexIndex()
        {
            int i = 0;
            foreach(var v in vertices)
            {
                v.VertexIndex = i++;
            }
        }

        public bool RemoveVertex(VertexData vertex)
        {
            var b = vertices.Remove(vertex);
            UpdateVertexIndex();

            return b;
        }

        public void SetVertices(Vector2[] positions)
        {
            vertices.Clear();

            foreach (var pos in positions)
            {
                vertices.Add(new VertexData(pos));
            }

            UpdateVertexIndex();
        }

        public void SetVertices(List<VertexData> vertexDataList)
        {
            vertices.Clear();

            vertexDataList.Sort((a, b) => a.VertexIndex - b.VertexIndex);
            
            foreach(var v in vertexDataList)
            {
                AddVertex(v);
            }
        }

        public void ClearVertices()
        {
            vertices.Clear();
        }

        public void SlideVertexIndices(int delta)
        {
            vertices.Rotate(delta);
            UpdateVertexIndex();
        }

        public void ReverseVertices(Vector2 linePointA, Vector2 linePointB)
        {
            foreach (var vertex in vertices)
            {
                var pos = vertex.Position.Value.Mirror(linePointA, linePointB);
                vertex.SetPosition(pos);
            }

            // 順番を逆転させる
            vertices.ReverseElements();
            UpdateVertexIndex();
        }

        public SimpleVector2[] GetVertexArray()
        {
            return Vertices.Select(x => new SimpleVector2(x.Position.Value)).ToArray();
        }
    }

    /// <summary>
    /// スペースホールドの頂点
    /// </summary>
    public class VertexData
    {
        public VertexData(Vector2 pos)
        {
            SetPosition(pos);
        }

        public VertexData(VertexData vertex)
        {
            SetPosition(vertex.Position.Value);
            VertexIndex = vertex.VertexIndex;
        }

        /// <summary>
        /// 正規化後の頂点座標
        /// </summary>
        ReactiveProperty<Vector2> position = new ReactiveProperty<Vector2>();
        public IReadOnlyReactiveProperty<Vector2> Position => position;
        public void SetPosition(Vector2 pos)
        {
            // 単位円上に配置されるように正規化
            pos = pos.ClampToUnitCircle();

            position.Value = pos;
        }

        public float GetSqrMagnitude(VertexData another)
        {
            return (this.position.Value - another.position.Value).sqrMagnitude;
        }

        public int VertexIndex { get; set; } = -1;
    }

    public class VertexDataToPos
    {
        public VertexDataToPos(VertexData data,Vector2 pos)
        {
            this.Data = data;
            this.Pos = pos;
        }

        public VertexData Data { get; set; }
        public Vector2 Pos { get; set; }
    }

    [System.Serializable]
    public class ColorSetting
    {
        [SerializeField] Color color;
        [SerializeField] bool isBlinking; 

        public Color Color { get { return color; } set { color = value; } }
        public bool IsBlinking { get { return isBlinking; } set { isBlinking = value; } }
    }

    /// <summary>
    /// エディットモード一覧
    /// </summary>
    public enum EditMode
    {
        None = 0,
        Deploy = 10,
        Move = 20,
        Moving = 21,
        Scale = 30,
        Scaling = 31,
        Connect = 40,
        Connecting = 41,
        ChangeType = 50,

        SpaceDeploy = 100,
        SpaceMove = 110,
        SpaceEdit = 120,
        NoteSelect = 150,

        VertexDeploy = 200,
        VertexMove = 210,
        VertexMoving = 211,
        VerticesRotate = 220,
        VerticesRotating = 221,
        VerticesScale = 230,
        VerticesScaling = 231,

        EditBarConfig = 500,
        EditingBarConfig = 501,
        EditSubDivisionConfig = 510,
        EditingSubDivisionConfig = 511,

        Explanation = 1000,
    }

    /// <summary>
    /// エディットノーツタイプ
    /// </summary>
    public enum EditNoteType
    {
        Ground = 1,
        Space = 2,
        Vertices = 3,
    }

    /// <summary>
    /// 音楽再生モード
    /// </summary>
    public enum PlayMode
    {
        Stop,
        Play,
    }

    /// <summary>
    /// 配置ノーツ一覧
    /// </summary>
    public enum DeploymentNoteType
    {
        TouchNote = 10,

        DynamicGroundUpward = 100,
        DynamicGroundDownward = 110,
        DynamicGroundRightward = 120,
        DynamicGroundLeftward = 130,

        Hold = 510,
        HoldHidden = 520,
        HoldEndUnjudge = 530,

        SpaceHold = 800,
        SpaceHoldHidden = 810,
    }
}

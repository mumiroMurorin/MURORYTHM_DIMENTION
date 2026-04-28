using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using System;

namespace ChartEditor
{
    /// <summary>
    /// スペースホールドの頂点リスト
    /// </summary>
    public class SpaceVertices
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

        public SpaceVertices()
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

        ReactiveProperty<int> vertexIndex = new ReactiveProperty<int>(-1);
        public int VertexIndex { get { return vertexIndex.Value; } set { vertexIndex.Value = value; } }
        public IReadOnlyReactiveProperty<int> VertexIndexRP => vertexIndex;
    }

    public class NoteDataToAddress
    {
        public NoteDataToAddress(IDeployableNoteData noteData, AddressWithinRange address)
        {
            this.NoteData = noteData;
            this.Address = address;
        }

        public NoteDataToAddress(IDeployableNoteData noteData, IReadOnlyAddressWithinRange address)
        {
            this.NoteData = noteData;
            this.Address = new AddressWithinRange(address);
        }

        public IDeployableNoteData NoteData { get; set; }
        public AddressWithinRange Address { get; set; }
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

    public class BarConfig
    {
        public BarConfig(int beatCount, float beatUnit, int divNum) 
        {
            this.BeatCount = beatCount;
            this.BeatUnit = beatUnit;
            this.DivisionNum = divNum;
        }

        public int BeatCount { get; set; }

        public float BeatUnit { get; set; }

        public int DivisionNum { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is not BarConfig other)
                return false;

            return BeatCount == other.BeatCount &&
                   BeatUnit == other.BeatUnit &&
                   DivisionNum == other.DivisionNum;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BeatCount, BeatUnit, DivisionNum);
        }

        public static bool operator ==(BarConfig a, BarConfig b) => Equals(a, b);
        public static bool operator !=(BarConfig a, BarConfig b) => !Equals(a, b);
    }

    public class SubdivisionConfig
    {
        public SubdivisionConfig(float bpm, float speedRatio)
        {
            this.Bpm = bpm;
            this.SpeedRatio = speedRatio;
        }

        public SubdivisionConfig(SubdivisionConfig config)
        {
            if(config == null) { return; }

            this.Bpm = config.Bpm;
            this.SpeedRatio = config.SpeedRatio;
        }

        public float Bpm { get; set; }

        public float SpeedRatio { get; set; }
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
        DisConnect = 42,
        ChangeType = 50,

        SpaceDeploy = 100,
        SpaceMove = 110,
        SpaceMoving = 111,
        NoteSelect = 150,

        VertexDeploy = 200,
        VertexMove = 210,
        VertexMoving = 211,
        VerticesRotate = 220,
        VerticesRotating = 221,
        VerticesScale = 230,
        VerticesScaling = 231,
        VerticesSelect = 290,

        EditBarConfig = 500,
        EditingBarConfig = 501,
        EditSubDivisionConfig = 510,
        EditingSubDivisionConfig = 511,

        Explanation = 1000,
        Preview = 2000
    }

    /// <summary>
    /// エディットノーツタイプ
    /// </summary>
    public enum EditNoteType
    {
        Ground = 1,
        Space = 2,
        Vertices = 3,
        Preview = 100
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
    /// インデックスを明示的に宣言しないこと
    /// </summary>
    public enum Resolution
    {
        w1920_1080,
        w1280_720,
        fullScreen,
    }

    /// <summary>
    /// 配置ノーツ一覧
    /// </summary>
    public enum DeploymentNoteType
    {
        Touch = 10,
        DivineTouch = 11,

        DynamicGroundUpward = 100,
        DynamicGroundDownward = 110,
        DynamicGroundRightward = 120,
        DynamicGroundLeftward = 130,

        HoldStart = 510,
        HoldRelay = 520,
        HoldMeshRelay = 530,
        HoldEnd = 540,
        HoldEndUnjudge = 550,

        SpaceHoldStart = 800,
        SpaceHoldRelay = 810,
        SpaceHoldMeshRelay = 820,
        SpaceHoldEnd = 830,

        SpaceBreak = 900,
    }
}

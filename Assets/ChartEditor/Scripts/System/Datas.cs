using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    /// <summary>
    /// 分線のデータ
    /// </summary>
    public class SubDivisionDataInBeat
    {
        const int SPACE_LOCATION_INDEX = 100;

        public SubDivisionDataInBeat(float bpm, int barIndex, int subIndex)
        {
            SetBpm(bpm);
            BarIndex = barIndex;
            SubDivisionIndex = subIndex;
        }

        public int BarIndex { get; }
        public int SubDivisionIndex { get; }

        // Ground配置場所
        public Transform[] PlacementLocation { private get; set; }
        public void SetPlacementLocation(Transform[] locates)
        {
            PlacementLocation = locates;
        }

        // 宙配置場所
        public Transform SpaceLocation { private get; set; }
        public void SetSpaceLocation(Transform locate)
        {
            SpaceLocation = locate;
        }

        #region ノーツデータ

        /// <summary>
        /// その分線に配置されたノーツのデータ
        /// </summary>
        ReactiveCollection<IDeployableNoteData> noteDatas = new ReactiveCollection<IDeployableNoteData>();

        /// <summary>
        /// ノーツの追加、削除の監視用
        /// </summary>
        public IReadOnlyReactiveCollection<IDeployableNoteData> NoteDatas => noteDatas;

        public void AddNote(IDeployableNoteData noteData)
        {
            noteDatas.Add(noteData);
        }

        public bool RemoveNote(IDeployableNoteData noteData)
        {
            return noteDatas.Remove(noteData);
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            // 宙配置場所
            if(address.SliderIndex == SPACE_LOCATION_INDEX)
            {
                return SpaceLocation;
            }

            // グラウンド配置場所
            if(address.SliderIndex > 15) {
                Debug.LogError($"【System】値が15を超えています: {address.SliderIndex}");
                return null; 
            }

            return PlacementLocation[(int)address.SliderIndex];
        }

        #endregion

        #region BPM

        const float BPM_MIN = 1f;
        const float BPM_MAX = 500f;

        /// <summary>
        /// BPM変化
        /// </summary>
        ReactiveProperty<float> bpm = new ReactiveProperty<float>();

        public IReadOnlyReactiveProperty<float> Bpm => bpm;

        public void SetBpm(float bpm)
        {
            this.bpm.Value = Mathf.Clamp(bpm, BPM_MIN, BPM_MAX);
        }
        
        #endregion
    }

    /// <summary>
    /// 小節のデータ
    /// </summary>
    public class BarDataInChart
    {
        public BarDataInChart(int beatCount, float beatUnit, int divNum, float bpm, int barIndex)
        {
            this.beatCount.Value = beatCount;
            this.beatUnit.Value = beatUnit;
            this.divisionNum.Value = divNum;
            this.barIndex = barIndex;

            UpdateSubDivisionData(beatCount, beatUnit, divNum, bpm);
        }

        public int barIndex;

        /// <summary>
        /// 小節内の分線データ
        /// </summary>
        ReactiveCollection<SubDivisionDataInBeat> subDivisionDatas = new ReactiveCollection<SubDivisionDataInBeat>();

        /// <summary>
        /// 分線の代入、クリアの監視
        /// </summary>
        public IReadOnlyReactiveCollection<SubDivisionDataInBeat> SubDivisionDatas => subDivisionDatas;

        public void SetSubDivisionDatas(List<SubDivisionDataInBeat> subDivisionDatas)
        {
            this.subDivisionDatas.Clear();
            foreach (var sub in subDivisionDatas)
            {
                this.subDivisionDatas.Add(sub);
            }
        }

        /// <summary>
        /// 分線の数の更新
        /// </summary>
        /// <param name="beatCount">n分の</param>
        /// <param name="beatUnit">m拍子</param>
        private void UpdateSubDivisionData(int beatCount, float beatUnit, int divNum, float bpm = -1)
        {
            if (beatCount <= 0) { return; }
            if (beatUnit <= 0) { return; }
            if (divNum <= 0) { return; }
            if (bpm == -1 && this.subDivisionDatas.Count == 0) { return; }

            // bpmは以前のやつを使う
            if (bpm == -1) { bpm = this.subDivisionDatas[0].Bpm.Value; }
            List<SubDivisionDataInBeat> subDivisionDatas = new List<SubDivisionDataInBeat>();

            // カウント数 * 分割数が分線の数
            for (int i = 0; i < beatCount * divNum; i++)
            {
                subDivisionDatas.Add(new SubDivisionDataInBeat(bpm, barIndex, i));
            }

            // データセット
            SetSubDivisionDatas(subDivisionDatas);
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            if (address.SubDivisionIndex >= subDivisionDatas.Count)
            {
                // Debug.LogError($"【System】値が分線の数を超えています: {address.SubDivisionIndex}/{subDivisionDatas.Count}");
                return null;
            }

            return subDivisionDatas[address.SubDivisionIndex].GetPlacementLocation(address);
        }

        #region その他データ

        /// <summary>
        /// n/m拍子のn
        /// </summary>
        ReactiveProperty<int> beatCount = new ReactiveProperty<int>();

        public IReadOnlyReactiveProperty<int> BeatCount => beatCount;

        public void SetBeatCount(int beatCount) 
        {
            beatCount = (int)Mathf.Clamp(beatCount, 0, float.MaxValue);
            if(this.beatCount.Value == beatCount) { return; }

            this.beatCount.Value = beatCount;
            UpdateSubDivisionData(beatCount, this.beatUnit.Value, this.divisionNum.Value);
        }

        /// <summary>
        /// n/m拍子のm
        /// </summary>
        ReactiveProperty<float> beatUnit = new ReactiveProperty<float>();

        public IReadOnlyReactiveProperty<float> BeatUnit => beatUnit;

        public void SetBeatUnit(float beatUnit)
        {
            beatUnit = Mathf.Clamp(beatUnit, 0, float.MaxValue);
            if (this.beatUnit.Value == beatUnit) { return; }

            this.beatUnit.Value = beatUnit;
            //UpdateSubDivisionData(this.beatCount.Value, beatUnit, this.divisionNum.Value);
        }

        /// <summary>
        /// 分割数 (〇分割)
        /// </summary>
        ReactiveProperty<int> divisionNum = new ReactiveProperty<int>();

        public IReadOnlyReactiveProperty<int> DivisionNum => divisionNum;

        public void SetDivisionNum(int divisionNum)
        {
            divisionNum = (int)Mathf.Clamp(divisionNum, 0, float.MaxValue);
            if (this.divisionNum.Value == divisionNum) { return; }

            this.divisionNum.Value = divisionNum;
            UpdateSubDivisionData(this.beatCount.Value, this.beatUnit.Value, divisionNum);
        }

        #endregion
    }

    /// <summary>
    /// エディタ用譜面データ
    /// </summary>
    public class ChartData
    {
        const int DEFAULT_BEAT_COUNT = 4;
        const float DEFAULT_BEAT_UNIT = 4;
        const int DEFAULT_DIVISION_NUM = 2;
        const float DEFAULT_BPM = 256;

        public ChartData(int barNum)
        {
            for (int i = 0; i < barNum; i++)
            {
                BarDataInChart barData = new BarDataInChart(DEFAULT_BEAT_COUNT, DEFAULT_BEAT_UNIT, DEFAULT_DIVISION_NUM, DEFAULT_BPM, i);
                barDatas.Add(barData);
            }
        }

        ReactiveCollection<BarDataInChart> barDatas = new ReactiveCollection<BarDataInChart>();

        /// <summary>
        /// 譜面内の全小節データ
        /// </summary>
        public IReadOnlyReactiveCollection<BarDataInChart> BarDatas => barDatas;

        /// <summary>
        /// 特定の分線から後のBPMを一括で変更する
        /// </summary>
        /// <param name="subDivisionData"></param>
        public void SetBPMFromSubDivisionUnit(SubDivisionDataInBeat findData, float bpm)
        {
            if(barDatas == null) { return; }

            bool isFound = false;

            // 特定のデータをしらみつぶしに探す
            foreach (var bar in barDatas)
            {
                foreach (var sub in bar.SubDivisionDatas)
                {
                    // 見つかったらフラグオン
                    if (sub == findData) { isFound = true; }

                    // フラグオンならBPM変更
                    if (isFound) { sub.SetBpm(bpm); }
                }
            }
        }

        /// <summary>
        /// 小節線の追加
        /// </summary>
        /// <param name="length"></param>
        public void AddBar(int length)
        {
            float bpm = DEFAULT_BPM;
            int barDataCount = BarDatas.Count;

            if (BarDatas.Count > 0) { bpm = BarDatas.Last().SubDivisionDatas.Last().Bpm.Value; }

            for (int i = 0; i < length; i++)
            {
                BarDataInChart barData = new BarDataInChart(DEFAULT_BEAT_COUNT, DEFAULT_BEAT_UNIT, DEFAULT_DIVISION_NUM, bpm, barDataCount + i);
                barDatas.Add(barData);
            }
        }

        /// <summary>
        /// 小節線の削除
        /// </summary>
        /// <param name="length"></param>
        public void RemoveBar(int length)
        {
            for (int i = 0; i < Mathf.Min(length, BarDatas.Count); i++)
            {
                barDatas.RemoveAt(barDatas.Count - 1);
            }
        }

        /// <summary>
        /// ノーツを追加する
        /// </summary>
        public void AddNote(IDeployableNoteData noteData, AddressInChart address)
        {
            // 新たな場所に追加
            SubDivisionDataInBeat newSubDivision = BarDatas[address.BarIndex].SubDivisionDatas[address.SubDivisionIndex];
            newSubDivision.AddNote(noteData);

            noteData.SetAddress(address);
            Debug.Log($"【配置】:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
        }

        /// <summary>
        /// ノーツを削除する
        /// </summary>
        public void RemoveNote(IDeployableNoteData noteData)
        {
            AddressInChart address = noteData.Address;
            SubDivisionDataInBeat subDivision = barDatas[address.BarIndex].SubDivisionDatas[address.SubDivisionIndex];

            if (!subDivision.RemoveNote(noteData))
            {
                Debug.LogError($"【削除】削除に失敗しました:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
            }
            else
            {
                Debug.Log($"【削除】:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
            }
        }

        /// <summary>
        /// ノーツの場所を移動させる
        /// </summary>
        /// <param name="noteData"></param>
        /// <param name="newAddress"></param>
        /// <returns></returns>
        public bool ChangeNoteAddress(IDeployableNoteData noteData, AddressInChart newAddress)
        {
            AddressInChart oldAddress = noteData.Address;

            // 古い場所から削除
            SubDivisionDataInBeat oldSubDivision = BarDatas[oldAddress.BarIndex].SubDivisionDatas[oldAddress.SubDivisionIndex];
            if (!oldSubDivision.RemoveNote(noteData)) 
            {
                Debug.LogError($"【移動】削除に失敗しました\n 該当するノーツが見つかりません");
                return false; 
            }

            // 新たな場所に追加
            SubDivisionDataInBeat newSubDivision = BarDatas[newAddress.BarIndex].SubDivisionDatas[newAddress.SubDivisionIndex];
            newSubDivision.AddNote(noteData);

            noteData.SetAddress(newAddress);
            return true;
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            if (address.BarIndex >= barDatas.Count)
            {
                // Debug.LogError($"【System】値が小節線の数を超えています: {address.BarIndex}");
                return null;
            }

            return barDatas[address.BarIndex].GetPlacementLocation(address);
        }
    }

    /// <summary>
    /// 譜面中の「小節番号」「分節番号」「スライダーインデックス」をまとめたクラス
    /// </summary>
    public class AddressInChart
    {
        const int BAR_INDEX_DEFAULT = 0; 
        const int SUBDIVISION_INDEX_DEFAULT = 0; 
        const int SLIDER_INDEX_DEFAULT = 0; 

        public AddressInChart(int barIndex = BAR_INDEX_DEFAULT, int subDivisionIndex = SUBDIVISION_INDEX_DEFAULT, float sliderIndex = SLIDER_INDEX_DEFAULT)
        {
            this.barIndex = new ReactiveProperty<int>(barIndex);
            this.subDivisionIndex = new ReactiveProperty<int>(subDivisionIndex);
            this.sliderIndex = new ReactiveProperty<float>(sliderIndex);
        }

        public AddressInChart(AddressInChart address)
        {
            if(address == null) 
            {
                this.barIndex = new ReactiveProperty<int>(BAR_INDEX_DEFAULT);
                this.subDivisionIndex = new ReactiveProperty<int>(SUBDIVISION_INDEX_DEFAULT);
                this.sliderIndex = new ReactiveProperty<float>(SLIDER_INDEX_DEFAULT);
            }
            else
            {
                this.barIndex = new ReactiveProperty<int>(address.BarIndex);
                this.subDivisionIndex = new ReactiveProperty<int>(address.SubDivisionIndex);
                this.sliderIndex = new ReactiveProperty<float>(address.SliderIndex);
            }
        }

        /// <summary>
        /// 小節線番号
        /// </summary>
        ReactiveProperty<int> barIndex;
        public int BarIndex { get { return barIndex.Value; } }
        public IReadOnlyReactiveProperty<int> BarIndexRP { get { return barIndex; } }
        public void SetBarIndex(int index)
        {
            barIndex.Value = index;
        }


        /// <summary>
        /// 分節番号
        /// </summary>
        ReactiveProperty<int> subDivisionIndex;
        public int SubDivisionIndex { get { return subDivisionIndex.Value; } }
        public IReadOnlyReactiveProperty<int> SubDivisionIndexRP { get { return subDivisionIndex; } }
        public void SetSubDivisionIndex(int index)
        {
            subDivisionIndex.Value = index;
        }


        /// <summary>
        /// スライダー番号
        /// </summary>
        ReactiveProperty<float> sliderIndex;
        public float SliderIndex { get { return sliderIndex.Value; } }
        public IReadOnlyReactiveProperty<float> SliderIndexRP { get { return sliderIndex; } }
        public void SetSliderIndex(float index)
        {
            sliderIndex.Value = index;
        }

        public void SetSameAddress(AddressInChart address)
        {
            this.barIndex.Value = address.BarIndex;
            this.subDivisionIndex.Value = address.SubDivisionIndex;
            this.sliderIndex.Value = address.SliderIndex;
        }

        /// <summary>
        /// どちらが先のアドレスか返す
        /// 引数のほうが遅ければTrue
        /// </summary>
        /// <param name="address"></param>
        public bool IsEarlierThan(AddressInChart address)
        {
            // 違う小節番号の場合
            if(this.barIndex.Value < address.barIndex.Value) { return true; }
            else if(this.barIndex.Value > address.barIndex.Value) { return false; }

            // 同じ小節番号の場合、分節番号で判断
            if (this.subDivisionIndex.Value < address.subDivisionIndex.Value) { return true; }
            else if (this.subDivisionIndex.Value > address.subDivisionIndex.Value) { return false; }

            // 全く同じ場合falseを返す
            return false;
        }

        /// <summary>
        /// 同じアドレスか調べる
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsSameAddress(AddressInChart address)
        {
            return address.BarIndex == this.BarIndex && address.SubDivisionIndex == this.SubDivisionIndex && address.SliderIndex == this.SliderIndex;
        }

        public string GetAddressText()
        {
            return $"#{BarIndex} - {SubDivisionIndex} - {SliderIndex}";
        }
    }

    /// <summary>
    /// スペースホールドの頂点リスト
    /// </summary>
    public class SpaceHoldVertices
    {
        List<Vector2> defaultVertices = new List<Vector2>
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
            SetVertices(defaultVertices.ToArray());
        }

        public void AddVertex(VertexData addVertex)
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
        }

        public bool RemoveVertex(VertexData vertex)
        {
            return vertices.Remove(vertex);
        }

        public void SetVertices(Vector2[] positions)
        {
            vertices.Clear();

            foreach (var pos in positions)
            {
                vertices.Add(new VertexData(pos));
            }
        }

        public void ClearVertices()
        {
            vertices.Clear();
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
        }

        ReactiveProperty<Vector2> position = new ReactiveProperty<Vector2>();
        public IReadOnlyReactiveProperty<Vector2> Position => position;
        public void SetPosition(Vector2 pos)
        {
            // 単位円状に配置されるように正規化
            pos = pos.ClampToUnitCircle();

            position.Value = pos;
        }

        public float GetSqrMagnitude(VertexData another)
        {
            return (this.position.Value - another.position.Value).sqrMagnitude;
        }
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
        Scale = 30,
        Connect = 40,
        Connecting = 41,
        ChangeType = 50,
        Destroy = 60,

        SpaceDeploy = 100,
        SpaceMove = 110,
        SpaceEdit = 120,

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
        TouchNote,
        DynamicGroundUpward,
        DynamicGroundDownward,
        DynamicGroundRightward,
        DynamicGroundLeftward,
        Hold,
        HoldHidden,
        HoldHiddenJudged,
        HoldEndUnjudge,

        SpaceHold,
        SpaceHoldHidden,
        SpaceHoldHiddenJudged,
        //SpaceHoldEndUnjudge
    }
}

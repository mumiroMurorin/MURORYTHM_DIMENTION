using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class SpaceHoldNote : NoteObject
    {

    }

    [System.Serializable]
    public class NoteData_SpaceHold : IGroundChainNoteData, ITypeChangableNoteData, IVerticesControlableNoteData
    {
        public NoteData_SpaceHold() { }

        public NoteData_SpaceHold(NoteData_SpaceHold data)
        {
            this.Address = new AddressInChart(data.Address);
            this.SetRange(data.Range.ToList());
        }

        // ノートタイプ
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.SpaceHold);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            if (noteType != DeploymentNoteType.SpaceHold &&
                noteType != DeploymentNoteType.SpaceHoldHidden &&
                noteType != DeploymentNoteType.SpaceHoldHiddenJudged &&
                noteType != DeploymentNoteType.SpaceHoldEndUnjudge) 
            {
                Debug.LogWarning($"【Note】SpaceHoldNoteは {noteType} に対応していません");
                return;
            }

            NoteType = noteType;
        }

        /// <summary>
        /// 頂点リスト
        /// </summary>
        public SpaceHoldVertices SpaceHoldVertices { get; private set; } = new SpaceHoldVertices();

        public AddressInChart Address { get; private set; }

        /// <summary>
        /// 配置範囲 (基本0～15)
        /// </summary>
        ReactiveCollection<float> range = new ReactiveCollection<float>() { 0 };

        /// <summary>
        /// ノーツの移動、拡大縮小の監視
        /// </summary>
        public IReadOnlyReactiveCollection<float> Range { get { return range; } }

        public void ChangeNoteType()
        {
            // 終点
            if(NextNote.Value == null)
            {
                switch (NoteType)
                {
                    // 判定あり終点 → 判定なし終点
                    case DeploymentNoteType.SpaceHold:
                        NoteType = DeploymentNoteType.SpaceHoldEndUnjudge;
                        break;
                    // 判定なし終点 → 判定あり終点
                    case DeploymentNoteType.SpaceHoldEndUnjudge:
                        NoteType = DeploymentNoteType.SpaceHold;
                        break;
                }
            }
            // 中継点
            else
            {
                switch (NoteType)
                {
                    // 可視 → 判定なし不可視(中継点のみ)
                    case DeploymentNoteType.SpaceHold:
                        NoteType = DeploymentNoteType.SpaceHoldHidden;
                        break;
                    // 判定なし不可視 → 判定あり不可視
                    case DeploymentNoteType.SpaceHoldHidden:
                        NoteType = DeploymentNoteType.SpaceHoldHiddenJudged;
                        break;
                    // 判定あり不可視 → 可視
                    case DeploymentNoteType.SpaceHoldHiddenJudged:
                        NoteType = DeploymentNoteType.SpaceHold;
                        break;
                }
            }

            UpdateNoteType();
        }

        private void UpdateNoteType()
        {
            // 終点ノーツが変なことになってたら元に戻す
            if(NextNote.Value == null && NoteType != DeploymentNoteType.SpaceHoldEndUnjudge)
            {
                NoteType = DeploymentNoteType.SpaceHold;
            }

            // 始点ノーツが変なことになってたら元に戻す
            if (BackNote.Value == null) 
            {
                NoteType = DeploymentNoteType.SpaceHold;
            }
        }

        public void SetRange(List<float> range)
        {
            this.range.Clear();

            foreach (float index in range)
            {
                this.range.Add(index);
            }

            Address.SetSliderIndex(this.range.First());
        }

        public void ChangeRange(float index, bool isRightAnchored)
        {
            List<float> shifted = new List<float>();
            float min = range.First();
            float max = range.Last();

            // 右固定で左側とindexが一緒のとき返す
            if (isRightAnchored && (int)min == index) { return; }
            // 左固定で右側とindexが一緒のとき返す
            if (!isRightAnchored && (int)max == index) { return; }

            // 右固定で左側に伸ばす
            if (isRightAnchored && index <= max)
            {
                for (float i = index; i <= max; i++) { shifted.Add(i); }
            }
            // 左固定で右側に伸ばす
            else if (!isRightAnchored && index >= min)
            {
                for (float i = min; i <= index; i++) { shifted.Add(i); }
            }
            else
            {
                return;
            }

            SetRange(shifted);
            Debug.Log($"【拡大】\n {range.First()} ～ {range.Last()}");
        }

        public void SetAddress(AddressInChart address)
        {
            // 同じアドレスなら返す
            if (Address != null && Address.IsSameAddress(address)) { return; }

            // 文節が更新されていなければチェインの更新はしない
            bool isUpdateSubLocate = true;
            if(Address == null) { isUpdateSubLocate = false; }
            else if(Address.BarIndex == address.BarIndex && Address.SubDivisionIndex == address.SubDivisionIndex) { isUpdateSubLocate = false; }

            if (Address == null) { Address = new AddressInChart(address); }
            else
            {
                Debug.Log($"【移動】:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
                Address.SetSameAddress(address);
            }

            // 移動に伴う範囲のセット
            int startIndex = (int)address.SliderIndex;
            List<float> currentRange = range.ToList();
            List<float> shifted = currentRange.Select(i => i - currentRange[0] + startIndex).ToList();

            SetRange(shifted);
            if (isUpdateSubLocate) { UpdateChainNote(); }
        }

        /// <summary>
        /// チェインノーツを追加
        /// </summary>
        /// <param name="addNote"></param>
        public void AddChainNote(IGroundChainNoteData addNote, bool isUpdateNoteType = true)
        {
            // 違うノーツ軍なら返す
            if (!CheckSameNoteTypeGroup(addNote.NoteType)) { return; }

            List<IGroundChainNoteData> chains = new List<IGroundChainNoteData>();

            // ノーツを追加
            chains.Add(this);
            chains.Add(addNote);

            // このノーツを遡って全部リストに追加
            IGroundChainNoteData backNote = this.BackNote.Value;
            while (backNote != null)
            {
                chains.Add(backNote);
                backNote = backNote.BackNote.Value;
            }

            // このノーツを進んで全部リストに追加
            IGroundChainNoteData nextNote = this.NextNote.Value;
            while (nextNote != null)
            {
                chains.Add(nextNote);
                nextNote = nextNote.NextNote.Value;
            }

            // 追加ノーツを遡って全部リストに追加
            backNote = addNote.BackNote.Value;
            while (backNote != null)
            {
                chains.Add(backNote);
                backNote = backNote.BackNote.Value;
            }

            // 追加ノーツを進んで全部リストに追加
            nextNote = addNote.NextNote.Value;
            while (nextNote != null)
            {
                chains.Add(nextNote);
                nextNote = nextNote.NextNote.Value;
            }

            ConnectChainNotes(chains, isUpdateNoteType);
        }

        /// <summary>
        /// 引数のタイプが同じSpaceHoldか返す
        /// </summary>
        /// <param name="noteType"></param>
        /// <returns></returns>
        private bool CheckSameNoteTypeGroup(DeploymentNoteType noteType)
        {
            return noteType == DeploymentNoteType.SpaceHold ||
                noteType == DeploymentNoteType.SpaceHoldEndUnjudge ||
                noteType == DeploymentNoteType.SpaceHoldHidden ||
                noteType == DeploymentNoteType.SpaceHoldHiddenJudged;
        }

        /// <summary>
        /// チェインノーツの順を更新
        /// </summary>
        public void UpdateChainNote()
        {
            List<IGroundChainNoteData> chains = new List<IGroundChainNoteData>();

            chains.Add(this);

            // このノーツを遡って全部リストに追加
            IGroundChainNoteData backNote = this.BackNote.Value;
            while (backNote != null)
            {
                chains.Add(backNote);
                backNote = backNote.BackNote.Value;
            }

            // このノーツを進んで全部リストに追加
            IGroundChainNoteData nextNote = this.NextNote.Value;
            while (nextNote != null)
            {
                chains.Add(nextNote);
                nextNote = nextNote.NextNote.Value;
            }

            ConnectChainNotes(chains);
        }

        /// <summary>
        /// 引数ノーツを順に全てつなげる
        /// </summary>
        /// <param name="chains"></param>
        private void ConnectChainNotes(List<IGroundChainNoteData> chains, bool isUpdateNoteType = true)
        {
            // 重複項目を削除
            chains = chains.Distinct().ToList();
            // ソート
            chains.Sort((a, b) => {
                if (a.Address.IsEarlierThan(b.Address)) { return -1; }
                else { return 1; }
            });

            // それぞれのノーツをつなげる
            for (int i = 0; i < chains.Count; i++)
            {
                // 中継点
                if (i > 0) { chains[i].SetBackNote(chains[i - 1], isUpdateNoteType); }
                // 始点
                else { chains[i].SetBackNote(null, isUpdateNoteType); }

                // 中継点
                if (i < chains.Count - 1) { chains[i].SetNextNote(chains[i + 1], isUpdateNoteType); }
                // 終点
                else { chains[i].SetNextNote(null, isUpdateNoteType); }
            }
        }

        /// <summary>
        /// ノーツを削除(コネクトを解除)
        /// </summary>
        public void RemoveNote()
        {
            // 前ノーツ、次ノーツに前後のノーツをセット
            NextNote.Value?.SetBackNote(BackNote.Value);
            BackNote.Value?.SetNextNote(NextNote.Value);
        }

        /// <summary>
        /// 次のノーツ
        /// </summary>
        ReactiveProperty<IGroundChainNoteData> nextNote = new ReactiveProperty<IGroundChainNoteData>();
        public IReadOnlyReactiveProperty<IGroundChainNoteData> NextNote => nextNote;
        public void SetNextNote(IGroundChainNoteData nextNote, bool isUpdateNoteType = true)
        {
            this.nextNote.Value = nextNote;
            if (isUpdateNoteType) { UpdateNoteType(); }
        }

        /// <summary>
        /// 前のノーツ
        /// </summary>
        ReactiveProperty<IGroundChainNoteData> backNote = new ReactiveProperty<IGroundChainNoteData>();
        public IReadOnlyReactiveProperty<IGroundChainNoteData> BackNote => backNote;
        public void SetBackNote(IGroundChainNoteData backNote, bool isUpdateNoteType = true)
        {
            this.backNote.Value = backNote;
            if (isUpdateNoteType) { UpdateNoteType(); }
        }

        public IConnectableObject NoteObject { get; private set; }

        public void SetNoteObject(IConnectableObject noteObject)
        {
            NoteObject = noteObject;
        }

        public IGroundNoteData Copy()
        {
            return new NoteData_SpaceHold(this);
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

        public SpaceHoldVertices()
        {
            foreach(var pos in defaultVertices)
            {
                vertices.Add(new SpaceHoldVertex(pos));
            }
        }

        // 頂点リスト
        ReactiveCollection<SpaceHoldVertex> vertices = new ReactiveCollection<SpaceHoldVertex>();
        public IReadOnlyReactiveCollection<SpaceHoldVertex> Vertices => vertices;

        public void AddVertex(SpaceHoldVertex vertex)
        {
            vertices.Add(vertex);
        }

        public bool RemoveVertex(SpaceHoldVertex vertex)
        {
            return vertices.Remove(vertex);
        }
    }

    /// <summary>
    /// スペースホールドの頂点
    /// </summary>
    public class SpaceHoldVertex
    {
        public SpaceHoldVertex(Vector2 pos)
        {
            SetPosition(pos);
        }

        ReactiveProperty<Vector2> position = new ReactiveProperty<Vector2>();
        public IReadOnlyReactiveProperty<Vector2> Position => position;
        public void SetPosition(Vector2 pos)
        {
            position.Value = pos;
        }
    }
}
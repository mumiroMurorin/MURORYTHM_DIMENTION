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
    public class NoteData_SpaceHold : IChainNoteData, ITypeChangableNoteData, IVerticesControlableNoteData
    {
        public NoteData_SpaceHold() {  }

        public NoteData_SpaceHold(NoteData_SpaceHold data)
        {
            this.Address = new AddressWithinRange(data.Address);
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
                noteType != DeploymentNoteType.SpaceHoldHidden)
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

        public AddressWithinRange Address { get; private set; }

        public void ChangeNoteType()
        {
            // 始点終点は必ず判定あり
            if(NextNote.Value == null || BackNote.Value == null) { return; }

            switch (NoteType)
            {
                // 可視 → 不可視(中継点のみ)
                case DeploymentNoteType.SpaceHold:
                    NoteType = DeploymentNoteType.SpaceHoldHidden;
                    break;
                // 不可視 → 可視
                case DeploymentNoteType.SpaceHoldHidden:
                    NoteType = DeploymentNoteType.SpaceHold;
                    break;
            }


            UpdateNoteType();
        }

        private void UpdateNoteType()
        {
            // 始点ノーツが変なことになってたら元に戻す
            if (BackNote.Value == null) 
            {
                NoteType = DeploymentNoteType.SpaceHold;
            }
        }

        public void SetAddress(AddressWithinRange address)
        {
            // 文節が更新されていなければチェインの更新はしない
            bool isUpdateSubLocate = true;
            if(Address == null) { isUpdateSubLocate = false; }
            else if(Address.BarIndex == address.BarIndex && Address.SubDivisionIndex == address.SubDivisionIndex) { isUpdateSubLocate = false; }

            if (Address == null) { Address = new AddressWithinRange(address); }
            else
            {
                //Debug.Log($"【移動】:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
                Address.SetSameAddress(address);
            }

            if (isUpdateSubLocate) { UpdateChainNote(); }
        }

        /// <summary>
        /// チェインノーツを追加
        /// </summary>
        /// <param name="addNote"></param>
        public void AddChainNote(IChainNoteData addNote, bool isUpdateNoteType = true)
        {
            // 違うノーツ軍なら返す
            if (!CheckSameNoteTypeGroup(addNote.NoteType)) { return; }

            List<IChainNoteData> chains = new List<IChainNoteData>();

            // ノーツを追加
            chains.Add(this);
            chains.Add(addNote);

            // このノーツを遡って全部リストに追加
            IChainNoteData backNote = this.BackNote.Value;
            while (backNote != null)
            {
                chains.Add(backNote);
                backNote = backNote.BackNote.Value;
            }

            // このノーツを進んで全部リストに追加
            IChainNoteData nextNote = this.NextNote.Value;
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
                noteType == DeploymentNoteType.SpaceHoldHidden; 
        }

        /// <summary>
        /// チェインノーツの順を更新
        /// </summary>
        public void UpdateChainNote()
        {
            List<IChainNoteData> chains = new List<IChainNoteData>();

            chains.Add(this);

            // このノーツを遡って全部リストに追加
            IChainNoteData backNote = this.BackNote.Value;
            while (backNote != null)
            {
                chains.Add(backNote);
                backNote = backNote.BackNote.Value;
            }

            // このノーツを進んで全部リストに追加
            IChainNoteData nextNote = this.NextNote.Value;
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
        private void ConnectChainNotes(List<IChainNoteData> chains, bool isUpdateNoteType = true)
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
        ReactiveProperty<IChainNoteData> nextNote = new ReactiveProperty<IChainNoteData>();
        public IReadOnlyReactiveProperty<IChainNoteData> NextNote => nextNote;
        public void SetNextNote(IChainNoteData nextNote, bool isUpdateNoteType = true)
        {
            this.nextNote.Value = nextNote;
            if (isUpdateNoteType) { UpdateNoteType(); }
        }

        /// <summary>
        /// 前のノーツ
        /// </summary>
        ReactiveProperty<IChainNoteData> backNote = new ReactiveProperty<IChainNoteData>();
        public IReadOnlyReactiveProperty<IChainNoteData> BackNote => backNote;
        public void SetBackNote(IChainNoteData backNote, bool isUpdateNoteType = true)
        {
            this.backNote.Value = backNote;
            if (isUpdateNoteType) { UpdateNoteType(); }
        }

        public IConnectableObject NoteObject { get; private set; }

        public void SetNoteObject(IConnectableObject noteObject)
        {
            NoteObject = noteObject;
        }

        public IDeployableNoteData Copy()
        {
            return new NoteData_SpaceHold(this);
        }
    }
}
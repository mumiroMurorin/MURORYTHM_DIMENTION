using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class HoldNote : NoteObject
    {

    }

    [System.Serializable]
    public class NoteData_Hold : IChainNoteData, ITypeChangableNoteData
    {
        public NoteData_Hold() { }

        public NoteData_Hold(NoteData_Hold data)
        {
            this.Address = new AddressWithinRange(data.Address);
        }

        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.Hold);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            if (noteType != DeploymentNoteType.Hold &&
                noteType != DeploymentNoteType.HoldHidden &&
                noteType != DeploymentNoteType.HoldEndUnjudge)
            {
                Debug.LogWarning($"【Note】HoldNoteは {noteType} に対応していません");
                return;
            }

            NoteType = noteType;
        }

        public AddressWithinRange Address { get; private set; }

        public void ChangeNoteType()
        {
            // 終点
            if(NextNote.Value == null)
            {
                switch (NoteType)
                {
                    // 判定あり終点 → 判定なし終点
                    case DeploymentNoteType.Hold:
                        NoteType = DeploymentNoteType.HoldEndUnjudge;
                        break;
                    // 判定なし終点 → 判定あり終点
                    case DeploymentNoteType.HoldEndUnjudge:
                        NoteType = DeploymentNoteType.Hold;
                        break;
                }
            }
            // 中継点
            else
            {
                switch (NoteType)
                {
                    // 可視 → 不可視
                    case DeploymentNoteType.Hold:
                        NoteType = DeploymentNoteType.HoldHidden;
                        break;
                    // 不可視 → 可視
                    case DeploymentNoteType.HoldHidden:
                        NoteType = DeploymentNoteType.Hold;
                        break;
                }
            }

            UpdateNoteType();
        }

        private void UpdateNoteType()
        {
            // 終点ノーツが変なことになってたら元に戻す
            if(NextNote.Value == null && NoteType != DeploymentNoteType.HoldEndUnjudge)
            {
                NoteType = DeploymentNoteType.Hold;
            }

            // 始点ノーツが変なことになってたら元に戻す
            if (BackNote.Value == null) 
            {
                NoteType = DeploymentNoteType.Hold;
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
        /// 引数のタイプが同じHoldか返す
        /// </summary>
        /// <param name="noteType"></param>
        /// <returns></returns>
        private bool CheckSameNoteTypeGroup(DeploymentNoteType noteType)
        {
            return noteType == DeploymentNoteType.Hold ||
                noteType == DeploymentNoteType.HoldEndUnjudge ||
                noteType == DeploymentNoteType.HoldHidden;
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
            return new NoteData_Hold(this);
        }
    }

}
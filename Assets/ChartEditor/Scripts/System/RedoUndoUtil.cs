using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChartEditor;
using static UndoRedo.History;

namespace UndoRedo.Vertices
{
    public static class VerticesMoveRecord
    {
        public static void RecordVertexMoving(VertexData data, Vector2 previous, Vector2 current)
        {
            Record(() => MoveVertex(data, current), () => MoveVertex(data, previous));
        }

        /// <summary>
        /// 頂点データの移動をRedoUndoに対応させる
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="current"></param>
        public static void RecordVertcesMoving(List<VertexDataToPos> previous, List<VertexDataToPos> current)
        {
            Record(() =>
            // 移動
            {
                foreach (var pair in current)
                {
                    MoveVertex(pair.Data, pair.Pos);
                }
            }, () =>
            // 戻す
            {
                foreach (var pair in previous)
                {
                    MoveVertex(pair.Data, pair.Pos);
                }
            });
        }

        private static void MoveVertex(VertexData data, Vector2 toPos)
        {
            data.SetPosition(toPos);
        }
    }
}

namespace UndoRedo.Notes
{
    public static class NotesMoveRecord
    {
        public static void RecordNotesMoving(List<NoteDataToAddress> previous, List<NoteDataToAddress> current)
        {
            Record(() =>
            // 移動
            {
                foreach (var pair in current)
                {
                    MoveNote(pair.NoteData, pair.Address);
                }
            }, () =>
            // 戻す
            {
                foreach (var pair in previous)
                {
                    MoveNote(pair.NoteData, pair.Address);
                }
            });
        }

        /// <summary>
        /// アドレスが1つでも変わっているときだけ、移動をRedoUndoに登録する
        /// (クリックしただけで動かしていない操作を履歴に積まないため)
        /// </summary>
        /// <returns>登録したとき true</returns>
        public static bool RecordNotesMovingIfChanged(List<NoteDataToAddress> previous, List<NoteDataToAddress> current)
        {
            if (!IsAddressChanged(previous, current)) { return false; }

            RecordNotesMoving(previous, current);
            return true;
        }

        private static bool IsAddressChanged(List<NoteDataToAddress> previous, List<NoteDataToAddress> current)
        {
            if (previous.Count != current.Count) { return true; }

            for (int i = 0; i < previous.Count; i++)
            {
                if (previous[i].NoteData != current[i].NoteData) { return true; }

                var a = previous[i].Address;
                var b = current[i].Address;
                if (a.BarIndex != b.BarIndex || a.SubDivisionIndex != b.SubDivisionIndex) { return true; }

                var rangeA = a.Range;
                var rangeB = b.Range;
                if (rangeA.Count != rangeB.Count) { return true; }
                for (int j = 0; j < rangeA.Count; j++)
                {
                    if (rangeA[j] != rangeB[j]) { return true; }
                }
            }

            return false;
        }

        public static void RecordNotesMovingMirror(List<NoteDataToAddress> previous, List<NoteDataToAddress> current)
        {
            Record(() =>
            // 移動
            {
                foreach (var pair in current)
                {
                    MoveNote(pair.NoteData, pair.Address);
                    ChangeTypeNote(pair.NoteData);
                    MirrorCurveControl(pair.NoteData);
                }
            }, () =>
            // 戻す
            {
                foreach (var pair in previous)
                {
                    MoveNote(pair.NoteData, pair.Address);
                    ChangeTypeNote(pair.NoteData);
                    MirrorCurveControl(pair.NoteData);
                }
            });
        }

        /// <summary>
        /// ホールドを曲げる制御点も、左右を入れ替える (反転した後も、反転した側に曲がったままになるように)
        /// </summary>
        public static void MirrorCurveControl(IDeployableNoteData noteData)
        {
            if (noteData is ChartEditor.NoteData_Hold hold) { hold.MirrorCurveControl(); }
        }

        public static void MoveNote(IDeployableNoteData noteData, AddressWithinRange address)
        {
            noteData.SetAddress(address);
        }

        public static void ChangeTypeNote(IDeployableNoteData noteData)
        {
            if (noteData is not IMirrorTypeChangableNoteData t) { return; }

            t?.ChangeNoteType();
        }
    }
}
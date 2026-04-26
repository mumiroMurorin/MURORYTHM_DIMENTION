using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChartEditor;
using static UndoRedo.History;

namespace UndoRedo.Vertices
{
    public static class VerticesMoveRecord
    {
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

        public static void RecordNotesMovingMirror(List<NoteDataToAddress> previous, List<NoteDataToAddress> current)
        {
            Record(() =>
            // 移動
            {
                foreach (var pair in current)
                {
                    MoveNote(pair.NoteData, pair.Address);
                    ChangeTypeNote(pair.NoteData);
                }
            }, () =>
            // 戻す
            {
                foreach (var pair in previous)
                {
                    MoveNote(pair.NoteData, pair.Address);
                    ChangeTypeNote(pair.NoteData);
                }
            });
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{
    /// <summary>
    /// 判定調整
    /// </summary>
    public class JudgementWindowAdjuster
    {
        /// <summary>
        /// 譜面データの中で判定枠を仕様に従って調整する
        /// </summary>
        /// <param name="chartData"></param>
        /// <param name="judgementWindows"></param>
        public void AdjustJudgementWindow(ChartData chartData, List<NoteTypeToJudgementWindow> judgementWindows)
        {
            List<IJudgableNoteData> judgableList = new List<IJudgableNoteData>();
            List<IClippedJudgableNote> clippedJudgableList = new List<IClippedJudgableNote>();

            // ノーツデータの中から判定持ちを取り出す
            foreach (var noteDataList in chartData.AllNoteDataLists)
            {
                // 判定持ちノーツだったら取り出す
                if (noteDataList.Count > 0 && noteDataList[0] is IJudgableNoteData)
                {
                    judgableList.AddRange(noteDataList.OfType<IJudgableNoteData>().ToList());
                }

                // 削り判定持ちノーツだったら取り出す
                else if (noteDataList.Count > 0 && noteDataList[0] is IClippedJudgableNote)
                {
                    clippedJudgableList.AddRange(noteDataList.OfType<IClippedJudgableNote>().ToList());
                }
            }

            // 判定持ちに判定枠を与える
            foreach (var judgableData in judgableList)
            {
                // 判定枠の取得
                var window = GetJudgementWindow(judgableData.NoteType, judgementWindows);
                if (window == null) { continue; }

                judgableData.JudgementWindow = window;
            }

            // 削り判定持ちに判定枠を与える
            // ソート
            clippedJudgableList.Sort((a, b) => a.Timing.CompareTo(b.Timing));
            for (int i = 0; i < clippedJudgableList.Count; i++)
            {
                var clippedData = clippedJudgableList[i];

                // 判定枠の取得
                var window = GetJudgementWindow(clippedData.NoteType, judgementWindows);
                if (window == null) { continue; }

                // ディープコピー
                clippedJudgableList[i].JudgementWindow = window.Copy();

                // 判定を削る
                ClipJudementWindow(clippedJudgableList, i);
            }
        }

        /// <summary>
        /// そのノーツの判定枠を返す
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="judgementWindows"></param>
        /// <returns></returns>
        private JudgementWindow GetJudgementWindow(NoteType targetType, List<NoteTypeToJudgementWindow> judgementWindows)
        {
            // 判定枠の取得
            foreach (var windowType in judgementWindows)
            {
                var window = windowType.CheckAndGetJudgementWindow(targetType);
                if (window != null) { return window; }
            }

            Debug.LogWarning($"【System】{targetType}に該当する判定枠が見つかりませんでした");
            return null;
        }

        private void ClipJudementWindow(List<IClippedJudgableNote> sortedList, int index)
        {
            if (sortedList.Count <= index) { Debug.LogError($"【System】範囲外です: {index}"); }

            IClippedJudgableNote targetNote = sortedList[index];
            JudgementWindow targetWindow = targetNote.JudgementWindow;

            // 前判定を削る
            int i = index;
            while (true)
            {
                // 最初のノーツならおしまい
                if (--i < 0) { break; }

                IClippedJudgableNote previousNote = sortedList[i];

                // ノーツが完全被りの場合は戻す
                if(targetNote.Timing == previousNote.Timing) { continue; }

                // 判定枠が被らなくなったらおしまい
                float startJudgement = targetNote.Timing - targetWindow.GoodWindowFaster;    // 前判定端
                float previousEndJudgement = previousNote.Timing + previousNote.JudgementWindow.GoodWindowLatter;
                float cover = previousEndJudgement - startJudgement;
                if (cover < 0) { break; }

                // レーンが被ってなかったら戻す
                if (!targetNote.Range.Intersect(previousNote.Range).Any()) { continue; }

                // このノーツの前判定を削り、前ノーツの後ろ判定も削る
                targetWindow.ClipWindow(cover / 2f, true);
                previousNote.JudgementWindow.ClipWindow(cover / 2f, false);
            }
        }
    }

}
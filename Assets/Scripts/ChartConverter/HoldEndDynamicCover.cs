using System;
using System.Collections.Generic;

namespace ChartConvert
{
    /// <summary>
    /// 判定なしのホールド終点 (HoldEndUnjudge) の上に、ダイナミックノーツが載っているかを調べる (ゲーム用の変換で使う)
    ///
    /// ・載っているとき: 今までどおり、終点のノーツを出す
    /// ・載っていないとき: 終点のノーツを出さず、ホールドの帯を終点に向けてフェードアウトさせる
    ///
    /// 「載っている」= 同じ分線に、終点とまったく同じレーン (同じ位置・同じ幅) のダイナミックノーツ (上下左右のどれでも) がある。
    /// 幅や位置が少しでも違うものは、載っていないとみなす (フェードアウトする)。
    /// 分線は、同じホールドのノーツが2つ以上あると「1つ目だけ」「2つ目だけ」…に分けて読まれ (ChainOrderResolver)、
    /// ダイナミックノーツは先頭の1つにしか入らない。そのため、分ける前の分線全体で調べておく。
    /// </summary>
    public sealed class HoldEndDynamicCover
    {
        // 今読んでいる分線にある、ダイナミックノーツのレーン
        readonly List<int[]> dynamicRanges = new List<int[]>();

        /// <summary>
        /// 分線を読み始める前に呼ぶ (分ける前の、分線全体のデータを渡す)
        /// </summary>
        public void Prepare(SubDivisionDataOrigin subDivision)
        {
            dynamicRanges.Clear();
            if (subDivision == null) { return; }

            AddRanges(subDivision.DynamicUpwardData, note => note.Range);
            AddRanges(subDivision.DynamicDownwardData, note => note.Range);
            AddRanges(subDivision.DynamicRightwardData, note => note.Range);
            AddRanges(subDivision.DynamicLeftwardData, note => note.Range);
        }

        /// <summary>
        /// 今読んでいる分線に、このレーンとまったく同じレーンのダイナミックノーツがあるか
        /// </summary>
        public bool HasDynamicOn(int[] range)
        {
            if (range == null || range.Length == 0) { return false; }

            foreach (int[] dynamicRange in dynamicRanges)
            {
                if (HasSameLanes(range, dynamicRange)) { return true; }
            }

            return false;
        }

        private void AddRanges<T>(List<T> notes, Func<T, int[]> getRange)
        {
            if (notes == null) { return; }

            foreach (T note in notes)
            {
                if (note == null) { continue; }

                int[] range = getRange(note);
                if (range != null && range.Length > 0) { dynamicRanges.Add(range); }
            }
        }

        // 同じレーンの組か (並び順は問わない。譜面のレーンは重複しない)
        private static bool HasSameLanes(int[] first, int[] second)
        {
            if (first.Length != second.Length) { return false; }

            foreach (int lane in first)
            {
                if (Array.IndexOf(second, lane) < 0) { return false; }
            }

            return true;
        }
    }
}

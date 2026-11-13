using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// ホールド・スペースホールドの「前後のつながり方に合わせて種類を直す処理」を、譜面の読み込み中だけ保留する
    ///
    /// 種類の補正は、前後のノーツとのつながりが変わった1フレーム後に走る。
    /// 読み込みは数フレームに分けて行うので、そのままだと中継点が「次のノーツがまだ読み込まれていない」状態で
    /// 補正されていったん終点扱いになり、帯つきの中継点(HoldMeshRelay など)がふつうの中継点に変わってしまう。
    /// そこで読み込み中は補正を後回しにして、つながりが確定してからまとめて1回だけ補正する。
    /// </summary>
    public static class ChainNoteTypeCorrection
    {
        static bool isSuspended;

        // 保留中に補正を求められたノーツと、その補正処理 (同じノーツは1回だけ行う)
        static readonly Dictionary<object, Action> deferredCorrections = new Dictionary<object, Action>();

        /// <summary>
        /// 補正の保留を始める (譜面の読み込みを始めるときに呼ぶ)
        /// </summary>
        public static void Suspend()
        {
            isSuspended = true;
        }

        /// <summary>
        /// 保留中なら、補正を後回しにして true を返す
        /// </summary>
        /// <param name="note">補正するノーツ</param>
        /// <param name="correction">後で行う補正処理</param>
        public static bool TryDefer(object note, Action correction)
        {
            if (!isSuspended) { return false; }

            deferredCorrections[note] = correction;
            return true;
        }

        /// <summary>
        /// 補正の保留を終える (譜面の読み込みが終わったときに必ず呼ぶ)
        /// </summary>
        /// <param name="runDeferred">true のとき、後回しにしていた補正をここで行う。false のときは捨てる</param>
        public static void Resume(bool runDeferred)
        {
            isSuspended = false;
            if (deferredCorrections.Count == 0) { return; }

            var corrections = new List<Action>(deferredCorrections.Values);
            deferredCorrections.Clear();
            if (!runDeferred) { return; }

            foreach (var correction in corrections)
            {
                try
                {
                    correction();
                }
                catch (Exception ex)
                {
                    // 1つの失敗で残りの補正を止めない
                    Debug.LogException(ex);
                }
            }
        }
    }
}

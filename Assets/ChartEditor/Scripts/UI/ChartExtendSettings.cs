using System;
using UniRx;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 譜面を延長するときの設定 (延長する小節数と、延長する小節の拍子・分割数) の決まりごと
    ///
    /// 値そのものは ChartEditorOptionHolder が持っている (IChartEditorOptionGetter の Extend〜)。
    /// 入力欄は、クイックメニュー (左下の「譜面長さ」) と、画面上のメニューの「譜面」の2か所にあり、
    /// どちらも同じ値を見ている (ChartExtendSettingsView)。
    /// </summary>
    public static class ChartExtendSettings
    {
        // 入力できる値の範囲
        // 延長は、追加する分線をその場で全部作る。桁を打ち間違えたときに、大量の分線を作り続けて固まるのを防ぐ
        public const int MIN_BAR_COUNT = 1;
        public const int MAX_BAR_COUNT = 500;
        // 拍子の分子 (1小節の拍の数)
        public const int MIN_BEAT_COUNT = 1;
        public const int MAX_BEAT_COUNT = 32;
        // 拍子の分母 (1拍の長さ。4 で4分音符、8 で8分音符)
        public const float MIN_BEAT_UNIT = 1f;
        public const float MAX_BEAT_UNIT = 64f;
        // 分割数 (1拍を何本の分線に分けるか)
        public const int MIN_DIVISION_NUM = 1;
        public const int MAX_DIVISION_NUM = 64;
        // 一度の延長で追加できる分線の合計 (小節数 × 拍の数 × 分割数)。値がそれぞれ範囲の中でも、掛け合わせると大きくなりすぎる
        public const int MAX_TOTAL_SUBDIVISIONS = 10000;

        // 最初に入っている値 (1小節、4/4拍子、4分割)
        public const int DEFAULT_BAR_COUNT = 1;
        public const int DEFAULT_BEAT_COUNT = 4;
        public const float DEFAULT_BEAT_UNIT = 4f;
        public const int DEFAULT_DIVISION_NUM = 4;

        const string ERROR_TITLE = "譜面を延長できません";

        /// <summary>
        /// 今の設定から、延長する小節数と、延長する小節の拍子・分割数を取り出す
        /// 延長できない組み合わせのときは、理由を表示して false を返す
        /// (値は、設定するときにそれぞれの範囲へ収められている。ここでは、掛け合わせた分線の合計を確かめる)
        /// </summary>
        public static bool TryGetValues(IChartEditorOptionGetter optionGetter, out int barCount, out BarConfig barConfig)
        {
            barCount = optionGetter.ExtendBarCount.Value;
            int beatCount = optionGetter.ExtendBeatCount.Value;
            float beatUnit = optionGetter.ExtendBeatUnit.Value;
            int divisionNum = optionGetter.ExtendDivisionNum.Value;
            barConfig = null;

            if (!IsWithinTotalLimit(barCount, beatCount, divisionNum, out long totalSubdivisions))
            {
                MessageDialog.ShowError(
                    ERROR_TITLE,
                    $"一度に追加できる分線は {MAX_TOTAL_SUBDIVISIONS} 本までです。\n" +
                    $"今の設定では {totalSubdivisions} 本になります (小節数 {barCount} × 拍の数 {beatCount} × 分割数 {divisionNum})。\n" +
                    "小節数を減らして、何回かに分けて延長してください。");
                return false;
            }

            barConfig = new BarConfig(beatCount, beatUnit, divisionNum);
            return true;
        }

        /// <summary>
        /// 追加する分線の合計が、一度に追加できる数に収まっているか
        /// </summary>
        public static bool IsWithinTotalLimit(int barCount, int beatCount, int divisionNum, out long totalSubdivisions)
        {
            totalSubdivisions = (long)barCount * beatCount * divisionNum;
            return totalSubdivisions <= MAX_TOTAL_SUBDIVISIONS;
        }

        /// <summary>
        /// 入力欄と、延長の設定をつなぐ
        /// ・設定が変わったら、入力欄に表示する
        /// ・入力欄に打ち込まれたら、設定を変える (範囲の外の値は、範囲の端の値になる)
        /// </summary>
        /// <param name="owner">つないだ処理の持ち主 (これが破棄されるときに、つなぎを解く)</param>
        public static void Bind(
            ChartExtendSettingsView view,
            IChartEditorOptionGetter optionGetter,
            IChartEditorOptionSetter optionSetter,
            Component owner)
        {
            if (view == null || optionGetter == null || optionSetter == null || owner == null) { return; }

            // 入力欄を、今の設定の値に合わせ直す
            // (打ち込んだ値が範囲の外で、設定が変わらなかったときも、入力欄には範囲の端の値を出す)
            Action refresh = () =>
            {
                view.SetBarCount(optionGetter.ExtendBarCount.Value);
                view.SetBeatCount(optionGetter.ExtendBeatCount.Value);
                view.SetBeatUnit(optionGetter.ExtendBeatUnit.Value);
                view.SetDivisionNum(optionGetter.ExtendDivisionNum.Value);
            };

            optionGetter.ExtendBarCount.Subscribe(view.SetBarCount).AddTo(owner);
            optionGetter.ExtendBeatCount.Subscribe(view.SetBeatCount).AddTo(owner);
            optionGetter.ExtendBeatUnit.Subscribe(view.SetBeatUnit).AddTo(owner);
            optionGetter.ExtendDivisionNum.Subscribe(view.SetDivisionNum).AddTo(owner);

            view.OnBarCountEnteredListener += value => { optionSetter.SetExtendBarCount(value); refresh(); };
            view.OnBeatCountEnteredListener += value => { optionSetter.SetExtendBeatCount(value); refresh(); };
            view.OnBeatUnitEnteredListener += value => { optionSetter.SetExtendBeatUnit(value); refresh(); };
            view.OnDivisionNumEnteredListener += value => { optionSetter.SetExtendDivisionNum(value); refresh(); };
            // 数として読めない入力は、無かったことにして、今の値に戻す
            view.OnInvalidInputListener += refresh;
        }
    }
}

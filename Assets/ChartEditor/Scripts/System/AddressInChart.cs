using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
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
            if (address == null)
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

        public AddressInChart(AddressWithinRange rangeAddress)
        {
            if (rangeAddress == null)
            {
                this.barIndex = new ReactiveProperty<int>(BAR_INDEX_DEFAULT);
                this.subDivisionIndex = new ReactiveProperty<int>(SUBDIVISION_INDEX_DEFAULT);
                this.sliderIndex = new ReactiveProperty<float>(SLIDER_INDEX_DEFAULT);
            }
            else
            {
                this.barIndex = new ReactiveProperty<int>(rangeAddress.BarIndex);
                this.subDivisionIndex = new ReactiveProperty<int>(rangeAddress.SubDivisionIndex);
                this.sliderIndex = new ReactiveProperty<float>(rangeAddress.Range[0]);
            }
        }

        public static AddressInChart operator +(AddressInChart z, AddressInChart w)
        {
            return new AddressInChart(z.BarIndex + w.BarIndex, z.SubDivisionIndex + w.SubDivisionIndex, z.SliderIndex + w.SliderIndex);
        }

        public static AddressInChart operator -(AddressInChart z, AddressInChart w)
        {
            return new AddressInChart(z.BarIndex - w.BarIndex, z.SubDivisionIndex - w.SubDivisionIndex, z.SliderIndex - w.SliderIndex);
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

        /// <summary>
        /// 同じアドレスか調べる
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsSameAddress(AddressInChart address)
        {
            return address.BarIndex == this.BarIndex && address.SubDivisionIndex == this.SubDivisionIndex && address.SliderIndex == this.SliderIndex;
        }

        public override string ToString()
        {
            return $"#{BarIndex} - {SubDivisionIndex} - {SliderIndex}";
        }
    }

    public class AddressWithinRange
    {
        const int BAR_INDEX_DEFAULT = 0;
        const int SUBDIVISION_INDEX_DEFAULT = 0;
        readonly List<float> RANGE_DEFAULT = new List<float> { 0f };

        public AddressWithinRange(int barIndex = BAR_INDEX_DEFAULT, int subDivisionIndex = SUBDIVISION_INDEX_DEFAULT, List<float> range = null)
        {
            range ??= RANGE_DEFAULT;

            SetBarIndex(barIndex);
            SetSubDivisionIndex(subDivisionIndex);
            SetRange(range);
        }

        public AddressWithinRange(AddressWithinRange address)
        {
            if (address == null)
            {
                SetBarIndex(BAR_INDEX_DEFAULT);
                SetSubDivisionIndex(SUBDIVISION_INDEX_DEFAULT);
                SetRange(RANGE_DEFAULT);
            }
            else
            {
                SetBarIndex(address.BarIndex);
                SetSubDivisionIndex(address.SubDivisionIndex);
                SetRange(address.Range);
            }
        }

        public AddressWithinRange(AddressInChart addressInChart, int rangeCount)
        {
            if (addressInChart == null)
            {
                SetBarIndex(BAR_INDEX_DEFAULT);
                SetSubDivisionIndex(SUBDIVISION_INDEX_DEFAULT);
            }
            else
            {
                SetBarIndex(addressInChart.BarIndex);
                SetSubDivisionIndex(addressInChart.SubDivisionIndex);
            }

            if (rangeCount == 0) 
            {
                SetRange(RANGE_DEFAULT);
            }
            else
            {
                var range = Enumerable.Range((int)addressInChart.SliderIndex, rangeCount).Select(x => (float)x).ToList();
                SetRange(range);
            }
        }

        /// <summary>
        /// 小節線番号
        /// </summary>
        ReactiveProperty<int> barIndex = new ReactiveProperty<int>();
        public int BarIndex { get { return barIndex.Value; } }
        public IReadOnlyReactiveProperty<int> BarIndexRP { get { return barIndex; } }
        public void SetBarIndex(int index)
        {
            barIndex.Value = index;
        }


        /// <summary>
        /// 分節番号
        /// </summary>
        ReactiveProperty<int> subDivisionIndex = new ReactiveProperty<int>();
        public int SubDivisionIndex { get { return subDivisionIndex.Value; } }
        public IReadOnlyReactiveProperty<int> SubDivisionIndexRP { get { return subDivisionIndex; } }
        public void SetSubDivisionIndex(int index)
        {
            subDivisionIndex.Value = index;
        }


        /// <summary>
        /// 配置範囲
        /// </summary>
        ReactiveCollection<float> range = new ReactiveCollection<float>();
        public List<float> Range { get { return RangeRP.ToList(); } }
        public IReadOnlyReactiveCollection<float> RangeRP { get { return range; } }
        public void SetRange(List<float> range)
        {
            this.range.Clear();

            foreach (float index in range)
            {
                if ((index < 0 || 15 < index) && index != 100) { continue; }
                this.range.Add(index);
            }
        }

        public void SetSameAddress(AddressWithinRange address)
        {
            SetBarIndex(address.BarIndex);
            SetSubDivisionIndex(address.SubDivisionIndex);
            SetRange(address.Range);
        }

        /// <summary>
        /// どちらが先のアドレスか返す
        /// 引数のほうが遅ければTrue
        /// </summary>
        /// <param name="address"></param>
        public bool IsEarlierThan(AddressWithinRange address)
        {
            // 違う小節番号の場合
            if (this.barIndex.Value < address.barIndex.Value) { return true; }
            else if (this.barIndex.Value > address.barIndex.Value) { return false; }

            // 同じ小節番号の場合、分節番号で判断
            if (this.subDivisionIndex.Value < address.subDivisionIndex.Value) { return true; }
            else if (this.subDivisionIndex.Value > address.subDivisionIndex.Value) { return false; }

            // 全く同じ場合falseを返す
            return false;
        }
    }
}

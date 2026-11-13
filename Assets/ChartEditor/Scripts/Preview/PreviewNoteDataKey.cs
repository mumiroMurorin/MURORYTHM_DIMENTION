using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// プレビュー用ノーツデータの「中身」を表すキー
    /// 2つのノーツデータの中身が同じなら同じキーになるので、作り直しが要るかどうかの判定に使う
    /// (中身を128ビットに要約した値。違う中身が同じキーになることは実用上起きない)
    /// </summary>
    public readonly struct PreviewNoteDataKey : IEquatable<PreviewNoteDataKey>
    {
        readonly ulong high;
        readonly ulong low;

        public PreviewNoteDataKey(ulong high, ulong low)
        {
            this.high = high;
            this.low = low;
        }

        public ulong High => high;

        public ulong Low => low;

        public bool Equals(PreviewNoteDataKey other) => high == other.high && low == other.low;

        public override bool Equals(object obj) => obj is PreviewNoteDataKey other && Equals(other);

        public override int GetHashCode() => (int)(low ^ (low >> 32));
    }

    /// <summary>
    /// 値を順に混ぜ込んでキーを作る
    /// </summary>
    public struct PreviewKeyHasher
    {
        // FNV-1a (64bit) を、初期値を変えて2本同時に計算する
        const ulong PRIME = 1099511628211UL;
        ulong high;
        ulong low;

        public static PreviewKeyHasher Create()
        {
            return new PreviewKeyHasher { high = 14695981039346656037UL, low = 0x9E3779B97F4A7C15UL };
        }

        public void Add(int value)
        {
            uint bits = unchecked((uint)value);
            for (int i = 0; i < 4; i++)
            {
                ulong b = (bits >> (i * 8)) & 0xFF;
                high = (high ^ b) * PRIME;
                low = (low ^ (b + 0x5BUL)) * PRIME;
            }
        }

        public void Add(float value)
        {
            // 数値として同じでも表現が違うもの(0と-0など)は別扱いになるが、
            // 同じ譜面からは常に同じ計算結果が出るので問題ない
            Add(new FloatBits { Float = value }.Int);
        }

        public void Add(bool value) => Add(value ? 1 : 0);

        public void Add(Vector2 value)
        {
            Add(value.x);
            Add(value.y);
        }

        public void Add(string value)
        {
            if (value == null) { Add(-1); return; }

            Add(value.Length);
            foreach (char c in value) { Add(c); }
        }

        public void Add(ulong value)
        {
            Add(unchecked((int)(value & 0xFFFFFFFFUL)));
            Add(unchecked((int)(value >> 32)));
        }

        public void Add(PreviewNoteDataKey key)
        {
            Add(key.High);
            Add(key.Low);
        }

        public PreviewNoteDataKey ToKey() => new PreviewNoteDataKey(high, low);

        [StructLayout(LayoutKind.Explicit)]
        struct FloatBits
        {
            [FieldOffset(0)] public float Float;
            [FieldOffset(0)] public int Int;
        }
    }

    /// <summary>
    /// ノーツデータからキーを作る
    ///
    /// ノーツデータのクラスが持つプロパティのうち、「譜面から決まる値」の型
    /// (数値・真偽値・レーン範囲・頂点・ホールドの経路)をすべて混ぜ込む。
    /// 入力やタイマーなどの参照、生成時に作られる描画用の部品は含めない。
    /// 新しい種類のノーツを足しても、同じ型のプロパティなら自動で対象になる。
    ///
    /// 注意: 必ず「変換した直後・生成する前」のデータに対して呼ぶこと
    /// (ファクトリが生成時にデータへ値を書き足すため、生成後は中身が変わる)
    /// </summary>
    public static class PreviewNoteDataKeyBuilder
    {
        delegate void PropertyHasher(object data, ref PreviewKeyHasher hasher);

        static readonly Dictionary<Type, PropertyHasher[]> hashersByType = new Dictionary<Type, PropertyHasher[]>();

        public static PreviewNoteDataKey Build(INoteData data)
        {
            var hasher = PreviewKeyHasher.Create();
            Type type = data.GetType();
            hasher.Add(type.FullName);

            foreach (var propertyHasher in GetPropertyHashers(type))
            {
                propertyHasher(data, ref hasher);
            }

            return hasher.ToKey();
        }

        static PropertyHasher[] GetPropertyHashers(Type type)
        {
            if (hashersByType.TryGetValue(type, out var cached)) { return cached; }

            var list = new List<PropertyHasher>();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // プロパティの並び順に左右されないよう、名前順に固定する
            Array.Sort(properties, (a, b) => string.CompareOrdinal(a.Name, b.Name));

            foreach (var property in properties)
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0) { continue; }

                var propertyHasher = CreatePropertyHasher(type, property);
                if (propertyHasher != null) { list.Add(propertyHasher); }
            }

            var result = list.ToArray();
            hashersByType.Add(type, result);
            return result;
        }

        /// <summary>
        /// 対象の型のプロパティなら、値を読んでキーに混ぜ込む処理を作る。対象外の型なら null
        /// </summary>
        static PropertyHasher CreatePropertyHasher(Type dataType, PropertyInfo property)
        {
            Type propertyType = property.PropertyType;
            string methodName;

            if (propertyType == typeof(float)) { methodName = nameof(CreateFloat); }
            else if (propertyType == typeof(int)) { methodName = nameof(CreateInt); }
            else if (propertyType == typeof(bool)) { methodName = nameof(CreateBool); }
            else if (propertyType.IsEnum) { return CreateEnum(property); }
            else if (propertyType == typeof(int[])) { methodName = nameof(CreateIntArray); }
            else if (propertyType == typeof(float[])) { methodName = nameof(CreateFloatArray); }
            else if (propertyType == typeof(Vector2[])) { methodName = nameof(CreateVector2Array); }
            else if (propertyType == typeof(List<TimeToRange>)) { methodName = nameof(CreateTimeToRanges); }
            else if (propertyType == typeof(List<TimeToVertices>)) { methodName = nameof(CreateTimeToVertices); }
            else { return null; }

            // プロパティが宣言されているクラスを型引数にして、値を直接読むデリゲートを作る
            // (リフレクションで毎回読むより速く、数値をオブジェクトに包む無駄も出ない)
            var method = typeof(PreviewNoteDataKeyBuilder)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(property.DeclaringType);

            return (PropertyHasher)method.Invoke(null, new object[] { property });
        }

        static Func<TData, TValue> CreateGetter<TData, TValue>(PropertyInfo property)
        {
            return (Func<TData, TValue>)Delegate.CreateDelegate(typeof(Func<TData, TValue>), property.GetGetMethod());
        }

        static PropertyHasher CreateFloat<TData>(PropertyInfo property)
        {
            var getter = CreateGetter<TData, float>(property);
            return (object data, ref PreviewKeyHasher hasher) => hasher.Add(getter((TData)data));
        }

        static PropertyHasher CreateInt<TData>(PropertyInfo property)
        {
            var getter = CreateGetter<TData, int>(property);
            return (object data, ref PreviewKeyHasher hasher) => hasher.Add(getter((TData)data));
        }

        static PropertyHasher CreateBool<TData>(PropertyInfo property)
        {
            var getter = CreateGetter<TData, bool>(property);
            return (object data, ref PreviewKeyHasher hasher) => hasher.Add(getter((TData)data));
        }

        static PropertyHasher CreateEnum(PropertyInfo property)
        {
            // 列挙型は種類が少なく呼ばれる回数も少ないので、リフレクションでそのまま読む
            return (object data, ref PreviewKeyHasher hasher) => hasher.Add(Convert.ToInt32(property.GetValue(data)));
        }

        static PropertyHasher CreateIntArray<TData>(PropertyInfo property)
        {
            var getter = CreateGetter<TData, int[]>(property);
            return (object data, ref PreviewKeyHasher hasher) =>
            {
                int[] values = getter((TData)data);
                if (values == null) { hasher.Add(-1); return; }

                hasher.Add(values.Length);
                foreach (int value in values) { hasher.Add(value); }
            };
        }

        static PropertyHasher CreateFloatArray<TData>(PropertyInfo property)
        {
            var getter = CreateGetter<TData, float[]>(property);
            return (object data, ref PreviewKeyHasher hasher) => AddFloats(ref hasher, getter((TData)data));
        }

        static PropertyHasher CreateVector2Array<TData>(PropertyInfo property)
        {
            var getter = CreateGetter<TData, Vector2[]>(property);
            return (object data, ref PreviewKeyHasher hasher) => AddVertices(ref hasher, getter((TData)data));
        }

        static PropertyHasher CreateTimeToRanges<TData>(PropertyInfo property)
        {
            var getter = CreateGetter<TData, List<TimeToRange>>(property);
            return (object data, ref PreviewKeyHasher hasher) =>
            {
                List<TimeToRange> values = getter((TData)data);
                if (values == null) { hasher.Add(-1); return; }

                hasher.Add(values.Count);
                foreach (TimeToRange value in values)
                {
                    if (value == null) { hasher.Add(-1); continue; }

                    hasher.Add(value.Timing);
                    AddFloats(ref hasher, value.Range);
                }
            };
        }

        static PropertyHasher CreateTimeToVertices<TData>(PropertyInfo property)
        {
            var getter = CreateGetter<TData, List<TimeToVertices>>(property);
            return (object data, ref PreviewKeyHasher hasher) =>
            {
                List<TimeToVertices> values = getter((TData)data);
                if (values == null) { hasher.Add(-1); return; }

                hasher.Add(values.Count);
                foreach (TimeToVertices value in values)
                {
                    if (value == null) { hasher.Add(-1); continue; }

                    hasher.Add(value.Timing);
                    AddVertices(ref hasher, value.Vertices);
                }
            };
        }

        static void AddFloats(ref PreviewKeyHasher hasher, float[] values)
        {
            if (values == null) { hasher.Add(-1); return; }

            hasher.Add(values.Length);
            foreach (float value in values) { hasher.Add(value); }
        }

        static void AddVertices(ref PreviewKeyHasher hasher, Vector2[] values)
        {
            if (values == null) { hasher.Add(-1); return; }

            hasher.Add(values.Length);
            foreach (Vector2 value in values) { hasher.Add(value); }
        }
    }
}

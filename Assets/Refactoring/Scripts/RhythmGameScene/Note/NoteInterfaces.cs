using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    /// <summary>
    /// 各種ノーツデータの基となるインターフェース
    /// ビルダーパターンの使用
    /// </summary>
    public interface INoteData
    {
        public NoteType NoteType { get; }

        /// <summary>
        /// 楽曲開始からn秒後にノーツの判定
        /// </summary>
        public float Timing { get; set; }

        public ITimeGetter Timer { get; set; }

    }

    /// <summary>
    /// 各種ノーツの生成を行う
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INoteFactory<T> where T : INoteData
    {
        void Initialize(NoteFactoryInitializingData initializingData);

        NoteObject<T> Spawn(T data);
    }

    /// <summary>
    /// インスペクターで設定できるように基底クラスでラップ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NoteFactory<T> : MonoBehaviour, INoteFactory<T> where T : INoteData
    {
        public abstract void Initialize(NoteFactoryInitializingData initializingData);

        public abstract NoteObject<T> Spawn(T data);
    }
}

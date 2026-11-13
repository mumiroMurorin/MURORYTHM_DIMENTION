using System;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// ノーツデータからエディタ上のノーツオブジェクトを生成する
    /// (配置中の仮ノーツ・譜面上のノーツで共通)
    /// </summary>
    public static class NoteObjectFactory
    {
        /// <param name="noteList">ノーツの種類 → プレハブ の対応表</param>
        /// <param name="getParentTransform">アドレス → 設置場所</param>
        /// <param name="onChainNoteCreated">接続できるノーツ(ホールド等)のとき、オブジェクトを紐づけた直後に呼ばれる</param>
        /// <returns>生成できなかったとき null</returns>
        public static IDeployableObject Instantiate(
            NoteTypeToNoteObjectList noteList,
            IDeployableNoteData noteData,
            Func<IReadOnlyAddressWithinRange, Transform> getParentTransform,
            Action<IChainNoteData> onChainNoteCreated = null)
        {
            GameObject origin = noteList.GetNote(noteData.NoteType);
            if (origin == null)
            {
                Debug.LogWarning($"【Note】{noteData.NoteType}に対応するプレハブが登録されていません");
                return null;
            }

            GameObject obj = UnityEngine.Object.Instantiate(origin);

            if (!obj.TryGetComponent(out IDeployableObject deployable))
            {
                Debug.LogWarning("ノーツにIDeployableObjectがくっついてねぇぞ！");
                UnityEngine.Object.Destroy(obj);
                return null;
            }

            // チェインノーツのときデータセット
            if (noteData is IChainNoteData chainData)
            {
                chainData.SetNoteObject(obj.GetComponent<IConnectableObject>());
                onChainNoteCreated?.Invoke(chainData);
            }

            deployable.OnInstantiate(noteData, getParentTransform);

            return deployable;
        }
    }
}

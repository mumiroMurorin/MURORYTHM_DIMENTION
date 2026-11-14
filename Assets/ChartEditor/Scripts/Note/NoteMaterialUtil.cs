using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 譜面エディタのノーツのマテリアル(材質)を扱う補助
    /// </summary>
    public static class NoteMaterialUtil
    {
        /// <summary>
        /// マテリアルを差し替える。差し替える前に付いていた色は、差し替えた後のマテリアルへ引き継ぐ
        ///
        /// ホールド(スペースホールド)のノーツは、2つの処理が別々に見た目を決めている。
        /// ・〜Connectable: ノーツの種類(始点・中継点・終点)に合わせて、マテリアルを差し替える
        /// ・〜TypeChangable: ノーツの種類に合わせて、マテリアルに色を付ける (青色などは、この色で付いている。マテリアル自体は白)
        /// 色を付けた後でマテリアルを差し替えると、付けた色が消えて、白いノーツになってしまう。
        /// 2つの処理のどちらが先に動くかは決まっていない (それぞれ、ノーツのデータが用意できるのを待ってから動き出す) ので、
        /// 差し替えるときに色を引き継いで、どちらが先でも同じ見た目になるようにする。
        /// </summary>
        public static void ReplaceKeepingColor(Renderer renderer, Material material)
        {
            if (renderer == null || material == null) { return; }

            // 今の色。sharedMaterial で読む (material で読むと、読むだけでマテリアルの複製が作られる)
            Material current = renderer.sharedMaterial;
            bool hasColor = current != null && current.HasProperty(colorPropertyId);
            Color color = hasColor ? current.GetColor(colorPropertyId) : Color.white;

            renderer.material = material;

            // 引き継ぐ色は、このノーツ専用の複製に付ける (renderer.material は、このノーツ専用の複製を返す。元のマテリアルは変えない)
            if (hasColor && material.HasProperty(colorPropertyId)) { renderer.material.SetColor(colorPropertyId, color); }
        }

        static readonly int colorPropertyId = Shader.PropertyToID("_Color");
    }
}

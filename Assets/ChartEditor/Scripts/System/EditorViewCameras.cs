using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 編集画面を映しているカメラ
    ///
    /// アウトライン(選択・警告などの縁取り)を、関係のあるカメラにだけ描かせるために使う。
    /// 指定しないと、アウトラインはシーン内の全カメラ(ミニプレビュー用のカメラなど)に毎フレーム描かれ、
    /// アウトラインが出ている間ずっと余計な描画が走ってしまう。
    ///
    /// ノーツや頂点はプレハブから実行時に作られ、シーン上のカメラを直接参照できないので、
    /// カメラを持っている CursorInteracter がここに登録する。
    /// </summary>
    public static class EditorViewCameras
    {
        /// <summary>
        /// 譜面(レーン)を映しているカメラ
        /// </summary>
        public static Camera GroundView { get; set; }

        /// <summary>
        /// 頂点の編集画面を映しているカメラ
        /// </summary>
        public static Camera VerticesView { get; set; }
    }
}

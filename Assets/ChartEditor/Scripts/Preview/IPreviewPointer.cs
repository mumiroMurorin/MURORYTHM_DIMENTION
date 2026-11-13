namespace ChartEditor
{
    /// <summary>
    /// カーソルが、プレビュー(編集画面のミニプレビューと、全画面プレビュー)の中のどこを指しているか
    /// (貼り付けなど、「カーソルの位置」を基準にする操作を、プレビューの上でも行えるようにするためのもの)
    /// </summary>
    public interface IPreviewPointer
    {
        /// <summary>
        /// カーソルが、いずれかのプレビューの映像の上にあるか
        /// </summary>
        bool IsPointerOnPreview { get; }

        /// <summary>
        /// カーソルがプレビューの中で指している分線を求める
        /// </summary>
        /// <returns>プレビューの上に無いとき、ノーツの流れる面を指していないときは false</returns>
        bool TryGetPointedAddress(out AddressInChart address);
    }
}

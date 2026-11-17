namespace ChartEditor
{
    /// <summary>
    /// 表示するかどうかを、画面上のメニューから切り替えられる画面部品
    ///
    /// 番号は、シーン(ChartEditor.unity)のメニュー項目と、保存される設定(PlayerPrefs)に使われているので、後から変えない。
    /// 部品を足すときは、空いている番号を使う。
    /// </summary>
    public enum EditorPanel
    {
        // ---- 編集画面の欄 (メニューの「表示」で切り替える)
        /// <summary>譜面情報 (コンボ数、ノーツの種類ごとの数)</summary>
        ChartInfo = 1,
        /// <summary>全体マップ (譜面全体の縮小図)</summary>
        ChartOverview = 2,
        /// <summary>ログ欄 (画面右上)</summary>
        Log = 3,
        /// <summary>操作説明 (画面の右側の、今できる操作の一覧)</summary>
        OperationDescription = 4,

        // ---- クイックメニュー (編集画面の左下の設定欄) のセクション (メニューの「クイックメニュー」で切り替える)
        // 101 (楽曲選択)、104 (レーン分割数)、110 (ウィンドウサイズ)、111 (インポート・エクスポート) は、
        // クイックメニューから外したので欠番 (これらは、上のメニューから使う)
        QuickChartLength = 102,
        QuickOffset = 103,
        QuickPlaybackSpeed = 105,
        QuickNoteSound = 106,
        QuickScrollSensitivity = 107,
        QuickNoteSpeed = 108,
        QuickHitVolume = 109,
        QuickMusicVolume = 112,
        /// <summary>全画面プレビューで表示する奥行き (録音 20261117_051155)</summary>
        QuickVisibleDistance = 113,
        /// <summary>カーブの半径 (グラウンドとノーツの並ぶ曲線。大きいほど、ゆるやか)</summary>
        QuickCurveRadius = 114,
    }

    public static class EditorPanelExtension
    {
        /// <summary>
        /// クイックメニュー (左下の設定欄) のセクションか
        /// </summary>
        public static bool IsQuickMenuSection(this EditorPanel panel)
        {
            return (int)panel >= 100;
        }

        /// <summary>
        /// 一度も切り替えていないときに、表示するか
        /// 編集画面の欄は、すべて表示する。クイックメニューは、譜面を作るときによく使うものだけを表示する
        /// (ほかのセクションは、メニューの「クイックメニュー」でチェックを入れると出てくる)
        /// </summary>
        public static bool IsVisibleByDefault(this EditorPanel panel)
        {
            if (!panel.IsQuickMenuSection()) { return true; }

            return panel == EditorPanel.QuickChartLength ||
                   panel == EditorPanel.QuickOffset;
        }
    }
}

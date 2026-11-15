using System;
using System.Collections.Generic;
using System.IO;

namespace ChartEditor
{
    /// <summary>
    /// 譜面のバックアップ (復元に使えるファイル) の一覧
    ///
    /// バックアップは2種類ある。
    /// ・自動保存: 編集中、一定時間ごとに、自動保存のフォルダに作られる (ChartSaveGuard)。
    ///   名前は「autosave_譜面のあるフォルダの名前~~譜面の名前_日付_時刻.json」。新しいものから決まった数だけ残る
    ///   (譜面のファイル名は、曲が違っても同じことが多い (chart_master.json など) ので、フォルダの名前で曲を見分ける。
    ///    以前の自動保存には、フォルダの名前が入っていない。保存先が決まっていなかった譜面にも、入らない)
    /// ・上書き保存する前のファイル: 上書き保存のたびに、譜面のファイルの隣に「譜面のファイル名.bak」として残る (1つだけ)
    ///
    /// ここでは、ファイルを探して、新しい順に並べ、メニューに出す名前を作るだけを行う。
    /// 読み込みは ChartDataImporter が行う。
    /// </summary>
    public static class ChartBackups
    {
        // 上書き保存する前のファイルの、名前の末尾
        public const string PREVIOUS_SAVE_EXTENSION = ".bak";
        // 保存先が決まっていなかった譜面の、自動保存での名前
        const string UNTITLED_NAME = "untitled";
        // 自動保存のファイル名で、譜面のあるフォルダの名前と、譜面の名前を区切る文字
        public const string FOLDER_SEPARATOR = "~~";
        // メニューに出すとき、フォルダの名前と譜面の名前の間に入れる文字
        const string NAME_SEPARATOR = " / ";
        // メニューに出す名前の、長さの上限 (半角1文字を 1、全角1文字を 2 と数える。これより長い名前は、途中で切る)
        // バックアップの一覧の横幅 (シーンの BackupList) に収まる長さ。横幅を変えたら、ここも見直す
        const int AUTO_SAVE_NAME_UNITS = 30;
        const int PREVIOUS_SAVE_NAME_UNITS = 18;
        // フォルダの名前と譜面の名前を並べるとき、フォルダの名前に最低限残す長さ
        const int MIN_FOLDER_UNITS = 6;

        /// <summary>
        /// バックアップ1つぶん
        /// </summary>
        public struct Entry
        {
            /// <summary>ファイルの場所</summary>
            public string Path;
            /// <summary>譜面の名前 (保存先が決まっていなかった譜面は "untitled")</summary>
            public string ChartName;
            /// <summary>譜面のあるフォルダの名前 (分からないときは空。以前の自動保存、保存先が決まっていなかった譜面)</summary>
            public string FolderName;
            /// <summary>作られた日時 (上書き保存する前のファイルは、その内容が保存された日時)</summary>
            public DateTime Time;
            /// <summary>上書き保存する前のファイルか (false のときは、自動保存)</summary>
            public bool IsPreviousSave;
        }

        /// <summary>
        /// 復元に使えるバックアップを、新しい順に集める
        /// </summary>
        /// <param name="autoSaveDirectory">自動保存のフォルダ</param>
        /// <param name="autoSavePrefix">自動保存のファイル名の先頭</param>
        /// <param name="currentChartPath">今開いている譜面のファイル (保存先が決まっていなければ空)</param>
        /// <param name="maxCount">集める数の上限</param>
        /// <param name="results">結果の入れ先 (新しい順)</param>
        public static void Collect(string autoSaveDirectory, string autoSavePrefix, string currentChartPath, int maxCount, List<Entry> results)
        {
            results.Clear();

            // 自動保存
            try
            {
                if (!string.IsNullOrEmpty(autoSaveDirectory) && Directory.Exists(autoSaveDirectory))
                {
                    foreach (var file in new DirectoryInfo(autoSaveDirectory).GetFiles(autoSavePrefix + "*.json"))
                    {
                        ParseAutoSaveName(file.Name, autoSavePrefix, out string folderName, out string chartName);
                        results.Add(new Entry
                        {
                            Path = file.FullName,
                            ChartName = chartName,
                            FolderName = folderName,
                            Time = file.LastWriteTime,
                            IsPreviousSave = false,
                        });
                    }
                }
            }
            catch (Exception)
            {
                // フォルダを調べられないときは、見つかった分だけを出す
            }

            // 今開いている譜面の、上書き保存する前のファイル
            try
            {
                if (!string.IsNullOrEmpty(currentChartPath))
                {
                    var previous = new FileInfo(currentChartPath + PREVIOUS_SAVE_EXTENSION);
                    if (previous.Exists)
                    {
                        results.Add(new Entry
                        {
                            Path = previous.FullName,
                            ChartName = System.IO.Path.GetFileNameWithoutExtension(currentChartPath),
                            FolderName = GetFolderName(currentChartPath),
                            Time = previous.LastWriteTime,
                            IsPreviousSave = true,
                        });
                    }
                }
            }
            catch (Exception)
            {
            }

            // 新しい順 (同じ日時のものは、名前の順にして、並びが毎回同じになるようにする)
            results.Sort((a, b) =>
            {
                int order = b.Time.CompareTo(a.Time);
                return order != 0 ? order : string.CompareOrdinal(a.Path, b.Path);
            });

            if (maxCount >= 0 && results.Count > maxCount) { results.RemoveRange(maxCount, results.Count - maxCount); }
        }

        /// <summary>
        /// 自動保存のファイル名を作る (autosave_フォルダの名前~~譜面の名前_日付_時刻.json)
        /// </summary>
        /// <param name="chartPath">譜面のファイル (保存先が決まっていなければ空。そのときの名前は autosave_untitled_日付_時刻.json)</param>
        public static string CreateAutoSaveFileName(string autoSavePrefix, string chartPath, DateTime time)
        {
            string origin = UNTITLED_NAME;
            if (!string.IsNullOrWhiteSpace(chartPath))
            {
                string chartName = System.IO.Path.GetFileNameWithoutExtension(chartPath);
                string folderName = GetFolderName(chartPath);
                origin = folderName.Length > 0 ? folderName + FOLDER_SEPARATOR + chartName : chartName;
            }

            return $"{autoSavePrefix}{origin}_{time:yyyyMMdd_HHmmss}.json";
        }

        /// <summary>
        /// 譜面のファイルがあるフォルダの名前 (自動保存のファイル名に入れる形。分からないときは空)
        /// </summary>
        public static string GetFolderName(string chartPath)
        {
            if (string.IsNullOrWhiteSpace(chartPath)) { return string.Empty; }

            try
            {
                string directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(chartPath));
                if (string.IsNullOrEmpty(directory)) { return string.Empty; }

                // 区切りの文字と同じ並びは、名前の中に残さない (ファイル名から読み戻すときに、区切りと取り違えないように)
                return System.IO.Path.GetFileName(Normalize(directory)).Replace(FOLDER_SEPARATOR, "~");
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 自動保存のファイル名から、譜面の名前を取り出す
        /// </summary>
        public static string GetChartName(string fileName, string autoSavePrefix)
        {
            ParseAutoSaveName(fileName, autoSavePrefix, out _, out string chartName);
            return chartName;
        }

        /// <summary>
        /// 自動保存のファイル名 (autosave_フォルダの名前~~譜面の名前_日付_時刻.json) から、フォルダの名前と、譜面の名前を取り出す
        /// (フォルダの名前が入っていない、以前の自動保存では、フォルダの名前は空になる)
        /// </summary>
        public static void ParseAutoSaveName(string fileName, string autoSavePrefix, out string folderName, out string chartName)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
            if (!string.IsNullOrEmpty(autoSavePrefix) && name.StartsWith(autoSavePrefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(autoSavePrefix.Length);
            }

            // 末尾の「_日付(8桁)_時刻(6桁)」を取り除く (譜面の名前そのものに「_」が入っていてもよい)
            const int stampLength = 16;
            if (name.Length >= stampLength)
            {
                string stamp = name.Substring(name.Length - stampLength);
                bool isStamp = stamp[0] == '_' && stamp[9] == '_';
                for (int i = 1; isStamp && i < stampLength; i++)
                {
                    if (i != 9 && !char.IsDigit(stamp[i])) { isStamp = false; }
                }

                if (isStamp) { name = name.Substring(0, name.Length - stampLength); }
            }

            // フォルダの名前 (最初の区切りより前)
            folderName = string.Empty;
            int separator = name.IndexOf(FOLDER_SEPARATOR, StringComparison.Ordinal);
            if (separator >= 0)
            {
                folderName = name.Substring(0, separator);
                name = name.Substring(separator + FOLDER_SEPARATOR.Length);
            }

            chartName = name.Length == 0 ? UNTITLED_NAME : name;
        }

        /// <summary>
        /// メニューに出す名前
        /// ・自動保存: 「11/15 20:17  フォルダの名前 / 譜面の名前」 (フォルダの名前が分からないものは、譜面の名前だけ)
        /// ・上書き保存する前のファイル: 「11/15 20:17  譜面の名前（前回の保存）」 (今開いている譜面のものなので、フォルダの名前は出さない)
        /// 長い名前は、一覧の横幅に収まるように切る。フォルダの名前と並べるときは、譜面の名前を優先して残す
        /// </summary>
        public static string FormatLabel(Entry entry)
        {
            string chartName = entry.ChartName ?? string.Empty;
            string folderName = entry.FolderName ?? string.Empty;

            string name;
            if (entry.IsPreviousSave)
            {
                name = FitToUnits(chartName, PREVIOUS_SAVE_NAME_UNITS);
            }
            else if (folderName.Length == 0)
            {
                name = FitToUnits(chartName, AUTO_SAVE_NAME_UNITS);
            }
            else
            {
                int separatorUnits = CountUnits(NAME_SEPARATOR);
                string chart = FitToUnits(chartName, AUTO_SAVE_NAME_UNITS - separatorUnits - MIN_FOLDER_UNITS);
                string folder = FitToUnits(folderName, AUTO_SAVE_NAME_UNITS - separatorUnits - CountUnits(chart));
                name = folder + NAME_SEPARATOR + chart;
            }

            string label = $"{entry.Time:MM/dd HH:mm}  {name}";
            return entry.IsPreviousSave ? label + "（前回の保存）" : label;
        }

        /// <summary>
        /// 文字列の長さ (半角1文字を 1、全角1文字を 2 と数える)
        /// </summary>
        public static int CountUnits(string text)
        {
            int units = 0;
            foreach (char c in text ?? string.Empty) { units += IsHalfWidth(c) ? 1 : 2; }
            return units;
        }

        /// <summary>
        /// 決まった長さに収まるように、文字列を切る (切ったときは、末尾に「…」を付ける)
        /// </summary>
        public static string FitToUnits(string text, int maxUnits)
        {
            text = text ?? string.Empty;
            if (CountUnits(text) <= maxUnits) { return text; }

            const string ellipsis = "…";
            int limit = maxUnits - CountUnits(ellipsis);
            int units = 0;
            int length = 0;
            while (length < text.Length)
            {
                int next = units + (IsHalfWidth(text[length]) ? 1 : 2);
                if (next > limit) { break; }

                units = next;
                length++;
            }

            return text.Substring(0, length) + ellipsis;
        }

        private static bool IsHalfWidth(char c)
        {
            // 英数字・記号と、半角カタカナ
            return c <= 0x7E || (c >= 0xFF61 && c <= 0xFF9F);
        }

        /// <summary>
        /// そのファイルが、バックアップか (自動保存のフォルダにあるファイルか、上書き保存する前のファイル)
        /// </summary>
        public static bool IsBackupPath(string path, string autoSaveDirectory)
        {
            if (string.IsNullOrEmpty(path)) { return false; }
            if (path.EndsWith(PREVIOUS_SAVE_EXTENSION, StringComparison.OrdinalIgnoreCase)) { return true; }
            if (string.IsNullOrEmpty(autoSaveDirectory)) { return false; }

            try
            {
                string directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
                return string.Equals(Normalize(directory), Normalize(System.IO.Path.GetFullPath(autoSaveDirectory)), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// そのバックアップが、今開いている譜面のものか
        /// (自動保存は、譜面の名前と、譜面のあるフォルダの名前が、どちらも同じもの。
        ///  上書き保存する前のファイルは、今の譜面のファイルの隣にあるもの)
        ///
        /// 「今開いている譜面のもの」と決まったバックアップだけが、復元した後も保存先をそのままにできる。
        /// 譜面の名前だけで決めると、別の曲の chart_master.json のバックアップを復元して上書き保存したときに、
        /// 今の曲のファイルを書き換えてしまう。フォルダの名前が入っていない以前の自動保存は、どの曲のものか決められないので、
        /// 今の譜面のものとは扱わない (保存するときに、保存先を選び直してもらう)
        /// </summary>
        /// <param name="backupPath">バックアップのファイル</param>
        /// <param name="currentChartPath">今開いている譜面のファイル (保存先が決まっていなければ空)</param>
        public static bool IsBackupOf(string backupPath, string currentChartPath, string autoSavePrefix)
        {
            if (string.IsNullOrEmpty(backupPath) || string.IsNullOrEmpty(currentChartPath)) { return false; }

            try
            {
                if (backupPath.EndsWith(PREVIOUS_SAVE_EXTENSION, StringComparison.OrdinalIgnoreCase))
                {
                    string original = backupPath.Substring(0, backupPath.Length - PREVIOUS_SAVE_EXTENSION.Length);
                    return string.Equals(Normalize(System.IO.Path.GetFullPath(original)), Normalize(System.IO.Path.GetFullPath(currentChartPath)), StringComparison.OrdinalIgnoreCase);
                }

                ParseAutoSaveName(System.IO.Path.GetFileName(backupPath), autoSavePrefix, out string backupFolder, out string backupName);
                if (backupFolder.Length == 0) { return false; }

                string currentName = System.IO.Path.GetFileNameWithoutExtension(currentChartPath);
                return string.Equals(backupName, currentName, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(backupFolder, GetFolderName(currentChartPath), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('/', '\\').TrimEnd('\\');
        }
    }
}

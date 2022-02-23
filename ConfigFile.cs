using System.Collections.Generic;

namespace LosslessCut
{
    public class ConfigFile
    {
        /// <summary>
        /// 設定ファイル名
        /// </summary>
        public const string ConfigFileName = "Config.txt";

        /// <summary>
        /// 区切り文字
        /// </summary>
        public const char Delimiter = '\t';

        /// <summary>
        /// 設定ファイルパス取得
        /// </summary>
        /// <returns>設定ファイルのフルパス。存在しない場合は空文字列を返す。</returns>
        public string GetPath()
        {
            // EXEのパス取得
            string appPath = App.GetAppPath();

            // 設定ファイルのフルパス組み立て
            return System.IO.Path.Combine(appPath, ConfigFileName);
        }
    }
}

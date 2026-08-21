using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CmnClsLib.Interface;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace CmnWinLib.Class
{
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]

    public partial class ClsIniFile
    {
        [LibraryImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringW", StringMarshalling = StringMarshalling.Utf16)]
        public static partial uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, [Out] char[] lpReturnedString, uint nSize, string lpFileName);

        [LibraryImport("kernel32.dll", EntryPoint = "GetPrivateProfileIntW", StringMarshalling = StringMarshalling.Utf16)]
        public static partial uint GetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault, string lpFileName);

        [LibraryImport("kernel32.dll", EntryPoint = "WritePrivateProfileStringW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int WritePrivateProfileString(string lpApplicationName, string lpKeyName, string? lpString, string lpFileName);

        private readonly ICmnLogger _logger;
        private string _filePath = string.Empty;

        /// <summary>
        /// 指定されたロガーを使用して <see cref="ClsIniFile"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力用のロガーオブジェクト。</param>
        /// <example>
        /// <code>
        /// ICmnLogger logger = new CmnLogger();
        /// ClsIniFile iniFile = new ClsIniFile(logger);
        /// </code>
        /// </example>
        public ClsIniFile(ICmnLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 操作対象となる INI ファイルの絶対パスまたは相対パスを取得または設定します。
        /// </summary>
        /// <example>
        /// <code>
        /// iniFile.FilePath = @"C:\Config\settings.ini";
        /// </code>
        /// </example>
        public string FilePath
        {
            get => _filePath;
            set => _filePath = value ?? string.Empty;
        }

        /// <summary>
        /// INI ファイルの指定されたセクションおよびキーから文字列値を取得します。
        /// </summary>
        /// <param name="section">検索するセクション名。</param>
        /// <param name="key">取得するキー名。</param>
        /// <param name="defaultValue">キーが存在しない場合や取得失敗時に返されるデフォルト値。</param>
        /// <returns>取得された文字列値。該当するキーが存在しない場合は <paramref name="defaultValue"/> を返します。</returns>
        /// <example>
        /// <code>
        /// string serverName = iniFile.ReadString("Database", "Server", "localhost");
        /// </code>
        /// </example>
        public string ReadString(string section, string? key, string defaultValue)
        {
            section ??= string.Empty;
            key ??= string.Empty;
            defaultValue ??= string.Empty;

            char[] buffer = new char[1024];
            uint size = GetPrivateProfileString(
                section,
                key,
                defaultValue,
                buffer,
                (uint)buffer.Length,
                _filePath);

            return new string(buffer, 0, (int)size);
        }

        /// <summary>
        /// INI ファイルの指定されたセクションおよびキーから 32 ビット符号付き整数値を取得します。
        /// </summary>
        /// <param name="section">検索するセクション名。</param>
        /// <param name="key">取得するキー名。</param>
        /// <param name="defaultValue">キーが存在しない場合や取得失敗時に返されるデフォルト値。</param>
        /// <returns>取得された整数値。該当するキーが存在しない場合は <paramref name="defaultValue"/> を返します。</returns>
        /// <example>
        /// <code>
        /// int port = iniFile.ReadInt32("Database", "Port", 3306);
        /// </code>
        /// </example>
        public int ReadInt32(string section, string key, int defaultValue)
        {
            section ??= string.Empty;
            key ??= string.Empty;
            return (int)GetPrivateProfileInt(section, key, defaultValue, _filePath);
        }

        /// <summary>
        /// INI ファイルの指定されたセクションおよびキーに文字列値を書き込みます。
        /// </summary>
        /// <param name="section">書き込み先のセクション名。</param>
        /// <param name="key">書き込み先のキー名。</param>
        /// <param name="value">書き込む文字列値。</param>
        /// <example>
        /// <code>
        /// iniFile.WriteString("Database", "Server", "192.168.1.10");
        /// </code>
        /// </example>
        public void WriteString(string section, string key, string? value)
        {
            section ??= string.Empty;
            key ??= string.Empty;
            value ??= string.Empty;
            WritePrivateProfileString(section, key, value, _filePath);
        }

        /// <summary>
        /// INI ファイルの指定されたセクションおよびキーに 32 ビット符号付き整数値を書き込みます。
        /// </summary>
        /// <param name="section">書き込み先のセクション名。</param>
        /// <param name="key">書き込み先のキー名。</param>
        /// <param name="value">書き込む整数値。</param>
        /// <example>
        /// <code>
        /// iniFile.WriteInt32("Database", "Port", 8080);
        /// </code>
        /// </example>
        public void WriteInt32(string section, string key, int value)
        {
            WriteString(section, key, value.ToString());
        }

        // --- 非推奨互換メソッド ---

        /// <summary>
        /// INI ファイルから文字列値を取得します。（非推奨：代わりに <see cref="ReadString(string, string?, string)"/> を使用してください）
        /// </summary>
        /// <param name="section">セクション名。</param>
        /// <param name="key">キー名。</param>
        /// <param name="defaultValue">デフォルト値。</param>
        /// <returns>取得した文字列値。</returns>
        /// <example>
        /// <code>
        /// string val = iniFile.GetValueString("Section", "Key", "Default");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'ReadString(section, key, defaultValue)' を使用します。")]
        public string GetValueString(string section, string? key, string defaultValue)
        {
            return ReadString(section, key, defaultValue);
        }

        /// <summary>
        /// INI ファイルから整数値を取得します。（非推奨：代わりに <see cref="ReadInt32(string, string, int)"/> を使用してください）
        /// </summary>
        /// <param name="section">セクション名。</param>
        /// <param name="key">キー名。</param>
        /// <param name="defaultValue">デフォルト値。</param>
        /// <returns>取得した整数値。</returns>
        /// <example>
        /// <code>
        /// int val = iniFile.GetValueInt("Section", "Key", 0);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'ReadInt32(section, key, defaultValue)' を使用します。")]
        public int GetValueInt(string section, string key, int defaultValue)
        {
            return ReadInt32(section, key, defaultValue);
        }

        /// <summary>
        /// INI ファイルに整数値を設定します。（非推奨：代わりに <see cref="WriteInt32(string, string, int)"/> を使用してください）
        /// </summary>
        /// <param name="section">セクション名。</param>
        /// <param name="key">キー名。</param>
        /// <param name="value">設定する整数値。</param>
        /// <example>
        /// <code>
        /// iniFile.SetValue("Section", "Key", 100);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteInt32(section, key, value)' を使用します。")]
        public void SetValue(string section, string key, int value)
        {
            WriteInt32(section, key, value);
        }

        /// <summary>
        /// INI ファイルに文字列値を設定します。（非推奨：代わりに <see cref="WriteString(string, string, string?)"/> を使用してください）
        /// </summary>
        /// <param name="section">セクション名。</param>
        /// <param name="key">キー名。</param>
        /// <param name="value">設定する文字列値。</param>
        /// <example>
        /// <code>
        /// iniFile.SetValue("Section", "Key", "Value");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteString(section, key, value)' を使用します。")]
        public void SetValue(string section, string key, string? value)
        {
            WriteString(section, key, value);
        }
    }
}

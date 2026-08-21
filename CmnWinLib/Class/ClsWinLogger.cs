using CmnClsLib.Class;
using CmnClsLib.Interface;
using CmnClsLib.Module;
using System;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace CmnWinLib.Class
{
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Windows環境に対応した共通ロガークラスです。
    /// コンソール、ファイル、Windows イベントログへのメッセージ出力を提供します。
    /// </summary>
    public class ClsWinLogger : ICmnLogger
    {
        // 定数
        public const string IS_EVENTLOG = "isEventlog";
        public const string IS_EVENTLOG_INFO = "isEventlogInfo";
        public const string IS_EVENTLOG_WARN = "isEventlogWarn";
        public const string IS_EVENTLOG_ERROR = "isEventlogError";
        public const string IS_EVENTLOG_SKIP = "isEventlogSkip";
        public const string LOG_LEVEL = "logLevel";
        public const string VERBOSE = "verbose";
        public const string EVENTLOG_VERBOSE = "eventLogVerbose";
        public const string COUNT_ERROR = "cntError";
        public const string COUNT_WARN = "cntWarn";

        // ClsLogger
        private readonly Lock _fileLock = new();
        private volatile bool _isStdErr = false;
        private volatile bool _isStdOut = false;
        private volatile bool _isConsole = true;
        private volatile bool _isFile = false;
        private volatile bool _isAppend = true;
        private volatile bool _isFlush = false;
        private volatile bool _isTrimEnd = true;
        private volatile bool _isTrimConsole = true;
        private volatile bool _isConsoleEncoding = false;
        private volatile string _dir = "";
        private volatile string _path = "";
        private volatile string _baseName = "";
        private volatile string _fileName = "";
        private volatile System.Text.Encoding _consoleEncoding = System.Text.Encoding.Default;
        private volatile System.Text.Encoding _fileEncoding = System.Text.Encoding.Default;
        // ClsWinLogger
        private volatile ClsEventLog? _eventLog = null;
        private volatile bool _isEventlog = false;
        private volatile bool _isEventlogInfo = false;
        private volatile bool _isEventlogWarn = false;
        private volatile bool _isEventlogError = false;
        private volatile bool _isEventlogSkip = false;
        private volatile int _logLevel = MdlConst.LVL_DEBUG;
        private volatile int _verbose = 0;
        private volatile int _eventLogVerbose = 0;
        private volatile int _cntError = 0;
        private volatile int _cntWarn = 0;
        private readonly bool _isWindows = false;

        /// <summary>
        /// <see cref="ClsWinLogger"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <example>
        /// <code>
        /// var logger = new ClsWinLogger();
        /// </code>
        /// </example>
        public ClsWinLogger()
        {
            if (OperatingSystem.IsWindows()) _isWindows = true;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// 指定されたファイルエンコーディング名を使用して、<see cref="ClsWinLogger"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="encoding">ファイル出力用のエンコーディング名（例: "utf-8", "shift_jis"）</param>
        /// <example>
        /// <code>
        /// var logger = new ClsWinLogger("utf-8");
        /// </code>
        /// </example>
        public ClsWinLogger(string encoding)
        {
            if (OperatingSystem.IsWindows()) _isWindows = true;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            SetValueByKey(ClsLogger.FILE_ENCODING, encoding);           // ファイル出力エンコーディング
        }

        /// <summary>
        /// イベントログ出力オブジェクトを取得または設定します。
        /// </summary>
        public ClsEventLog? EventLog { get => _eventLog; set => _eventLog = value; }

        /// <summary>
        /// 発生したエラーログのカウント数を取得または設定します。
        /// </summary>
        public int CntError { get => _cntError; set => _cntError = value; }

        /// <summary>
        /// 発生した警告ログのカウント数を取得または設定します。
        /// </summary>
        public int CntWarn { get => _cntWarn; set => _cntWarn = value; }

        /// <summary>
        /// 指定されたキーに対応するロガーの設定プロパティ値を設定します。
        /// </summary>
        /// <param name="key">プロパティ名（例: "isFile", "dir", "logLevel" など）</param>
        /// <param name="value">設定する値</param>
        /// <example>
        /// <code>
        /// logger.SetValueByKey("isFile", "true");
        /// logger.SetValueByKey("dir", @"C:\Logs");
        /// </code>
        /// </example>
        public void SetValueByKey(string key, string value)
        {
            switch (key)
            {
                case ClsLogger.IS_STDOUT:
                    _isStdOut = MdlUtil.IsTrue(value, false);
                    break;
                case ClsLogger.IS_STDERR:
                    _isStdErr = MdlUtil.IsTrue(value, false);
                    break;
                case ClsLogger.IS_CONSOLE:
                    _isConsole = MdlUtil.IsTrue(value, true);
                    break;
                case ClsLogger.IS_FILE:
                    _isFile = MdlUtil.IsTrue(value, false);
                    break;
                case ClsLogger.IS_APPEND:
                    _isAppend = MdlUtil.IsTrue(value, true);
                    break;
                case ClsLogger.IS_FLUSH:
                    _isFlush = MdlUtil.IsTrue(value, false);
                    break;
                case ClsLogger.IS_TRIM_END:
                    _isTrimEnd = MdlUtil.IsTrue(value, true);
                    break;
                case ClsLogger.IS_TRIM_CONSOLE:
                    _isTrimConsole = MdlUtil.IsTrue(value, true);
                    break;
                case ClsLogger.IS_CONSOLE_ENCODING:
                    _isConsoleEncoding = MdlUtil.IsTrue(value, false);
                    break;
                case ClsLogger.DIR:
                    _dir = value;
                    break;
                case ClsLogger.PATH:
                    _path = value;
                    break;
                case ClsLogger.BASENAME:
                    _baseName = value;
                    break;
                case ClsLogger.FILENAME:
                    _fileName = value;
                    break;
                case ClsLogger.CONSOLE_ENCODING:
                    _consoleEncoding = MdlUtil.GetEncoding(value);
                    break;
                case ClsLogger.FILE_ENCODING:
                    _fileEncoding = MdlUtil.GetEncoding(value);
                    break;
                case ClsWinLogger.IS_EVENTLOG:
                    _isEventlog = MdlUtil.IsTrue(value, false);
                    break;
                case ClsWinLogger.IS_EVENTLOG_INFO:
                    _isEventlogInfo = MdlUtil.IsTrue(value, false);
                    break;
                case ClsWinLogger.IS_EVENTLOG_WARN:
                    _isEventlogWarn = MdlUtil.IsTrue(value, false);
                    break;
                case ClsWinLogger.IS_EVENTLOG_ERROR:
                    _isEventlogError = MdlUtil.IsTrue(value, false);
                    break;
                case ClsWinLogger.IS_EVENTLOG_SKIP:
                    _isEventlogSkip = MdlUtil.IsTrue(value, false);
                    break;
                case ClsWinLogger.LOG_LEVEL:
                    _logLevel = MdlUtil.ParseInt(value, MdlConst.LVL_DEBUG);
                    break;
                case ClsWinLogger.VERBOSE:
                    _verbose = MdlUtil.ParseInt(value, 0);
                    break;
                case ClsWinLogger.EVENTLOG_VERBOSE:
                    _eventLogVerbose = MdlUtil.ParseInt(value, 0);
                    break;
            }
        }

        /// <summary>
        /// 指定されたキーに対応するロガーの設定プロパティ値を設定します。（旧式非推奨）
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="val">設定する値</param>
        /// <example>
        /// <code>
        /// logger.SetValByKey("isFile", "true");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'SetValueByKey(string, string)' を使用します。")]
        public void SetValByKey(string key, string val)
        {
            SetValueByKey(key, val);
        }

        /// <summary>
        /// 指定されたキーに対応するロガーの設定プロパティ値（文字列）を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>プロパティの文字列値、またはデフォルト値</returns>
        /// <example>
        /// <code>
        /// string dir = logger.GetValueByKey("dir", @"C:\Logs");
        /// </code>
        /// </example>
        public string GetValueByKey(string key, string defaultValue)
        {
            string value = defaultValue;
            switch (key)
            {
                case ClsLogger.IS_STDOUT:
                case ClsLogger.IS_STDERR:
                case ClsLogger.IS_CONSOLE:
                case ClsLogger.IS_FILE:
                case ClsLogger.IS_APPEND:
                case ClsLogger.IS_FLUSH:
                case ClsLogger.IS_TRIM_END:
                case ClsLogger.IS_TRIM_CONSOLE:
                case ClsLogger.IS_CONSOLE_ENCODING:
                    value = GetValueByKey(key, MdlUtil.IsTrue(defaultValue, false)).ToString();
                    break;
                case ClsWinLogger.LOG_LEVEL:
                case ClsWinLogger.VERBOSE:
                case ClsWinLogger.EVENTLOG_VERBOSE:
                    value = GetValueByKey(key, MdlUtil.ParseInt(defaultValue, 0)).ToString();
                    break;
                case ClsLogger.DIR:
                    value = _dir;
                    break;
                case ClsLogger.PATH:
                    value = _path;
                    break;
                case ClsLogger.BASENAME:
                    value = _baseName;
                    break;
                case ClsLogger.FILENAME:
                    value = _fileName;
                    break;
                case ClsLogger.CONSOLE_ENCODING:
                    value = MdlUtil.GetEncodingName(_consoleEncoding);
                    break;
                case ClsLogger.FILE_ENCODING:
                    value = MdlUtil.GetEncodingName(_fileEncoding);
                    break;
                case ClsWinLogger.COUNT_ERROR:
                    value = _cntError.ToString();
                    break;
                case ClsWinLogger.COUNT_WARN:
                    value = _cntWarn.ToString();
                    break;
            }
            return value;
        }

        /// <summary>
        /// 指定されたキーに対応するロガーの設定プロパティ値（文字列）を取得します。（旧式非推奨）
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>プロパティの文字列値、またはデフォルト値</returns>
        /// <example>
        /// <code>
        /// string dir = logger.GetValByKey("dir", @"C:\Logs");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetValueByKey(string, string)' を使用します。")]
        public string GetValByKey(string key, string defaultValue)
        {
            return GetValueByKey(key, defaultValue);
        }

        /// <summary>
        /// 指定されたキーに対応するロガーの設定プロパティ値（真偽値）を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>プロパティの真偽値、またはデフォルト値</returns>
        /// <example>
        /// <code>
        /// bool isFile = logger.GetValueByKey("isFile", false);
        /// </code>
        /// </example>
        public bool GetValueByKey(string key, bool defaultValue)
        {
            bool value = defaultValue;
            switch (key)
            {
                case ClsLogger.IS_STDOUT:
                    value = _isStdOut;
                    break;
                case ClsLogger.IS_STDERR:
                    value = _isStdErr;
                    break;
                case ClsLogger.IS_CONSOLE:
                    value = _isConsole;
                    break;
                case ClsLogger.IS_FILE:
                    value = _isFile;
                    break;
                case ClsLogger.IS_APPEND:
                    value = _isAppend;
                    break;
                case ClsLogger.IS_FLUSH:
                    value = _isFlush;
                    break;
                case ClsLogger.IS_TRIM_END:
                    value = _isTrimEnd;
                    break;
                case ClsLogger.IS_TRIM_CONSOLE:
                    value = _isTrimConsole;
                    break;
                case ClsLogger.IS_CONSOLE_ENCODING:
                    value = _isConsoleEncoding;
                    break;
                case ClsWinLogger.IS_EVENTLOG:
                    value = _isEventlog;
                    break;
                case ClsWinLogger.IS_EVENTLOG_INFO:
                    value = _isEventlogInfo;
                    break;
                case ClsWinLogger.IS_EVENTLOG_WARN:
                    value = _isEventlogWarn;
                    break;
                case ClsWinLogger.IS_EVENTLOG_ERROR:
                    value = _isEventlogError;
                    break;
                case ClsWinLogger.IS_EVENTLOG_SKIP:
                    value = _isEventlogSkip;
                    break;
            }
            return value;
        }

        /// <summary>
        /// 指定されたキーに対応するロガーの設定プロパティ値（真偽値）を取得します。（旧式非推奨）
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>プロパティの真偽値、またはデフォルト値</returns>
        /// <example>
        /// <code>
        /// bool isFile = logger.GetValByKey("isFile", false);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetValueByKey(string, bool)' を使用します。")]
        public bool GetValByKey(string key, bool defaultValue)
        {
            return GetValueByKey(key, defaultValue);
        }

        /// <summary>
        /// 指定されたキーに対応するロガーの設定プロパティ値（整数値）を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>プロパティの整数値、またはデフォルト値</returns>
        /// <example>
        /// <code>
        /// int logLevel = logger.GetValueByKey("logLevel", 0);
        /// </code>
        /// </example>
        public int GetValueByKey(string key, int defaultValue)
        {
            int value = defaultValue;
            switch (key)
            {
                case ClsWinLogger.LOG_LEVEL:
                    value = _logLevel;
                    break;
                case ClsWinLogger.VERBOSE:
                    value = _verbose;
                    break;
                case ClsWinLogger.EVENTLOG_VERBOSE:
                    value = _eventLogVerbose;
                    break;
            }
            return value;
        }

        /// <summary>
        /// 指定されたキーに対応するロガーの設定プロパティ値（整数値）を取得します。（旧式非推奨）
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>プロパティの整数値、またはデフォルト値</returns>
        /// <example>
        /// <code>
        /// int logLevel = logger.GetValByKey("logLevel", 0);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetValueByKey(string, int)' を使用します。")]
        public int GetValByKey(string key, int defaultValue)
        {
            return GetValueByKey(key, defaultValue);
        }

        /// <summary>
        /// 指定されたエラーレベルでログメッセージを書き込みます。
        /// </summary>
        /// <param name="errorLevel">エラーレベル（例: LVL_INFO, LVL_ERROR など）</param>
        /// <param name="line">ログメッセージ</param>
        /// <example>
        /// <code>
        /// logger.WriteLine(1, "処理を開始しました。");
        /// </code>
        /// </example>
        public void WriteLine(int errorLevel, string? line)
        {
            WriteLine(errorLevel, -1, line, ClsEventLog.EVENTLOG_MODE_OFF);
        }

        /// <summary>
        /// 指定されたエラーレベルでログメッセージを書き込みます。（旧式非推奨）
        /// </summary>
        /// <param name="errorLevel">エラーレベル</param>
        /// <param name="line">ログメッセージ</param>
        /// <example>
        /// <code>
        /// logger.Writeln(1, "処理を開始しました。");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteLine(int, string?)' を使用します。")]
        public void Writeln(int errorLevel, string? line)
        {
            WriteLine(errorLevel, line);
        }

        /// <summary>
        /// 指定されたエラーレベルおよびイベントログモードの設定でログメッセージを書き込みます。
        /// </summary>
        /// <param name="errorLevel">エラーレベル</param>
        /// <param name="line">ログメッセージ</param>
        /// <param name="isEventLogModeOn">イベントログモードがオンかどうか</param>
        /// <example>
        /// <code>
        /// logger.WriteLine(3, "エラーが発生しました。", true);
        /// </code>
        /// </example>
        public void WriteLine(int errorLevel, string? line, bool isEventLogModeOn)
        {
            WriteLine(errorLevel, -1, line, isEventLogModeOn);
        }

        /// <summary>
        /// 指定されたエラーレベルおよびイベントログモードの設定でログメッセージを書き込みます。（旧式非推奨）
        /// </summary>
        /// <param name="errorLevel">エラーレベル</param>
        /// <param name="line">ログメッセージ</param>
        /// <param name="isEventLogModeOn">イベントログモードがオンかどうか</param>
        /// <example>
        /// <code>
        /// logger.Writeln(3, "エラーが発生しました。", true);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteLine(int, string?, bool)' を使用します。")]
        public void Writeln(int errorLevel, string? line, bool isEventLogModeOn)
        {
            WriteLine(errorLevel, line, isEventLogModeOn);
        }

        /// <summary>
        /// 新しい <see cref="ClsEventLog"/> オブジェクトを作成および初期化します。
        /// </summary>
        /// <param name="eventSource">イベントソース名</param>
        /// <example>
        /// <code>
        /// logger.CreateEventLog("MyAppSource");
        /// </code>
        /// </example>
        public void CreateEventLog(string eventSource)
        {
            if (_isWindows)
            {
                _eventLog = new(this)
                {
                    Verbose = _eventLogVerbose,
                    SourceName = eventSource
                };
                _eventLog.Initialize();
            }
        }

        /// <summary>
        /// 新しい <see cref="ClsEventLog"/> オブジェクトを作成および初期化します。（旧式非推奨）
        /// </summary>
        /// <param name="eventSource">イベントソース名</param>
        /// <example>
        /// <code>
        /// logger.NewClsEventLog("MyAppSource");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'CreateEventLog(string)' を使用します。")]
        public void NewClsEventLog(string eventSource)
        {
            CreateEventLog(eventSource);
        }

        /// <summary>
        /// 条件判定を行ってコンソール、ファイル、イベントログへメッセージを出力する内部共通メソッドです。
        /// </summary>
        /// <param name="errorLevel">エラーレベル</param>
        /// <param name="outputLevel">出力レベル</param>
        /// <param name="line">ログメッセージ</param>
        /// <param name="isEventLogModeOn">イベントログモードがオンかどうか</param>
        private void WriteLine(int errorLevel, int outputLevel, string? line, bool isEventLogModeOn)
        {
            if (errorLevel < _logLevel) return;
            if (outputLevel > 0 && _verbose < outputLevel) return;
            bool isStdErr = _isStdErr;
            switch (errorLevel)
            {
                case MdlConst.LVL_W:
                    if (_cntWarn > (MdlConst.INT_MAX - 100)) _cntWarn = 0;
                    _cntWarn++;
                    isStdErr = true;
                    break;
                case MdlConst.LVL_E:
                case MdlConst.LVL_F:
                    if (_cntError > (MdlConst.INT_MAX - 100)) _cntError = 0;
                    _cntError++;
                    isStdErr = true;
                    break;
            }
            switch (errorLevel)
            {
                case MdlConst.LVL_DEBUG:
                case MdlConst.LVL_I:
                case MdlConst.LVL_W:
                case MdlConst.LVL_E:
                    line = MdlDate.GetFormattedDate("yyyy/MM/dd HH:mm:ss") + " " + MdlLog.GetLogLevelPrefix(errorLevel) + line;
                    break;
                default:
                    line = MdlLog.GetLogLevelPrefix(errorLevel) + line;
                    break;
            }
            string trimEndLine = line ?? string.Empty;
            if (_isTrimEnd) trimEndLine = trimEndLine.TrimEnd();
            if (_isConsole) WriteToConsole((!_isStdOut && isStdErr), (_isTrimConsole ? trimEndLine : (line ?? string.Empty)));
            WriteToFile(trimEndLine);
            if (isEventLogModeOn)
            {
                WriteEventLog(errorLevel, trimEndLine);
            }
            else
            {
                switch (errorLevel)
                {
                    case MdlConst.LVL_I:
                        if (_isEventlogInfo) WriteEventLog(errorLevel, trimEndLine);
                        break;
                    case MdlConst.LVL_W:
                        if (_isEventlogWarn) WriteEventLog(errorLevel, trimEndLine);
                        break;
                    case MdlConst.LVL_E:
                        if (_isEventlogError) WriteEventLog(errorLevel, trimEndLine);
                        break;
                }
            }
        }

        /// <summary>
        /// コンソールにログメッセージを出力する内部メソッドです。
        /// </summary>
        /// <param name="isStdErr">標準エラー出力に書き込むかどうか</param>
        /// <param name="line">ログメッセージ</param>
        private void WriteToConsole(bool isStdErr, string line)
        {
            try
            {
                if (_isConsoleEncoding) Console.OutputEncoding = _consoleEncoding;
                if (isStdErr)
                {
                    Console.Error.WriteLine(line);
                }
                else
                {
                    Console.Out.WriteLine(line);
                }
            }
            catch { }
        }

        /// <summary>
        /// ログメッセージをファイルに書き込む内部メソッドです。
        /// </summary>
        /// <param name="line">書き込むログメッセージ</param>
        private void WriteToFile(string line)
        {
            if (!_isFile) return;
            string currentPath;
            if (string.IsNullOrEmpty(_path))
            {
                if (string.IsNullOrEmpty(_fileName))
                {
                    if (string.IsNullOrEmpty(_baseName)) _baseName = MdlApp.GetAppNameWithHostName();
                    currentPath = Path.Combine(_dir, MdlLog.GenerateLogFileName(_baseName));
                }
                else
                {
                    currentPath = Path.Combine(_dir, _fileName);
                }
            }
            else
            {
                currentPath = _path;
            }
            MdlFile.CreateDirectory(MdlFile.GetDirectoryPath(currentPath));
            lock (_fileLock)
            {
                try
                {
                    using StreamWriter sw = new(currentPath, _isAppend, _fileEncoding);
                    sw.WriteLine(line);
                    if (_isFlush) sw.Flush();
                }
                catch (Exception ex)
                {
                    _isFile = false;
                    WriteToConsole(true, "ERROR [Logger.WriteToFile()] EXCEPTION : " + ex.Message);
                }
                finally
                {
                    _isAppend = true;
                }
            }
        }

        /// <summary>
        /// Windows イベントログにメッセージを書き込む内部メソッドです。
        /// </summary>
        /// <param name="errorLevel">エラーレベル</param>
        /// <param name="message">ログメッセージ</param>
        private void WriteEventLog(int errorLevel, string message)
        {
            if (!_isWindows) return;
            if (!_isEventlogSkip)
            {
                if (_isEventlogInfo || _isEventlogWarn || _isEventlogError || _isEventlog)
                {
                    if (_eventLogVerbose > 0) WriteLine(MdlConst.LVL_NONE, "[ClsLog.WriteEventLog()] START");
                    _eventLog ??= new ClsEventLog(this) { Verbose = _eventLogVerbose, SourceName = _baseName };
                    if (!_eventLog.IsInit) _eventLog.Initialize();
                    if (_eventLog.IsInit) _eventLog.WriteEvent(errorLevel, message);
                }
            }
        }

    }
}

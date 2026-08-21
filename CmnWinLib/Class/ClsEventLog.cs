using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Interface;
using CmnClsLib.Module;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace CmnWinLib.Class
{
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Windows イベントログへの書き込みおよび管理機能を提供するクラスです。
    /// </summary>
    /// <param name="logger">ログ出力に使用する <see cref="ICmnLogger"/> インターフェース</param>
    /// <example>
    /// <code>
    /// ICmnLogger logger = new ClsLogger();
    /// ClsEventLog eventLog = new ClsEventLog(logger);
    /// eventLog.SourceName = "MyApp";
    /// if (eventLog.Initialize())
    /// {
    ///     eventLog.WriteInfo("アプリケーションが起動しました。");
    /// }
    /// </code>
    /// </example>
    public class ClsEventLog(ICmnLogger logger)
    {
        /// <summary>イベントログ機能有効フラグ</summary>
        public const bool EVENTLOG_MODE_ON = true;

        /// <summary>イベントログ機能無効フラグ</summary>
        public const bool EVENTLOG_MODE_OFF = false;

        private readonly ICmnLogger _logger = logger;
        private System.Diagnostics.EventLog _eventLog = new();

        /// <summary>イベントソース名を取得または設定します。</summary>
        public string SourceName { get; set; } = "Application";

        /// <summary>ログ名を取得または設定します。</summary>
        public string LogName { get; set; } = "Application";

        /// <summary>対象マシン名を取得または設定します。</summary>
        public string MachineName { get; set; } = ".";

        /// <summary>イベントIDを取得または設定します。</summary>
        public int EventId { get; set; } = 1232;

        /// <summary>詳細ログ出力レベルを取得または設定します。</summary>
        public int Verbose { get; set; } = 0;

        /// <summary>スタックトレース出力フラグを取得または設定します。</summary>
        public bool IsStackTrace { get; set; } = false;

        /// <summary>ログオン判定フラグを取得または設定します。</summary>
        public bool IsLogonAlwaysOk { get; set; } = false;

        /// <summary>初期化済みフラグを取得または設定します。</summary>
        public bool IsInit { get; set; } = false;

        /// <summary>
        /// イベントログの初期化を行います。イベントソースの存在確認および必要に応じた作成、EventLogインスタンスの生成を実施します。
        /// </summary>
        /// <param name="none">なし</param>
        /// <returns>初期化が成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// ClsEventLog eventLog = new ClsEventLog(logger);
        /// bool success = eventLog.Initialize();
        /// </code>
        /// </example>
        public bool Initialize()
        {
            int result = MdlConst.LVL_I;
            bool isOk = true;
            try
            {
                if (string.IsNullOrEmpty(MachineName)) MachineName = ".";
                if (string.Equals(MachineName, "localhost", StringComparison.OrdinalIgnoreCase)) MachineName = ".";
                result = EventSourceExists();
                if (MdlConst.LVL_E == result)
                {
                    return false;
                }
                if (MdlConst.LVL_W == result)
                {
                    isOk = CreateEventSource();
                }
                if (isOk)
                {
                    if (Verbose > 4) _logger.WriteLine(MdlConst.LVL_I, $"[ClsEventLog.Initialize()] NEW EventLog({LogName},{MachineName},{SourceName})");
                    _eventLog = new System.Diagnostics.EventLog(LogName, MachineName, SourceName);
                }
            }
            catch (Exception ex)
            {
                isOk = false;
                _logger.WriteLine(MdlConst.LVL_E, $"[ClsEventLog.Initialize()] EXCEPTION : {ex.Message}");
                if (IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
            }
            IsInit = true;
            return isOk;
        }

        /// <summary>
        /// [非推奨] イベントログの初期化を行います。代わりに <see cref="Initialize"/> を使用してください。
        /// </summary>
        /// <param name="none">なし</param>
        /// <returns>初期化が成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool success = eventLog.Init();
        /// </code>
        /// </example>
        [Obsolete("代わりに 'Initialize()' を使用します。")]
        public bool Init()
        {
            return Initialize();
        }

        /// <summary>
        /// 指定されたマシン上にイベントソースが存在するかどうかを確認します。
        /// </summary>
        /// <param name="none">なし</param>
        /// <returns>存在する場合は <see cref="MdlConst.LVL_I"/> (情報)、存在しない場合は <see cref="MdlConst.LVL_W"/> (警告)、例外発生時は <see cref="MdlConst.LVL_E"/> (エラー) を返します。</returns>
        /// <example>
        /// <code>
        /// int status = eventLog.EventSourceExists();
        /// </code>
        /// </example>
        public int EventSourceExists()
        {
            int ret = MdlConst.LVL_I;
            try
            {
                if (Verbose > 4) _logger.WriteLine(MdlConst.LVL_I, $"[ClsEventLog.EventSourceExists()] TRY CHECK EventLog.SourceExists({SourceName},{MachineName})");
                if (!System.Diagnostics.EventLog.SourceExists(SourceName, MachineName)) ret = MdlConst.LVL_W;
            }
            catch (Exception ex)
            {
                ret = MdlConst.LVL_E;
                _logger.WriteLine(MdlConst.LVL_E, $"[ClsEventLog.EventSourceExists()] EXCEPTION : {ex.Message}");
                if (IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
                if (!MachineName.Equals("."))
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "※ネットワークパスが見つからない場合は「RemoteRegistry」サービス設定を確認（「無効」⇒「手動(トリガー開始)」）して下さい。");
                    _logger.WriteLine(MdlConst.LVL_NONE, $"  Get-Service -ComputerName {MachineName} -Name RemoteRegistry");
                    _logger.WriteLine(MdlConst.LVL_NONE, "※操作が許可されていない場合は認証を通して下さい。");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
            }
            return ret;
        }

        /// <summary>
        /// [非推奨] イベントソースが存在するかどうかを確認します。代わりに <see cref="EventSourceExists"/> を使用してください。
        /// </summary>
        /// <param name="none">なし</param>
        /// <returns>存在レベルコード (<see cref="MdlConst.LVL_I"/> / <see cref="MdlConst.LVL_W"/> / <see cref="MdlConst.LVL_E"/>)</returns>
        /// <example>
        /// <code>
        /// int result = eventLog.CheckIsExistEventSource();
        /// </code>
        /// </example>
        [Obsolete("代わりに 'EventSourceExists()' を使用します。")]
        public int CheckIsExistEventSource()
        {
            return EventSourceExists();
        }

        /// <summary>
        /// イベントソースを新規作成します。管理者権限での実行が必要です。
        /// </summary>
        /// <param name="none">なし</param>
        /// <returns>作成が成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool isCreated = eventLog.CreateEventSource();
        /// </code>
        /// </example>
        public bool CreateEventSource()
        {
            bool isOk = true;
            try
            {
                if (Verbose > 4) _logger.WriteLine(MdlConst.LVL_I, $"[ClsEventLog.CreateEventSource()] LOGNAME = {LogName} / SOURCE = {SourceName})");
                System.Diagnostics.EventSourceCreationData objEscd = new System.Diagnostics.EventSourceCreationData(SourceName, LogName);
                System.Diagnostics.EventLog.CreateEventSource(objEscd);
            }
            catch (Exception ex)
            {
                isOk = false;
                _logger.WriteLine(MdlConst.LVL_E, $"[ClsEventLog.CreateEventSource()] EXCEPTION : {ex.Message}");
                if (IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
                _logger.WriteLine(MdlConst.LVL_E, $"[ClsEventLog.CreateEventSource()] 管理者権限で初回実行が必要です。 : SOURCE={SourceName} / LOGNAME={LogName}");
            }
            return isOk;
        }

        /// <summary>
        /// 指定されたログレベルに応じてイベントログにメッセージを書き込みます。
        /// </summary>
        /// <param name="errorLevel">ログエラーレベル (<see cref="MdlConst.LVL_F"/>, <see cref="MdlConst.LVL_E"/>, <see cref="MdlConst.LVL_W"/>, <see cref="MdlConst.LVL_I"/> など)</param>
        /// <param name="message">書き込むログメッセージ</param>
        /// <returns>書き込みが成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool result = eventLog.WriteEvent(MdlConst.LVL_E, "処理中にエラーが発生しました。");
        /// </code>
        /// </example>
        public bool WriteEvent(int errorLevel, string message) => errorLevel switch
        {
            MdlConst.LVL_F or MdlConst.LVL_E => WriteError(message),
            MdlConst.LVL_W => WriteWarn(message),
            _ => WriteInfo(message)
        };

        /// <summary>
        /// [非推奨] 指定されたログレベルに応じてイベントログにメッセージを書き込みます。代わりに <see cref="WriteEvent"/> を使用してください。
        /// </summary>
        /// <param name="errorLevel">ログエラーレベル</param>
        /// <param name="message">書き込むログメッセージ</param>
        /// <returns>書き込みが成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool result = eventLog.EvnetWrite(MdlConst.LVL_E, "メッセージ");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteEvent(errorLevel, message)' を使用します。")]
        public bool EvnetWrite(int errorLevel, string message)
        {
            return WriteEvent(errorLevel, message);
        }

        /// <summary>
        /// 情報レベル (Information) のイベントログを書き込みます。
        /// </summary>
        /// <param name="message">書き込むログメッセージ</param>
        /// <returns>書き込みが成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool success = eventLog.WriteInfo("処理が正常終了しました。");
        /// </code>
        /// </example>
        public bool WriteInfo(string message)
        {
            bool isOk = true;
            try
            {
                _eventLog.WriteEntry(message, System.Diagnostics.EventLogEntryType.Information, EventId);
            }
            catch (Exception ex)
            {
                _logger.WriteLine(MdlConst.LVL_E, $"[ClsEventLog.WriteInfo()] EXCEPTION : {ex.Message}");
                if (IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
                isOk = false;
            }
            return isOk;
        }

        /// <summary>
        /// [非推奨] 情報レベルのイベントログを書き込みます。代わりに <see cref="WriteInfo"/> を使用してください。
        /// </summary>
        /// <param name="message">書き込むログメッセージ</param>
        /// <returns>書き込みが成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool success = eventLog.EvnetInfo("情報メッセージ");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteInfo(message)' を使用します。")]
        public bool EvnetInfo(string message)
        {
            return WriteInfo(message);
        }

        /// <summary>
        /// 警告レベル (Warning) のイベントログを書き込みます。
        /// </summary>
        /// <param name="message">書き込むログメッセージ</param>
        /// <returns>書き込みが成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool success = eventLog.WriteWarn("ディスク空容量が低下しています。");
        /// </code>
        /// </example>
        public bool WriteWarn(string message)
        {
            bool isOk = true;
            try
            {
                _eventLog.WriteEntry(message, System.Diagnostics.EventLogEntryType.Warning, EventId);
            }
            catch (Exception ex)
            {
                _logger.WriteLine(MdlConst.LVL_E, $"[ClsEventLog.WriteWarn()] EXCEPTION : {ex.Message}");
                if (IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
                isOk = false;
            }
            return isOk;
        }

        /// <summary>
        /// [非推奨] 警告レベルのイベントログを書き込みます。代わりに <see cref="WriteWarn"/> を使用してください。
        /// </summary>
        /// <param name="message">書き込むログメッセージ</param>
        /// <returns>書き込みが成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool success = eventLog.EvnetWarn("警告メッセージ");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteWarn(message)' を使用します。")]
        public bool EvnetWarn(string message)
        {
            return WriteWarn(message);
        }

        /// <summary>
        /// エラーレベル (Error) のイベントログを書き込みます。
        /// </summary>
        /// <param name="message">書き込むログメッセージ</param>
        /// <returns>書き込みが成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool success = eventLog.WriteError("データベース接続エラーが発生しました。");
        /// </code>
        /// </example>
        public bool WriteError(string message)
        {
            bool isOk = true;
            try
            {
                _eventLog.WriteEntry(message, System.Diagnostics.EventLogEntryType.Error, EventId);
            }
            catch (Exception ex)
            {
                _logger.WriteLine(MdlConst.LVL_E, $"[ClsEventLog.WriteError()] EXCEPTION : {ex.Message}");
                if (IsStackTrace)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                    _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                    _logger.WriteLine(MdlConst.LVL_NONE, "");
                }
                isOk = false;
            }
            return isOk;
        }

        /// <summary>
        /// [非推奨] エラーレベルのイベントログを書き込みます。代わりに <see cref="WriteError"/> を使用してください。
        /// </summary>
        /// <param name="message">書き込むログメッセージ</param>
        /// <returns>書き込みが成功した場合は true。失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool success = eventLog.EvnetError("エラーメッセージ");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteError(message)' を使用します。")]
        public bool EvnetError(string message)
        {
            return WriteError(message);
        }

    }
}




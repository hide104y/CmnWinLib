using System;
using System.Threading;
using Microsoft.Win32;
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
    /// レジストリの読み込み、書き込み、削除機能を提供するクラスです。
    /// </summary>
    /// <example>
    /// <code>
    /// ICmnLogger logger = new CmnLogger();
    /// ClsReg reg = new ClsReg(logger)
    /// {
    ///     RegKeyHome = @"SOFTWARE\InfraTools"
    /// };
    /// string val = reg.GetRegistry(ClsReg.TRGT_USER_REG, "SettingName", RegistryValueKind.String);
    /// </code>
    /// </example>
    public class ClsReg
    {
        // レジストリターゲット定数
        public const int TRGT_USER_REG = 0;             // ユーザー
        public const int TRGT_MACHINE_REG = 1;          // マシーン
        public const int USE_REG = 1;                   // 公開引数
        public const int NOT_USE_REG = 0;               // 隠し引数
        public const string REG_KEY_INFRATOOLS_PATH = @"SOFTWARE\InfraTools";
        public const string REG_KEY_INFRATOOLS_PWOW = @"SOFTWARE\Wow6432Node\InfraTools";

        private readonly ICmnLogger _logger;
        private readonly Lock _lock = new();
        private string _regKeyHome = "";
        private string _regKeyHomeWW = "";
        private string _message = "";
        private int _verbose = 0;
        private bool _isException = false;

        /// <summary>
        /// <see cref="ClsReg"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力を行うロガーインスタンス</param>
        /// <example>
        /// <code>
        /// ICmnLogger logger = new CmnLogger();
        /// ClsReg reg = new ClsReg(logger);
        /// </code>
        /// </example>
        public ClsReg(ICmnLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// レジストリキーのルートパス（標準）を取得または設定します。
        /// </summary>
        public string RegKeyHome { get => _regKeyHome; set => _regKeyHome = value; }

        /// <summary>
        /// レジストリキーのルートパス（64ビット環境用/Wow6432Node）を取得または設定します。
        /// </summary>
        public string RegKeyHomeWW { get => _regKeyHomeWW; set => _regKeyHomeWW = value; }

        /// <summary>
        /// 直近の処理で発生した例外メッセージを取得または設定します。
        /// </summary>
        public string Message { get => _message; set => _message = value; }

        /// <summary>
        /// ログ出力の冗長度レベル（Verbose）を取得または設定します。
        /// </summary>
        public int Verbose { get => _verbose; set => _verbose = value; }

        /// <summary>
        /// 直近の処理で例外が発生したかどうかを取得または設定します。
        /// </summary>
        public bool IsException { get => _isException; set => _isException = value; }

        /// <summary>
        /// サブキーを組み合わせたレジストリキーのフルパスを取得します。
        /// </summary>
        /// <param name="subKey">結合するサブキー名（相対パス）</param>
        /// <returns>環境（32bit/64bit）に応じたレジストリキーのフルパス。未設定時は空文字列。</returns>
        /// <example>
        /// <code>
        /// string path = reg.GetRegKeyPath("SubSection");
        /// </code>
        /// </example>
        public string GetRegKeyPath(string subKey)
        {
            if (_regKeyHome == null)
            {
                if (_verbose > 5) _logger.WriteLine(MdlConst.LVL_E, $"[ClsReg.GetRegKeyPath({subKey})] _regKeyHome = null");
                return "";
            }
            if (_regKeyHomeWW == null)
            {
                if (_verbose > 5) _logger.WriteLine(MdlConst.LVL_E, $"[ClsReg.GetRegKeyPath({subKey})] _regKeyHomeWW = null");
                return "";
            }
            if (_verbose > 5) _logger.WriteLine(MdlConst.LVL_I, $"[ClsReg.GetRegKeyPath({subKey})] _regKeyHome = {_regKeyHome} / _regKeyHomeWW = {_regKeyHomeWW}");

            string regHome = Environment.Is64BitProcess ? _regKeyHomeWW : _regKeyHome;
            if (!string.IsNullOrEmpty(subKey))
            {
                regHome = $"{regHome}\\{subKey}";
            }
            return regHome;
        }

        #region GetRegistry

        /// <summary>
        /// 設定されたルートパス配下のレジストリから指定された値を取得します。
        /// </summary>
        /// <param name="target">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="name">取得対象のレジストリ名</param>
        /// <param name="valueKind">レジストリ値の種類</param>
        /// <returns>取得したレジストリ値の文字列表現。取得失敗時またはキーが存在しない場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string val = reg.GetRegistry(ClsReg.TRGT_USER_REG, "Version", RegistryValueKind.String);
        /// </code>
        /// </example>
        public string GetRegistry(int target, string name, RegistryValueKind valueKind)
        {
            string? regKey = GetRegKeyPath("");
            if (string.IsNullOrEmpty(regKey))
            {
                if (_verbose > 4) _logger.WriteLine(MdlConst.LVL_I, $"[ClsReg.GetRegistry({target}, {name}, {valueKind})] regKey = null or empty");
                return "";
            }
            if (_verbose > 4) _logger.WriteLine(MdlConst.LVL_I, $"[ClsReg.GetRegistry()] CALL GetRegistryCustom({target}, {regKey}, {name}, {valueKind})");
            return GetRegistryCustom(target, regKey, name, valueKind);
        }

        /// <summary>
        /// （非推奨）設定されたルートパス配下のレジストリから指定された値を取得します。<see cref="GetRegistry"/> を使用してください。
        /// </summary>
        /// <param name="regTarget">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="regName">取得対象のレジストリ名</param>
        /// <param name="regType">レジストリ値の種類</param>
        /// <returns>取得したレジストリ値の文字列表現。</returns>
        /// <example>
        /// <code>
        /// string val = reg.GetRegistory(ClsReg.TRGT_USER_REG, "Version", RegistryValueKind.String);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetRegistry(target, name, valueKind)' を使用します。")]
        public string GetRegistory(int regTarget, string regName, RegistryValueKind regType)
        {
            return GetRegistry(regTarget, regName, regType);
        }

        #endregion

        #region GetRegistryCustom

        /// <summary>
        /// 指定されたレジストリキーパスから値を取得します。
        /// </summary>
        /// <param name="target">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="keyPath">レジストリキーのパス</param>
        /// <param name="name">取得対象のレジストリ名</param>
        /// <param name="valueKind">レジストリ値の種類</param>
        /// <returns>取得したレジストリ値の文字列表現。取得失敗時またはキーが存在しない場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string val = reg.GetRegistryCustom(ClsReg.TRGT_USER_REG, @"SOFTWARE\InfraTools", "Path", RegistryValueKind.String);
        /// </code>
        /// </example>
        public string GetRegistryCustom(int target, string keyPath, string name, RegistryValueKind valueKind)
        {
            const string STR_MY_NAME = "[ClsReg.GetRegistryCustom()]";
            string regVal = "";
            _isException = false;

            lock (_lock)
            {
                try
                {
                    RegistryKey baseKey = target switch
                    {
                        TRGT_USER_REG => Registry.CurrentUser,
                        _ => Registry.LocalMachine
                    };

                    using RegistryKey? objRegKey = baseKey.OpenSubKey(keyPath, false);
                    if (objRegKey != null)
                    {
                        switch (valueKind)
                        {
                            case RegistryValueKind.DWord:
                                object? rawVal = objRegKey.GetValue(name, MdlConst.INT_NULL);
                                if (rawVal is int intRegVal && intRegVal != MdlConst.INT_NULL)
                                {
                                    regVal = intRegVal.ToString();
                                }
                                break;
                            default:
                                regVal = objRegKey.GetValue(name) as string ?? "";
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _isException = true;
                    _message = $"{STR_MY_NAME}[{target}, {name}] Exception : {ex.Message}";
                    if (_verbose > 4) _logger.WriteLine(MdlConst.LVL_DEBUG, _message);
                }
            }

            return regVal;
        }

        /// <summary>
        /// （非推奨）指定されたレジストリキーパスから値を取得します。<see cref="GetRegistryCustom"/> を使用してください。
        /// </summary>
        /// <param name="regTarget">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="regKey">レジストリキーのパス</param>
        /// <param name="regName">取得対象のレジストリ名</param>
        /// <param name="regType">レジストリ値の種類</param>
        /// <returns>取得したレジストリ値の文字列表現。</returns>
        /// <example>
        /// <code>
        /// string val = reg.GetRegistoryCstm(ClsReg.TRGT_USER_REG, @"SOFTWARE\InfraTools", "Path", RegistryValueKind.String);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetRegistryCustom(target, keyPath, name, valueKind)' を使用します。")]
        public string GetRegistoryCstm(int regTarget, string regKey, string regName, RegistryValueKind regType)
        {
            return GetRegistryCustom(regTarget, regKey, regName, regType);
        }

        #endregion

        #region SetRegistryIfEmpty

        /// <summary>
        /// レジストリに値が存在しない場合に指定された値を設定します。
        /// </summary>
        /// <param name="target">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="name">設定対象のレジストリ名</param>
        /// <param name="value">設定を試みる値</param>
        /// <param name="defaultValue">レジストリが未設定だった場合に使用するデフォルト値</param>
        /// <param name="valueKind">レジストリ値の種類</param>
        /// <returns>設定に成功した場合は true、失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool result = reg.SetRegistryIfEmpty(ClsReg.TRGT_USER_REG, "Timeout", "", "30", RegistryValueKind.String);
        /// </code>
        /// </example>
        public bool SetRegistryIfEmpty(int target, string name, string value, string defaultValue, RegistryValueKind valueKind)
        {
            bool isOk = true;
            if (string.IsNullOrEmpty(value))
            {
                if (string.IsNullOrEmpty(GetRegistry(target, name, valueKind)))
                {
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        isOk = SetRegistry(target, name, defaultValue, valueKind);
                    }
                }
            }
            else
            {
                isOk = SetRegistry(target, name, value, valueKind);
            }
            return isOk;
        }

        /// <summary>
        /// （非推奨）レジストリに値が存在しない場合に指定された値を設定します。<see cref="SetRegistryIfEmpty"/> を使用してください。
        /// </summary>
        /// <param name="regTarget">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="regName">設定対象のレジストリ名</param>
        /// <param name="regVal">設定を試みる値</param>
        /// <param name="regValDefault">デフォルト値</param>
        /// <param name="regType">レジストリ値の種類</param>
        /// <returns>設定に成功した場合は true、失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool result = reg.SetRegIfNothing(ClsReg.TRGT_USER_REG, "Timeout", "", "30", RegistryValueKind.String);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'SetRegistryIfEmpty(target, name, value, defaultValue, valueKind)' を使用します。")]
        public bool SetRegIfNothing(int regTarget, string regName, string regVal, string regValDefault, RegistryValueKind regType)
        {
            return SetRegistryIfEmpty(regTarget, regName, regVal, regValDefault, regType);
        }

        #endregion

        #region SetRegistry

        /// <summary>
        /// 設定されたルートパス配下のレジストリに指定された値を書き込みます。
        /// </summary>
        /// <param name="target">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="name">設定対象のレジストリ名</param>
        /// <param name="value">書き込む値</param>
        /// <param name="valueKind">レジストリ値の種類</param>
        /// <returns>書き込みに成功した場合は true、失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool result = reg.SetRegistry(ClsReg.TRGT_USER_REG, "Port", "8080", RegistryValueKind.DWord);
        /// </code>
        /// </example>
        public bool SetRegistry(int target, string name, string value, RegistryValueKind valueKind)
        {
            const string STR_MY_NAME = "[ClsReg.SetRegistry()]";
            bool isOk = true;
            string? regKey = GetRegKeyPath("");
            _isException = false;

            if (string.IsNullOrEmpty(regKey)) return false;

            lock (_lock)
            {
                try
                {
                    RegistryKey baseKey = target switch
                    {
                        TRGT_USER_REG => Registry.CurrentUser,
                        _ => Registry.LocalMachine
                    };

                    using RegistryKey? objRegKey = baseKey.CreateSubKey(regKey);
                    if (objRegKey != null)
                    {
                        switch (valueKind)
                        {
                            case RegistryValueKind.DWord:
                                if (int.TryParse(value, out int intVal))
                                {
                                    objRegKey.SetValue(name, intVal, valueKind);
                                }
                                else
                                {
                                    objRegKey.SetValue(name, 0, valueKind);
                                }
                                break;
                            default:
                                objRegKey.SetValue(name, value, valueKind);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    isOk = false;
                    _isException = true;
                    _message = $"{STR_MY_NAME}[{target}, {name}, {value}] Exception : {ex.Message}";
                    _logger.WriteLine(MdlConst.LVL_E, _message);
                }
            }

            return isOk;
        }

        /// <summary>
        /// （非推奨）設定されたルートパス配下のレジストリに指定された値を書き込みます。<see cref="SetRegistry"/> を使用してください。
        /// </summary>
        /// <param name="regTarget">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="regName">設定対象のレジストリ名</param>
        /// <param name="regVal">書き込む値</param>
        /// <param name="regType">レジストリ値の種類</param>
        /// <returns>書き込みに成功した場合は true、失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool result = reg.SetRegistory(ClsReg.TRGT_USER_REG, "Port", "8080", RegistryValueKind.DWord);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'SetRegistry(target, name, value, valueKind)' を使用します。")]
        public bool SetRegistory(int regTarget, string regName, string regVal, RegistryValueKind regType)
        {
            return SetRegistry(regTarget, regName, regVal, regType);
        }

        #endregion

        #region DeleteRegistry

        /// <summary>
        /// 設定されたルートパス配下のレジストリから指定された値を削除します。
        /// </summary>
        /// <param name="target">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="name">削除対象のレジストリ名</param>
        /// <returns>削除に成功した場合は true、失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool result = reg.DeleteRegistry(ClsReg.TRGT_USER_REG, "TempKey");
        /// </code>
        /// </example>
        public bool DeleteRegistry(int target, string name)
        {
            const string STR_MY_NAME = "[ClsReg.DeleteRegistry()]";
            bool isOk = true;
            string? regKey = GetRegKeyPath("");
            _isException = false;

            if (string.IsNullOrEmpty(regKey)) return false;

            lock (_lock)
            {
                try
                {
                    RegistryKey baseKey = target switch
                    {
                        TRGT_USER_REG => Registry.CurrentUser,
                        _ => Registry.LocalMachine
                    };

                    using RegistryKey? objRegKey = baseKey.OpenSubKey(regKey, true);
                    if (objRegKey != null)
                    {
                        objRegKey.DeleteValue(name, false);
                    }
                }
                catch (Exception ex)
                {
                    isOk = false;
                    _isException = true;
                    _message = $"{STR_MY_NAME}[{target}, {name}] Exception : {ex.Message}";
                    _logger.WriteLine(MdlConst.LVL_E, _message);
                }
            }

            return isOk;
        }

        /// <summary>
        /// （非推奨）設定されたルートパス配下のレジストリから指定された値を削除します。<see cref="DeleteRegistry"/> を使用してください。
        /// </summary>
        /// <param name="regTarget">対象レジストリ領域（0: HKEY_CURRENT_USER, 1: HKEY_LOCAL_MACHINE）</param>
        /// <param name="regName">削除対象のレジストリ名</param>
        /// <returns>削除に成功した場合は true、失敗した場合は false。</returns>
        /// <example>
        /// <code>
        /// bool result = reg.DeleteRegistory(ClsReg.TRGT_USER_REG, "TempKey");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'DeleteRegistry(target, name)' を使用します。")]
        public bool DeleteRegistory(int regTarget, string regName)
        {
            return DeleteRegistry(regTarget, regName);
        }

        #endregion
    }
}

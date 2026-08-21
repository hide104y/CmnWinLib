using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.ComponentModel;
using CmnClsLib.Class;
using CmnClsLib.Module;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

//
// 参考
//   http://jehupc.exblog.jp/13776105
//   http://stackoverflow.com/questions/1477328/calling-wnetaddconnection2-from-powershell
//
// PS C:\> $objNetUse= New-Object CmnWinLib.Class.ClsNetUse
// PS C:\> $objNetUse.strDomainName = "remotehost"
// PS C:\> $objNetUse.strUserName = "ここにユーザ名"
// PS C:\> $objNetUse.strPassword = "ここにパスワード"
// PS C:\> $objNetUse.strPathDShare = "\\remotehost\share"
// PS C:\> $objNetUse.Connect()
// True
// PS C:\> Write-Output $objNetUse.strMessage
// OK : 接続(0) => \\ymmr01\D : この操作を正しく終了しました。
// PS C:\> $objNetUse.DisConnect()
// True
// PS C:\> Write-Output $objNetUse.strMessage
// OK : 切断(0) => \\ymmr01\D : この操作を正しく終了しました。
// PS C:\>
//

namespace CmnWinLib.Class
{
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]

    public partial class ClsNetUse
    {
        // [LibraryImport]を使用すると「Unable to find an entry point named 'WNetCancelConnection2' in DLL 'mpr.dll'」が発生するため、[DllImport] に固定
        [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

        [DllImport("mpr.dll", EntryPoint = "WNetAddConnection2", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NETRESOURCE lpNetResource, string? lpPassword, string? lpUsername, int dwFlags);

        public enum ResourceScope
        {
            RESOURCE_CONNECTED = 1,
            RESOURCE_GLOBALNET,
            RESOURCE_REMEMBERED,
            RESOURCE_RECENT,
            RESOURCE_CONTEXT
        };

        public enum ResourceType
        {
            RESOURCETYPE_ANY,
            RESOURCETYPE_DISK,
            RESOURCETYPE_PRINT,
            RESOURCETYPE_RESERVED = 8
        };

        [Flags]
        public enum ResourceUsage
        {
            RESOURCEUSAGE_CONNECTABLE = 0x00000001,
            RESOURCEUSAGE_CONTAINER = 0x00000002,
            RESOURCEUSAGE_NOLOCALDEVICE = 0x00000004,
            RESOURCEUSAGE_SIBLING = 0x00000008,
            RESOURCEUSAGE_ATTACHED = 0x00000010,
            RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE |
                                 RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED),
        };

        public enum ResourceDisplayType
        {
            RESOURCEDISPLAYTYPE_GENERIC,
            RESOURCEDISPLAYTYPE_DOMAIN,
            RESOURCEDISPLAYTYPE_SERVER,
            RESOURCEDISPLAYTYPE_SHARE,
            RESOURCEDISPLAYTYPE_FILE,
            RESOURCEDISPLAYTYPE_GROUP,
            RESOURCEDISPLAYTYPE_NETWORK,
            RESOURCEDISPLAYTYPE_ROOT,
            RESOURCEDISPLAYTYPE_SHAREADMIN,
            RESOURCEDISPLAYTYPE_DIRECTORY,
            RESOURCEDISPLAYTYPE_TREE,
            RESOURCEDISPLAYTYPE_NDSCONTAINER
        };

        [Flags]
        public enum AddConnectionOptions
        {
            CONNECT_UPDATE_PROFILE = 0x00000001,
            CONNECT_UPDATE_RECENT = 0x00000002,
            CONNECT_TEMPORARY = 0x00000004,
            CONNECT_INTERACTIVE = 0x00000008,
            CONNECT_PROMPT = 0x00000010,
            CONNECT_NEED_DRIVE = 0x00000020,
            CONNECT_REFCOUNT = 0x00000040,
            CONNECT_REDIRECT = 0x00000080,
            CONNECT_LOCALDRIVE = 0x00000100,
            CONNECT_CURRENT_MEDIA = 0x00000200,
            CONNECT_DEFERRED = 0x00000400,
            CONNECT_RESERVED = unchecked((int)0xFF000000),
            CONNECT_COMMANDLINE = 0x00000800,
            CONNECT_CMD_SAVECRED = 0x00001000,
            CONNECT_CRED_RESET = 0x00002000
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NETRESOURCE
        {
            public ResourceScope dwScope;                   //列挙の範囲
            public ResourceType dwType;                     //リソースタイプ
            public ResourceDisplayType dwDisplayType;       //表示オブジェクト
            public ResourceUsage dwUsage;                   //リソースの使用方法
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpLocalName;                      //ローカルデバイス名。使わないならNULL。
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpRemoteName;                     //リモートネットワーク名。使わないならNULL
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpComment;                        //ネットワーク内の提供者に提供された文字列
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpProvider;                       //リソースを所有しているプロバイダ名
        }

        private List<int> _allowedErrorCodes = new();
        private string _networkPath = "";
        private string _driveName = "";
        private string _domain = "";
        private string _username = "";
        private string _password = "";
        private bool _ignoreErrors = false;
        private string _message = "";

        /// <summary>
        /// 接続・切断時にエラーが発生しても成功（"--"）として扱うエラーコードのリストを取得または設定します。
        /// </summary>
        /// <value>許可するエラーコードのリスト。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.AllowedErrorCodes.Add(1219); // 複数接続エラーを許可
        /// </code>
        /// </example>
        public List<int> AllowedErrorCodes { get { return _allowedErrorCodes; } set { _allowedErrorCodes = value; } }

        /// <summary>
        /// 接続対象のネットワークパス（UNCパスなど）を取得または設定します。
        /// </summary>
        /// <value>ネットワークパス文字列。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.NetworkPath = @"\\server\share";
        /// </code>
        /// </example>
        public string NetworkPath { get { return _networkPath; } set { _networkPath = value; } }

        /// <summary>
        /// 割り当てるローカルドライブ名（例: "Z" または "Z:"）を取得または設定します。ドライブ割り当てを行わない場合は空文字を指定します。
        /// </summary>
        /// <value>ドライブ名。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.DriveName = "Z";
        /// </code>
        /// </example>
        public string DriveName { get { return _driveName; } set { _driveName = value; } }

        /// <summary>
        /// 接続に使用するユーザー名を取得または設定します。
        /// </summary>
        /// <value>ユーザー名。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.Username = "Administrator";
        /// </code>
        /// </example>
        public string Username { get { return _username; } set { _username = value; } }

        /// <summary>
        /// 接続に使用するパスワードを取得または設定します。
        /// </summary>
        /// <value>パスワード。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.Password = "P@ssw0rd";
        /// </code>
        /// </example>
        public string Password { get { return _password; } set { _password = value; } }

        /// <summary>
        /// 接続に使用するドメイン名またはホスト名を取得または設定します。
        /// </summary>
        /// <value>ドメイン名。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.Domain = "MYDOMAIN";
        /// </code>
        /// </example>
        public string Domain { get { return _domain; } set { _domain = value; } }

        /// <summary>
        /// 処理結果に関するメッセージを取得または設定します。
        /// </summary>
        /// <value>実行結果メッセージ。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.Connect();
        /// Console.WriteLine(netUse.Message);
        /// </code>
        /// </example>
        public string Message { get { return _message; } set { _message = value; } }

        /// <summary>
        /// 接続・切断時にエラーが発生した場合でも常に成功扱い（"--"）とするかどうかを取得または設定します。
        /// </summary>
        /// <value>エラーを無視して成功とする場合は <c>true</c>。それ以外は <c>false</c>。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.IgnoreErrors = true;
        /// </code>
        /// </example>
        public bool IgnoreErrors { get { return _ignoreErrors; } set { _ignoreErrors = value; } }

        /// <summary>
        /// 接続・切断時にエラーが発生しても成功（"--"）として扱うエラーコードのリストを取得または設定します。（旧API）
        /// </summary>
        /// <value>許可するエラーコードのリスト。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.NetUseOkErrNoList.Add(1219);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'AllowedErrorCodes' を使用します。")]
        public List<int> NetUseOkErrNoList { get { return AllowedErrorCodes; } set { AllowedErrorCodes = value; } }

        /// <summary>
        /// 接続・切断時にエラーが発生した場合でも常に成功扱い（"--"）とするかどうかを取得または設定します。（旧API）
        /// </summary>
        /// <value>エラーを無視して成功とする場合は <c>true</c>。それ以外は <c>false</c>。</value>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// netUse.IsAlwaysLogonOk = true;
        /// </code>
        /// </example>
        [Obsolete("代わりに 'IgnoreErrors' を使用します。")]
        public bool IsAlwaysLogonOk { get { return IgnoreErrors; } set { IgnoreErrors = value; } }

        /// <summary>
        /// <see cref="ClsNetUse"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse();
        /// </code>
        /// </example>
        public ClsNetUse()
        {
        }

        /// <summary>
        /// 設定されたプロパティ値に基づいてネットワーク接続を確立します。
        /// </summary>
        /// <returns>接続処理が成功した場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse
        /// {
        ///     NetworkPath = @"\\remotehost\share",
        ///     Username = "user",
        ///     Password = "password"
        /// };
        /// bool success = netUse.Connect();
        /// Console.WriteLine(netUse.Message);
        /// </code>
        /// </example>
        public bool Connect()
        {
            bool isSuccess = true;
            _message = "NG";
            if (string.IsNullOrEmpty(_networkPath)) _networkPath = "パスが指定されていません。";
            NormalizeParameters();
            NETRESOURCE netResource = new();
            netResource.dwScope = 0;
            netResource.dwType = ResourceType.RESOURCETYPE_DISK;
            netResource.dwDisplayType = 0;
            netResource.dwUsage = 0;
            netResource.lpLocalName = (string.IsNullOrEmpty(_driveName) ? "" : _driveName + ":");
            netResource.lpRemoteName = _networkPath;
            netResource.lpProvider = "";
            try
            {
                string logonUser = _username;
                if (!string.IsNullOrEmpty(_domain)) logonUser = $@"{_domain}\{_username}";
                int returnCode = WNetAddConnection2(ref netResource, _password, logonUser, 0);
                switch (returnCode)
                {
                    case 0:
                        _message = "OK";
                        break;
                    default:
                        if (_ignoreErrors)
                        {
                            _message = "--";
                        }
                        else
                        {
                            if (_allowedErrorCodes.Count > 0)
                            {
                                if (_allowedErrorCodes.Contains(returnCode))
                                {
                                    _message = "--";
                                }
                                else
                                {
                                    isSuccess = false;
                                    _message = "NG";
                                }
                            }
                            else
                            {
                                isSuccess = false;
                                _message = "NG";
                            }
                        }
                        break;
                }
                string errorDescription = new Win32Exception(returnCode).Message;
                _message += $" : 接続({returnCode}) => {_networkPath}";
                if (!string.IsNullOrEmpty(errorDescription)) _message += $" : {errorDescription}";
            }
            catch (Exception e)
            {
                if (_ignoreErrors)
                {
                    _message = $"-- : 接続(EXCEPTION) => {_networkPath} : {e.Message}";
                }
                else
                {
                    isSuccess = false;
                    _message = $"NG : 接続(EXCEPTION) => {_networkPath} : {e.Message}";
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 確立されたネットワーク接続を切断します。
        /// </summary>
        /// <returns>切断処理が成功した場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse
        /// {
        ///     NetworkPath = @"\\remotehost\share"
        /// };
        /// bool success = netUse.Disconnect();
        /// Console.WriteLine(netUse.Message);
        /// </code>
        /// </example>
        public bool Disconnect()
        {
            bool isSuccess = true;
            _message = "NG";
            if (string.IsNullOrEmpty(_networkPath)) _networkPath = "パスが指定されていません。";
            NormalizeParameters();
            try
            {
                int returnCode = WNetCancelConnection2(_networkPath, 0, true);
                switch (returnCode)
                {
                    case 0:
                    case 2250:
                        _message = "OK";
                        break;
                    default:
                        if (_ignoreErrors)
                        {
                            _message = "--";
                        }
                        else
                        {
                            if (_allowedErrorCodes.Count > 0)
                            {
                                if (_allowedErrorCodes.Contains(returnCode))
                                {
                                    _message = "--";
                                }
                                else
                                {
                                    isSuccess = false;
                                    _message = "NG";
                                }
                            }
                            else
                            {
                                isSuccess = false;
                                _message = "NG";
                            }
                        }
                        break;
                }
                string errorDescription = new Win32Exception(returnCode).Message;
                _message += $" : 切断({returnCode}) => {_networkPath}";
                if (!string.IsNullOrEmpty(errorDescription)) _message += $" : {errorDescription}";
            }
            catch (Exception e)
            {
                if (_ignoreErrors)
                {
                    _message = $"-- : 切断(EXCEPTION) => {_networkPath} : {e.Message}";
                }
                else
                {
                    isSuccess = false;
                    _message = $"NG : 切断(EXCEPTION) => {_networkPath} : {e.Message}";
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 確立されたネットワーク接続を切断します。（旧API）
        /// </summary>
        /// <returns>切断処理が成功した場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse { NetworkPath = @"\\remotehost\share" };
        /// netUse.DisConnect();
        /// </code>
        /// </example>
        [Obsolete("代わりに 'Disconnect()' を使用します。")]
        public bool DisConnect()
        {
            return Disconnect();
        }

        /// <summary>
        /// 設定されたパスやドライブ名のパラメータを正規化します（末尾のパス区切り文字やコロンを除去）。
        /// </summary>
        /// <returns>常に <c>true</c> を返します。</returns>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse
        /// {
        ///     NetworkPath = @"\\remotehost\share\",
        ///     DriveName = "Z:"
        /// };
        /// netUse.NormalizeParameters();
        /// // netUse.NetworkPath は @"\\remotehost\share", netUse.DriveName は "Z" に正規化されます
        /// </code>
        /// </example>
        public bool NormalizeParameters()
        {
            if (!string.IsNullOrEmpty(_networkPath)) _networkPath = MdlFile.RemoveTrailingPathSeparator(_networkPath);
            if (!string.IsNullOrEmpty(_driveName)) _driveName = _driveName.Replace(":", "");
            return true;
        }

        /// <summary>
        /// 設定されたパスやドライブ名のパラメータを修正・正規化します。（旧API）
        /// </summary>
        /// <returns>常に <c>true</c> を返します。</returns>
        /// <example>
        /// <code>
        /// var netUse = new ClsNetUse { NetworkPath = @"\\remotehost\share\", DriveName = "Z:" };
        /// netUse.FixParams();
        /// </code>
        /// </example>
        [Obsolete("代わりに 'NormalizeParameters()' を使用します。")]
        public bool FixParams()
        {
            return NormalizeParameters();
        }

    }
}

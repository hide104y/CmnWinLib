using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace CmnWinLib.Class
{
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Windows ユーザーロールおよび UAC (ユーザーアカウント制御) のトークン情報を取得・判定する機能を提供するクラスです。
    /// </summary>
    public partial class ClsUserRole
    {
        /// <summary>
        /// アクセストークンに関する情報を取得する Windows Win32 API です。
        /// </summary>
        /// <param name="TokenHandle">アクセストークンのハンドル。</param>
        /// <param name="TokenInformationClass">取得するトークン情報の種類。</param>
        /// <param name="TokenInformation">取得した情報が格納されるバッファへのポインタ。</param>
        /// <param name="TokenInformationLength">バッファのサイズ（バイト単位）。</param>
        /// <param name="ReturnLength">実際に返された情報のサイズを受け取る変数。</param>
        /// <returns>成功した場合は true、失敗した場合は false。</returns>
        [LibraryImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetTokenInformation(
            IntPtr TokenHandle,
            TokenInformationClass TokenInformationClass,
            IntPtr TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        /// <summary>
        /// アクセストークン情報の種類を表す列挙型です。
        /// </summary>
        public enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /// <summary>
        /// アクセストークン情報の種類を表す列挙型です（非推奨。代わりに TokenInformationClass を使用してください）。
        /// </summary>
        [Obsolete("代わりに 'TokenInformationClass' を使用します。")]
        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /// <summary>
        /// UAC 昇格トークンの種類を表す列挙型です。
        /// </summary>
        public enum TokenElevationType
        {
            /// <summary>デフォルト（UAC無効時、または通常のユーザー・管理者）</summary>
            TokenElevationTypeDefault = 1,
            /// <summary>昇格済み（管理者として実行中）</summary>
            TokenElevationTypeFull,
            /// <summary>限定的（管理者権限を持つが非昇格で実行中）</summary>
            TokenElevationTypeLimited
        }

        /// <summary>
        /// UAC 昇格トークンの種類を表す列挙型です（非推奨。代わりに TokenElevationType を使用してください）。
        /// </summary>
        [Obsolete("代わりに 'TokenElevationType' を使用します。")]
        public enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        private bool _isOk = false; 
        private System.Security.Principal.WindowsIdentity? _windowsIdentity = null;
        private System.Security.Principal.WindowsPrincipal? _windowsPrincipal = null;

        /// <summary>
        /// <see cref="ClsUserRole"/> クラスの新しいインスタンスを初期化します。
        /// 現在の Windows ユーザー識別子およびプリンシパルを取得します。
        /// </summary>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// </code>
        /// </example>
        public ClsUserRole()
        {
            // 現在のユーザーを表すWindowsIdentityオブジェクトを取得する
            _windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            // WindowsPrincipalオブジェクトを作成する
            _windowsPrincipal = new System.Security.Principal.WindowsPrincipal(_windowsIdentity);
        }

        /// <summary>
        /// 処理が正常に行われたかどうかのステータスを取得または設定します。
        /// </summary>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// bool status = userRole.IsOk;
        /// </code>
        /// </example>
        public bool IsOk { get => _isOk; set => _isOk = value; }

        /// <summary>
        /// 指定された Built-in セキュリティグループ（ロール）に現在のユーザーが属しているか確認します。
        /// </summary>
        /// <param name="role">確認対象の Built-in Windows ロール（<see cref="System.Security.Principal.WindowsBuiltInRole"/>）。</param>
        /// <returns>指定されたロールに属している場合は true。属していない、または判定不可の場合は false。</returns>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// bool isUser = userRole.IsInRole(System.Security.Principal.WindowsBuiltInRole.User);
        /// </code>
        /// </example>
        public bool IsInRole(System.Security.Principal.WindowsBuiltInRole role)
        {
            // 指定セキュリティグループに属しているか調べる
            return _windowsPrincipal?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// 現在のユーザーが管理者ロール（Administrator）に属しているか確認します。
        /// </summary>
        /// <returns>管理者ロールに属している場合は true。属していない場合は false。</returns>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// bool isAdmin = userRole.IsAdministrator();
        /// </code>
        /// </example>
        public bool IsAdministrator()
        {
            return _windowsPrincipal?.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ?? false;
        }

        /// <summary>
        /// 現在のユーザー名（ドメイン名\ユーザー名）を取得します。
        /// </summary>
        /// <returns>ユーザー名を表す文字列。取得できない場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// string name = userRole.GetUserName();
        /// </code>
        /// </example>
        public string GetUserName()
        {
            return _windowsPrincipal?.Identity.Name ?? "";
        }

        /// <summary>
        /// ユーザー名を取得します（非推奨。代わりに <see cref="GetUserName"/> を使用してください）。
        /// </summary>
        /// <returns>ユーザー名を表す文字列。</returns>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// string name = userRole.Username();
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetUserName()' を使用します。")]
        public string Username()
        {
            return GetUserName();
        }

        /// <summary>
        /// 認証タイプ（Kerberos、NTLM 等）を取得します。
        /// </summary>
        /// <returns>認証タイプを表す文字列。取得できない場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// string authType = userRole.GetAuthenticationType();
        /// </code>
        /// </example>
        public string GetAuthenticationType()
        {
            return _windowsPrincipal?.Identity.AuthenticationType ?? "";
        }

        /// <summary>
        /// 認証タイプを取得します（非推奨。代わりに <see cref="GetAuthenticationType"/> を使用してください）。
        /// </summary>
        /// <returns>認証タイプを表す文字列。</returns>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// string authType = userRole.AuthenticationType();
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetAuthenticationType()' を使用します。")]
        public string AuthenticationType()
        {
            return GetAuthenticationType();
        }

        /// <summary>
        /// ユーザーが認証されているかどうかを確認します。
        /// </summary>
        /// <returns>認証済みの場合は true。認証されていない場合は false。</returns>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// bool isAuth = userRole.IsAuthenticated();
        /// </code>
        /// </example>
        public bool IsAuthenticated()
        {
            return _windowsPrincipal?.Identity.IsAuthenticated ?? false;
        }

        /// <summary>
        /// トークンの UAC エレベーションタイプ（昇格状態）を取得します。
        /// </summary>
        /// <returns>昇格トークンの種類を示す <see cref="TokenElevationType"/>。取得失敗時または非Windows OS時は <see cref="TokenElevationType.TokenElevationTypeDefault"/> を返す。</returns>
        /// <example>
        /// <code>
        /// var userRole = new ClsUserRole();
        /// var elevationType = userRole.GetTokenElevationType();
        /// if (userRole.IsOk)
        /// {
        ///     Console.WriteLine($"Elevation Type: {elevationType}");
        /// }
        /// </code>
        /// </example>
        public TokenElevationType GetTokenElevationType()
        {
            _isOk = true;
            TokenElevationType returnValue = TokenElevationType.TokenElevationTypeDefault;
            if (!OperatingSystem.IsWindows())
            {
                _isOk = false;
                return returnValue;
            }
            TokenElevationType elevationType = TokenElevationType.TokenElevationTypeDefault;
            uint returnLength = 0;
            uint elevationTypeSize = (uint)Marshal.SizeOf((int)elevationType);
            IntPtr elevationTypePtr = Marshal.AllocHGlobal((int)elevationTypeSize);
            try
            {
                if (GetTokenInformation(System.Security.Principal.WindowsIdentity.GetCurrent().Token, TokenInformationClass.TokenElevationType, elevationTypePtr, elevationTypeSize, out returnLength))
                {
                    returnValue = (TokenElevationType)Marshal.ReadInt32(elevationTypePtr);
                }
            }
            catch
            {
                _isOk = false;
            }
            finally
            {
                Marshal.FreeHGlobal(elevationTypePtr);
            }
            return returnValue;
        }

    }
}

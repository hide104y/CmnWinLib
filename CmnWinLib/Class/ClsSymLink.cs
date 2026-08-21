using System;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using CmnClsLib.Class;
using CmnClsLib.Interface;
using CmnClsLib.Module;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace CmnWinLib.Class
{
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Windows 環境におけるシンボリックリンクの作成、コピー、削除および実パス取得機能を提供するクラスです。
    /// </summary>
    public partial class ClsSymLink
    {
        /// <summary>
        /// シンボリックリンクを作成する Win32 API
        /// </summary>
        [LibraryImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CreateSymbolicLink(
            string lpSymlinkFileName,
            string lpTargetFileName,
            int dwFlags);

        /// <summary>
        /// ハンドルから最終的なファイルパスを取得する Win32 API
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetFinalPathNameByHandle(
            [In] IntPtr hFile,
            [Out] StringBuilder lpszFilePath,
            [In] int cchFilePath,
            [In] int dwFlags);

        /// <summary>
        /// ファイルまたはディレクトリの SafeFileHandle を取得する Win32 API
        /// </summary>
        [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial SafeFileHandle CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            int dwShareMode,
            IntPtr SecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        private const int CREATION_DISPOSITION_OPEN_EXISTING = 3;
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

        private readonly ICmnLogger _logger;
        private string _message = string.Empty;
        private string _realPath = string.Empty;
        private int _verbose = 0;
        private bool _isSilent = false;

        /// <summary>
        /// ロガーを指定して <see cref="ClsSymLink"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力に使用する <see cref="ICmnLogger"/> インスタンス。</param>
        /// <example>
        /// <code>
        /// ICmnLogger logger = new ClsWinLogger();
        /// ClsSymLink symLink = new ClsSymLink(logger);
        /// </code>
        /// </example>
        public ClsSymLink(ICmnLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 最後に実行された処理のエラーメッセージまたは処理ログメッセージを取得または設定します。
        /// </summary>
        /// <returns>処理結果メッセージ文字列。</returns>
        /// <example>
        /// <code>
        /// string lastMessage = symLink.Message;
        /// </code>
        /// </example>
        public string Message { get => _message; set => _message = value; }

        /// <summary>
        /// 最後に取得されたシンボリックリンクの実際の参照先パスを取得または設定します。
        /// </summary>
        /// <returns>実パス文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.RealPath;
        /// </code>
        /// </example>
        public string RealPath { get => _realPath; set => _realPath = value; }

        /// <summary>
        /// ログ出力の詳細レベル（0: 非出力, 1: 最小, 2: 詳細, 3: デバッグ）を取得または設定します。
        /// </summary>
        /// <returns>ログ詳細レベルの値。</returns>
        /// <example>
        /// <code>
        /// symLink.Verbose = 2;
        /// </code>
        /// </example>
        public int Verbose { get => _verbose; set => _verbose = value; }

        /// <summary>
        /// ログ出力を完全に抑制するかどうかを示す値を取得または設定します。
        /// </summary>
        /// <returns>サイレントモードの場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// symLink.IsSilent = true;
        /// </code>
        /// </example>
        public bool IsSilent { get => _isSilent; set => _isSilent = value; }

        /// <summary>
        /// 指定されたレベルでログメッセージを出力します（サイレントモード時は出力されません）。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="message">出力するメッセージ。</param>
        /// <returns>戻り値はありません。</returns>
        /// <example>
        /// <code>
        /// symLink.WriteLine(MdlConst.LVL_NONE, "処理を開始します。");
        /// </code>
        /// </example>
        public void WriteLine(int level, string message)
        {
            if (!_isSilent) _logger.WriteLine(level, message);
        }

        /// <summary>
        /// [非推奨] ログメッセージを出力します。代わりに <see cref="WriteLine(int, string)"/> を使用してください。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="message">出力するメッセージ。</param>
        /// <returns>戻り値はありません。</returns>
        /// <example>
        /// <code>
        /// symLink.Writeln(MdlConst.LVL_NONE, "ログメッセージ");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'WriteLine(int, string)' を使用します。")]
        public void Writeln(int level, string message)
        {
            WriteLine(level, message);
        }

        /// <summary>
        /// シンボリックリンクをコピーします（相対パス指定フラグ対応）。
        /// </summary>
        /// <param name="sourcePath">コピー元となるファイルまたはディレクトリのパス。</param>
        /// <param name="destinationPath">コピー先となるファイルまたはディレクトリのパス。</param>
        /// <param name="overwrite">コピー先が存在する場合に上書きする場合は <c>true</c>。上書きしない場合は <c>false</c>。</param>
        /// <param name="isRelative">実パスの取得・設定を相対パスとして処理する場合は <c>true</c>。</param>
        /// <returns>コピーおよびリンク生成が成功した場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = symLink.Copy(@"C:\data\link.txt", @"C:\backup\link.txt", overwrite: true, isRelative: false);
        /// </code>
        /// </example>
        public bool Copy(string sourcePath, string destinationPath, bool overwrite, bool isRelative)
        {
            bool isSuccess = true;
            string realPath = string.Empty;
            _message = string.Empty;
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath)) return false;

            int pathType = MdlFile.GetPathType(sourcePath);
            switch (pathType)
            {
                case MdlFile.PATH_IS_DIRECTORY:
                case MdlFile.PATH_IS_FILE:
                    break;
                default:
                    _message = $" => ERROR : ClsSymLink.Copy() : NO SUCH A FILE OR DIRECTORY : {sourcePath}";
                    if (_verbose > 1) WriteLine(MdlConst.LVL_NONE, _message);
                    isSuccess = false;
                    break;
            }
            if (!isSuccess) return isSuccess;

            switch (MdlFile.GetPathType(destinationPath))
            {
                case MdlFile.PATH_IS_DIRECTORY:
                case MdlFile.PATH_IS_FILE:
                    if (MdlFile.IsSymlink(destinationPath))
                    {
                        if (!overwrite)
                        {
                            _message = $" => SKIP : ClsSymLink.Copy() : SYMLINK ALREADY EXISTS : {destinationPath}";
                            if (_verbose > 1) WriteLine(MdlConst.LVL_NONE, _message);
                            return true;
                        }
                    }
                    else
                    {
                        isSuccess = Delete(destinationPath);
                    }
                    break;
            }
            if (!isSuccess) return isSuccess;

            if (MdlFile.IsSymlink(sourcePath)) realPath = GetRealPath(sourcePath, isRelative);
            if (realPath == null) return false;

            isSuccess = CreateSymbolicLink(destinationPath, realPath, pathType, overwrite);
            return isSuccess;
        }

        /// <summary>
        /// シンボリックリンクをコピーします（絶対パス固定）。
        /// </summary>
        /// <param name="sourcePath">コピー元となるファイルまたはディレクトリのパス。</param>
        /// <param name="destinationPath">コピー先となるファイルまたはディレクトリのパス。</param>
        /// <param name="overwrite">コピー先が存在する場合に上書きする場合は <c>true</c>。上書きしない場合は <c>false</c>。</param>
        /// <returns>コピーおよびリンク生成が成功した場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = symLink.Copy(@"C:\data\link.txt", @"C:\backup\link.txt", overwrite: true);
        /// </code>
        /// </example>
        public bool Copy(string sourcePath, string destinationPath, bool overwrite)
        {
            return Copy(sourcePath, destinationPath, overwrite, false);
        }

        /// <summary>
        /// 指定したパスにシンボリックリンクを作成します。
        /// </summary>
        /// <param name="symbolicLinkPath">作成するシンボリックリンクのパス。</param>
        /// <param name="targetPath">リンク先のターゲットファイルまたはディレクトリのパス。</param>
        /// <param name="pathType">パス種別（<see cref="MdlFile.PATH_IS_DIRECTORY"/> または <see cref="MdlFile.PATH_IS_FILE"/>）。</param>
        /// <param name="overwrite">既存の同名リンクまたはファイルを削除して作成し直す場合は <c>true</c>。</param>
        /// <returns>作成が成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = symLink.CreateSymbolicLink(@"C:\link_dir", @"D:\real_dir", MdlFile.PATH_IS_DIRECTORY, overwrite: true);
        /// </code>
        /// </example>
        public bool CreateSymbolicLink(string symbolicLinkPath, string targetPath, int pathType, bool overwrite)
        {
            int win32Error = 0;
            bool isSuccess = true;
            string option = string.Empty;
            _message = string.Empty;

            if (string.IsNullOrWhiteSpace(symbolicLinkPath) || string.IsNullOrWhiteSpace(targetPath)) return false;

            if (overwrite) Delete(symbolicLinkPath);
            try
            {
                switch (pathType)
                {
                    case MdlFile.PATH_IS_DIRECTORY:
                        option = "/D ";
                        if (_verbose > 2) WriteLine(MdlConst.LVL_NONE, $" => ClsSymLink.CreateSymbolicLink() : mklink {option}SYMLINK({symbolicLinkPath}) -> REAL({targetPath})");
                        isSuccess = CreateSymbolicLink(symbolicLinkPath, targetPath, SYMBOLIC_LINK_FLAG_DIRECTORY);
                        if (!isSuccess) win32Error = Marshal.GetLastWin32Error();
                        break;
                    case MdlFile.PATH_IS_FILE:
                        if (_verbose > 2) WriteLine(MdlConst.LVL_NONE, $" => ClsSymLink.CreateSymbolicLink() : mklink {option}SYMLINK({symbolicLinkPath}) -> REAL({targetPath})");
                        isSuccess = CreateSymbolicLink(symbolicLinkPath, targetPath, 0);
                        if (!isSuccess) win32Error = Marshal.GetLastWin32Error();
                        break;
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                _message = $"[ClsSymLink.CreateSymbolicLink()] {ex.Message}";
                if (_verbose > 1) WriteLine(MdlConst.LVL_NONE, _message);
            }

            if (isSuccess) isSuccess = MdlFile.IsSymlink(symbolicLinkPath);

            if (!isSuccess)
            {
                if (win32Error != 0)
                {
                    _message = $" => ERROR : ClsSymLink.CreateSymbolicLink() : mklink {option}{symbolicLinkPath} {targetPath} : {new Win32Exception(win32Error).Message}";
                }
                else if (string.IsNullOrEmpty(_message))
                {
                    _message = $" => ERROR : ClsSymLink.CreateSymbolicLink() : Failed to create or verify symbolic link for {symbolicLinkPath}";
                }
                if (_verbose > 1) WriteLine(MdlConst.LVL_NONE, _message);
            }
            return isSuccess;
        }

        /// <summary>
        /// [非推奨] シンボリックリンクを作成します。代わりに <see cref="CreateSymbolicLink(string, string, int, bool)"/> を使用してください。
        /// </summary>
        /// <param name="symlinkPath">作成するシンボリックリンクのパス。</param>
        /// <param name="targetPath">ターゲットパス。</param>
        /// <param name="type">パス種別。</param>
        /// <param name="overwrite">上書きフラグ。</param>
        /// <returns>成功時は <c>true</c>。失敗時は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool result = symLink.Mklink(@"C:\link.txt", @"C:\target.txt", MdlFile.PATH_IS_FILE, true);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'CreateSymbolicLink(string, string, int, bool)' を使用します。")]
        public bool Mklink(string symlinkPath, string targetPath, int type, bool overwrite)
        {
            return CreateSymbolicLink(symlinkPath, targetPath, type, overwrite);
        }

        /// <summary>
        /// 指定されたパスのシンボリックリンクまたはファイル・ディレクトリを再帰的に削除します。
        /// </summary>
        /// <param name="symbolicLinkPath">削除対象のパス。</param>
        /// <returns>削除が成功した場合は <c>true</c>。失敗した場合は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// bool deleted = symLink.Delete(@"C:\link.txt");
        /// </code>
        /// </example>
        public bool Delete(string symbolicLinkPath)
        {
            return MdlFile.DeleteRecursively(symbolicLinkPath);
        }

        /// <summary>
        /// ファイル・ディレクトリが存在し、かつシンボリックリンクである場合にその参照先の実パスを取得します（相対パス変換指定可能）。
        /// </summary>
        /// <param name="symbolicLinkPath">対象のシンボリックリンクパス。</param>
        /// <param name="isRelative">取得パスを相対パスに変換する場合は <c>true</c>。</param>
        /// <returns>実パス文字列。ファイルが存在しない場合またはシンボリックリンクでない場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPathIfExists(@"C:\link.txt", isRelative: false);
        /// </code>
        /// </example>
        public string GetRealPathIfExists(string symbolicLinkPath, bool isRelative)
        {
            bool isSuccess = true;
            _message = string.Empty;
            if (string.IsNullOrWhiteSpace(symbolicLinkPath)) return string.Empty;

            switch (MdlFile.GetPathType(symbolicLinkPath))
            {
                case MdlFile.PATH_IS_DIRECTORY:
                case MdlFile.PATH_IS_FILE:
                    break;
                default:
                    isSuccess = false;
                    _message = $" => ERROR : ClsSymLink.GetRealPathIfExists() : NO SUCH A FILE OR DIRECTORY : {symbolicLinkPath}";
                    if (_verbose > 1) WriteLine(MdlConst.LVL_NONE, _message);
                    break;
            }
            if (!isSuccess) return string.Empty;
            if (!MdlFile.IsSymlink(symbolicLinkPath)) return string.Empty;
            return GetRealPath(symbolicLinkPath, isRelative);
        }

        /// <summary>
        /// [非推奨] シンボリックリンクの実際のパスを取得します。代わりに <see cref="GetRealPathIfExists(string, bool)"/> を使用してください。
        /// </summary>
        /// <param name="symlinkPath">シンボリックリンクパス。</param>
        /// <param name="isRelative">相対パスフラグ。</param>
        /// <returns>実パス文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPathIfExist(@"C:\link.txt", false);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetRealPathIfExists(string, bool)' を使用します。")]
        public string GetRealPathIfExist(string symlinkPath, bool isRelative)
        {
            return GetRealPathIfExists(symlinkPath, isRelative);
        }

        /// <summary>
        /// ファイル・ディレクトリが存在し、かつシンボリックリンクである場合にその参照先の実パスを取得します（絶対パス固定）。
        /// </summary>
        /// <param name="symbolicLinkPath">対象のシンボリックリンクパス。</param>
        /// <returns>実パス文字列。ファイルが存在しない場合またはシンボリックリンクでない場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPathIfExists(@"C:\link.txt");
        /// </code>
        /// </example>
        public string GetRealPathIfExists(string symbolicLinkPath)
        {
            return GetRealPathIfExists(symbolicLinkPath, false);
        }

        /// <summary>
        /// [非推奨] シンボリックリンクの実際のパスを取得します。代わりに <see cref="GetRealPathIfExists(string)"/> を使用してください。
        /// </summary>
        /// <param name="symlinkPath">シンボリックリンクパス。</param>
        /// <returns>実パス文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPathIfExist(@"C:\link.txt");
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetRealPathIfExists(string)' を使用します。")]
        public string GetRealPathIfExist(string symlinkPath)
        {
            return GetRealPathIfExists(symlinkPath);
        }

        /// <summary>
        /// 指定されたシンボリックリンクが参照しているターゲットの実パスを取得します（相対パス指定可能）。
        /// </summary>
        /// <param name="symbolicLinkPath">対象のシンボリックリンクパス。</param>
        /// <param name="isRelative">結果を相対パスに変換する場合は <c>true</c>。</param>
        /// <returns>実パス文字列。取得に失敗した場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPath(@"C:\link.txt", isRelative: true);
        /// </code>
        /// </example>
        public string GetRealPath(string symbolicLinkPath, bool isRelative)
        {
            int win32Error = 0;
            string realPath = string.Empty;
            _message = string.Empty;
            _realPath = string.Empty;
            if (string.IsNullOrWhiteSpace(symbolicLinkPath)) return string.Empty;

            try
            {
                System.IO.DirectoryInfo dirInfo = new(symbolicLinkPath);
                using SafeFileHandle dirHandle = CreateFile(dirInfo.FullName, 0, 2, IntPtr.Zero, CREATION_DISPOSITION_OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
                if (dirHandle.IsInvalid) win32Error = Marshal.GetLastWin32Error();
                if (win32Error != 0)
                {
                    _message = $" => ERROR : ClsSymLink.GetRealPath() : FAILED TO LOAD SYMLINK : {symbolicLinkPath} : {new Win32Exception(win32Error).Message}";
                    if (_verbose > 1) WriteLine(MdlConst.LVL_NONE, _message);
                    return string.Empty;
                }

                StringBuilder sb = new(512);
                int isOk = GetFinalPathNameByHandle(dirHandle.DangerousGetHandle(), sb, sb.Capacity, 0);
                if (isOk < 0) win32Error = Marshal.GetLastWin32Error();
                if (win32Error != 0)
                {
                    _message = $" => ERROR : ClsSymLink.GetRealPath() : FAILED TO GET FINAL PATHNAME BY HANDLE : {symbolicLinkPath} : {new Win32Exception(win32Error).Message}";
                    if (_verbose > 1) WriteLine(MdlConst.LVL_NONE, _message);
                    return string.Empty;
                }

                if (sb.Length >= 4 && sb[0] == '\\' && sb[1] == '\\' && sb[2] == '?' && sb[3] == '\\')
                {
                    realPath = sb.ToString().Substring(4);
                }
                else
                {
                    realPath = sb.ToString();
                }
                realPath = System.Text.RegularExpressions.Regex.Replace(realPath, @"^UNC\\", @"\\");
                if (isRelative) realPath = MdlFile.GetRelativePath(symbolicLinkPath, realPath);
            }
            catch (Exception ex)
            {
                _message = $"[ClsSymLink.GetRealPath()] {ex.Message}";
            }
            if (_verbose > 2) WriteLine(MdlConst.LVL_NONE, $" => ClsSymLink.GetRealPath() : SYMLINK({symbolicLinkPath}) -> REAL({realPath})");
            _realPath = realPath;
            return realPath;
        }

        /// <summary>
        /// 指定されたシンボリックリンクが参照しているターゲットの実パスを取得します（絶対パス固定）。
        /// </summary>
        /// <param name="symbolicLinkPath">対象のシンボリックリンクパス。</param>
        /// <returns>実パス文字列。取得に失敗した場合は空文字列。</returns>
        /// <example>
        /// <code>
        /// string realPath = symLink.GetRealPath(@"C:\link.txt");
        /// </code>
        /// </example>
        public string GetRealPath(string symbolicLinkPath)
        {
            return GetRealPath(symbolicLinkPath, false);
        }
    }
}

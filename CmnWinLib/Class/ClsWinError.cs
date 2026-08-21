using System;
using System.ComponentModel;
using System.Runtime.Versioning;

// 2026/08/08 Gemini 3.6 Flash (High) Review & Modified

namespace CmnWinLib.Class
{
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Windows エラーコードに対応するエラーメッセージを取得するためのユーティリティクラスです。
    /// </summary>
    public class ClsWinError
    {
        /// <summary>
        /// <see cref="ClsWinError"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <example>
        /// <code>
        /// ClsWinError winError = new ClsWinError();
        /// </code>
        /// </example>
        public ClsWinError()
        {
        }

        /// <summary>
        /// 指定されたエラーコードに対応するWindowsエラーメッセージを取得します。（非推奨）
        /// </summary>
        /// <param name="errorCode">Windows Win32 エラーコード</param>
        /// <returns>エラーコードに対応するエラーメッセージ文字列</returns>
        /// <example>
        /// <code>
        /// ClsWinError winError = new ClsWinError();
        /// string message = winError.GetWinErrMessage(2);
        /// </code>
        /// </example>
        [Obsolete("代わりに 'GetErrorMessage(int)' を使用します。")]
        public string GetWinErrMessage(int errorCode)
        {
            return GetErrorMessage(errorCode);
        }

        /// <summary>
        /// 指定された Windows Win32 エラーコードに対応するエラーメッセージを取得します。
        /// </summary>
        /// <param name="errorCode">Windows Win32 エラーコード（例: 2 は「指定されたファイルが見つかりません。」）</param>
        /// <returns>エラーコードに対応するローカライズされたエラーメッセージ文字列</returns>
        /// <example>
        /// <code>
        /// string message = ClsWinError.GetErrorMessage(2);
        /// Console.WriteLine(message);
        /// </code>
        /// </example>
        public static string GetErrorMessage(int errorCode)
        {
            return new Win32Exception(errorCode).Message;
        }
    }
}

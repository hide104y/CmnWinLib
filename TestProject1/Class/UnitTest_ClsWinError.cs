using System;
using System.ComponentModel;
using System.Runtime.Versioning;
using CmnWinLib.Class;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1.Class
{
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsWinError
    {
        // ====================================================================
        // 1. コンストラクタのテスト
        // ====================================================================

        [Fact]
        public void Constructor_InitializesSuccessfully()
        {
            var winError = new ClsWinError();
            Assert.NotNull(winError);
        }

        // ====================================================================
        // 2. GetErrorMessage (static) のテスト
        // ====================================================================

        [Theory]
        [InlineData(0)]       // ERROR_SUCCESS (0x0)
        [InlineData(2)]       // ERROR_FILE_NOT_FOUND (0x2)
        [InlineData(3)]       // ERROR_PATH_NOT_FOUND (0x3)
        [InlineData(5)]       // ERROR_ACCESS_DENIED (0x5)
        [InlineData(87)]      // ERROR_INVALID_PARAMETER (0x57)
        [InlineData(123)]     // ERROR_INVALID_NAME (0x7B)
        public void GetErrorMessage_ReturnsExpectedMessageForStandardCodes(int errorCode)
        {
            string expected = new Win32Exception(errorCode).Message;
            string actual = ClsWinError.GetErrorMessage(errorCode);

            Assert.Equal(expected, actual);
            Assert.False(string.IsNullOrWhiteSpace(actual));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(999999)]
        public void GetErrorMessage_UnknownOrNegativeErrorCode_ReturnsWin32ExceptionMessage(int errorCode)
        {
            string expected = new Win32Exception(errorCode).Message;
            string actual = ClsWinError.GetErrorMessage(errorCode);

            Assert.Equal(expected, actual);
            Assert.False(string.IsNullOrWhiteSpace(actual));
        }

        // ====================================================================
        // 3. GetWinErrMessage (Obsolete インスタンスメソッド) のテスト
        // ====================================================================

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(87)]
        public void GetWinErrMessage_Obsolete_ReturnsSameAsGetErrorMessage(int errorCode)
        {
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
            var winError = new ClsWinError();
            string expected = ClsWinError.GetErrorMessage(errorCode);
            string actual = winError.GetWinErrMessage(errorCode);

            Assert.Equal(expected, actual);
#pragma warning restore CS0618
        }
    }
}

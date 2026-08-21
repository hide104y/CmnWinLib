using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Module;
using CmnWinLib.Class;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1.Class
{
    // #pragma warning disable CA1416 // プラットフォームの互換性を検証する。
    // Windows専用クラス宣言
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsReg
    {
        public const string REG_KEY_HOME = @"SOFTWARE\InfraTools\UnitTest";
        public const string REG_KEY_HMWW = @"SOFTWARE\Wow6432Node\InfraTools\UnitTest";

        private static ClsWinLogger _logger = new();
        private ClsReg _reg = new(_logger);

        public UnitTest_ClsReg()
        {
            _reg.RegKeyHome = REG_KEY_HOME;
            _reg.RegKeyHomeWW = REG_KEY_HMWW;
        }

        // --------------------------------------------------------------------
        // SetRegistory() & GetRegistory()
        // => UACをOFFにした環境でテスト : isSkipTestAtUACIsOn = false
        // --------------------------------------------------------------------
        [Fact]
        public void レジストリに書き込み読み込みできること()
        {

            bool isSkipTestAtUACIsOn = true;
            bool isOk = false;
            string value = "UNIXTIME_" + MdlDate.GetUnixTimeString();
            string regName = "UnitTest_ClsReg";
            string? expectedValue = "";
            string progress = "";

            if (isSkipTestAtUACIsOn)
            {
                progress = "OK";
            }
            else
            {
                if (_reg.SetRegistry(ClsReg.TRGT_MACHINE_REG, regName, value, Microsoft.Win32.RegistryValueKind.String))
                {
                    expectedValue = _reg.GetRegistry(ClsReg.TRGT_MACHINE_REG, regName, Microsoft.Win32.RegistryValueKind.String);
                    if (!String.IsNullOrEmpty(expectedValue) && expectedValue.Equals(value))
                    {
                        isOk = true;
                        progress = "SETVAL(" + value + ") == GETVAL(" + expectedValue + ") [" + IntPtr.Size + "] " + _reg.GetRegKeyPath("");
                    }
                    else
                    {
                        progress = "SETVAL(" + value + ") != GETVAL(" + expectedValue + ") [" + IntPtr.Size + "] " + _reg.GetRegKeyPath("");
                    }
                    if (isOk && _reg.DeleteRegistry(ClsReg.TRGT_MACHINE_REG, regName))
                    {
                        progress = "OK";
                    }
                }
                else
                {
                    progress = "CANNOT SETREG";
                }
            }
            Assert.Equal("OK", progress);

        }
    }
    // #pragma warning restore CA1416 // プラットフォームの互換性を検証する。
}

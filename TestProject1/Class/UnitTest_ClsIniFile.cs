using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using CmnClsLib.Interface;
using CmnWinLib.Class;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1.Class
{
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsIniFile : IDisposable
    {
        private readonly string _tempFilePath;
        private readonly TestLogger _logger;
        private readonly ClsIniFile _iniFile;

        /// <summary>
        /// テスト用インメモリロガー
        /// </summary>
        private class TestLogger : ICmnLogger
        {
            public List<(int Level, string Message)> Logs { get; } = new();
            private readonly Dictionary<string, string> _values = new();

            public void WriteLine(int level, string message) => Logs.Add((level, message));
            public void Write(int level, string message) => Logs.Add((level, message));
            public void Flush() { }

            public string GetValueByKey(string key, string defaultValue)
            {
                return _values.TryGetValue(key, out var val) ? val : defaultValue;
            }

            public bool GetValueByKey(string key, bool defaultValue)
            {
                return _values.TryGetValue(key, out var val) && bool.TryParse(val, out var res) ? res : defaultValue;
            }

            public void SetValueByKey(string key, string val)
            {
                _values[key] = val;
            }

            public string GetValByKey(string key, string defaultValue) => GetValueByKey(key, defaultValue);
            public bool GetValByKey(string key, bool defaultValue) => GetValueByKey(key, defaultValue);
            public void SetValByKey(string key, string val) => SetValueByKey(key, val);
            public void Writeln(int level, string msg) => WriteLine(level, msg);
        }

        public UnitTest_ClsIniFile()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"unittest_inifile_{Guid.NewGuid():N}.ini");
            _logger = new TestLogger();
            _iniFile = new ClsIniFile(_logger)
            {
                FilePath = _tempFilePath
            };
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch
                {
                    // クリーンアップ時の例外は無視
                }
            }
        }

        // --------------------------------------------------------------------
        // プロパティ及び初期化テスト
        // --------------------------------------------------------------------
        [Fact]
        public void Properties_DefaultAndAssignment_WorkCorrectly()
        {
            var logger = new TestLogger();
            var ini = new ClsIniFile(logger);

            // 初期状態は空文字
            Assert.Equal(string.Empty, ini.FilePath);

            // 値の設定と取得
            const string testPath = @"C:\Test\config.ini";
            ini.FilePath = testPath;
            Assert.Equal(testPath, ini.FilePath);

            // null 設定時は string.Empty になること
            ini.FilePath = null!;
            Assert.Equal(string.Empty, ini.FilePath);
        }

        // --------------------------------------------------------------------
        // 文字列の読み書きテスト (WriteString / ReadString)
        // --------------------------------------------------------------------
        [Fact]
        public void WriteString_And_ReadString_BasicSuccess()
        {
            _iniFile.WriteString("ServerConfig", "HostName", "192.168.1.100");

            string result = _iniFile.ReadString("ServerConfig", "HostName", "defaultHost");
            Assert.Equal("192.168.1.100", result);
        }

        [Fact]
        public void WriteString_And_ReadString_OverwriteExistingValue()
        {
            _iniFile.WriteString("AppSettings", "Theme", "Light");
            Assert.Equal("Light", _iniFile.ReadString("AppSettings", "Theme", ""));

            _iniFile.WriteString("AppSettings", "Theme", "Dark");
            Assert.Equal("Dark", _iniFile.ReadString("AppSettings", "Theme", ""));
        }

        [Fact]
        public void ReadString_WhenKeyOrSectionOrFileDoesNotExist_ReturnsDefaultValue()
        {
            // まだファイルが存在しない状態での読み出し
            string notExistFileResult = _iniFile.ReadString("SectionNotFound", "KeyNotFound", "fallbackDefault");
            Assert.Equal("fallbackDefault", notExistFileResult);

            // ファイル作成後、別セクション・別キーの読み出し
            _iniFile.WriteString("ExistSection", "ExistKey", "ExistValue");

            string sectionNotFoundResult = _iniFile.ReadString("NonExistentSection", "ExistKey", "sectionDefault");
            Assert.Equal("sectionDefault", sectionNotFoundResult);

            string keyNotFoundResult = _iniFile.ReadString("ExistSection", "NonExistentKey", "keyDefault");
            Assert.Equal("keyDefault", keyNotFoundResult);
        }

        [Fact]
        public void ReadString_And_WriteString_HandlesNullArguments()
        {
            // section, key, value が null の場合でも例外が発生せず動作すること
            _iniFile.WriteString(null!, null!, null);

            string result = _iniFile.ReadString(null!, null!, null!);
            // 空キー・空セクションで書き込まれた空文字が取得される
            Assert.Equal(string.Empty, result);

            // defaultValue が null で存在しないキーを取得した場合、空文字にフォールバックされること
            string missingDefaultResult = _iniFile.ReadString("MissingSection", "MissingKey", null!);
            Assert.Equal(string.Empty, missingDefaultResult);
        }

        [Fact]
        public void WriteString_And_ReadString_HandlesUnicodeJapanese()
        {
            // WindowsのWritePrivateProfileStringWは、新規作成時にUnicode BOMがないとANSIファイルとして作成されるため、
            // Unicode(UTF-16)を正しく扱うにはBOM付きUnicodeファイルとして存在するか確認する
            File.WriteAllText(_tempFilePath, "\uFEFF", System.Text.Encoding.Unicode);

            const string section = "システム設定";
            const string key = "管理者名";
            const string value = "田中 太郎（テスト管理者）";

            _iniFile.WriteString(section, key, value);

            string result = _iniFile.ReadString(section, key, "デフォルト");
            Assert.Equal(value, result);
        }

        [Fact]
        public void WriteString_And_ReadString_HandlesEmptyString()
        {
            _iniFile.WriteString("EmptySection", "EmptyKey", "");

            string result = _iniFile.ReadString("EmptySection", "EmptyKey", "defaultValue");
            Assert.Equal(string.Empty, result);
        }

        // --------------------------------------------------------------------
        // 整数値の読み書きテスト (WriteInt32 / ReadInt32)
        // --------------------------------------------------------------------
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8080)]
        [InlineData(-1)]
        [InlineData(-9999)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void WriteInt32_And_ReadInt32_ValuesMatch(int expectedValue)
        {
            _iniFile.WriteInt32("PortConfig", "PortNumber", expectedValue);

            int result = _iniFile.ReadInt32("PortConfig", "PortNumber", 9999);
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public void WriteInt32_And_ReadInt32_OverwriteExistingValue()
        {
            _iniFile.WriteInt32("Database", "TimeoutSeconds", 30);
            Assert.Equal(30, _iniFile.ReadInt32("Database", "TimeoutSeconds", 0));

            _iniFile.WriteInt32("Database", "TimeoutSeconds", 120);
            Assert.Equal(120, _iniFile.ReadInt32("Database", "TimeoutSeconds", 0));
        }

        [Fact]
        public void ReadInt32_WhenKeyOrSectionOrFileDoesNotExist_ReturnsDefaultValue()
        {
            int notExistFileResult = _iniFile.ReadInt32("NonExistentSection", "NonExistentKey", 404);
            Assert.Equal(404, notExistFileResult);

            _iniFile.WriteInt32("ExistSection", "ExistKey", 200);

            int sectionNotFoundResult = _iniFile.ReadInt32("OtherSection", "ExistKey", 500);
            Assert.Equal(500, sectionNotFoundResult);

            int keyNotFoundResult = _iniFile.ReadInt32("ExistSection", "OtherKey", 501);
            Assert.Equal(501, keyNotFoundResult);
        }

        [Fact]
        public void ReadInt32_And_WriteInt32_HandlesNullArguments()
        {
            // section, key に null を渡しても例外が出ずフォールバックされること
            _iniFile.WriteInt32(null!, null!, 777);

            int result = _iniFile.ReadInt32(null!, null!, 0);
            Assert.Equal(777, result);
        }

        [Fact]
        public void ReadString_LongString_WorksWithinBufferSize()
        {
            // 500文字の長い文字列を読み書きできることを確認（バッファは1024文字）
            string longValue = new string('A', 500);
            _iniFile.WriteString("LongSection", "LongKey", longValue);

            string result = _iniFile.ReadString("LongSection", "LongKey", "");
            Assert.Equal(longValue, result);
        }

        [Fact]
        public void ReadInt32_WhenValueIsNotANumber_ReturnsZero()
        {
            // 数値に変換できない文字列が格納されている場合、Win32 API (GetPrivateProfileInt) の仕様により 0 が返る
            _iniFile.WriteString("InvalidNumberSection", "NotIntKey", "NotANumber");

            int result = _iniFile.ReadInt32("InvalidNumberSection", "NotIntKey", 999);
            Assert.Equal(0, result);
        }

        // --------------------------------------------------------------------
        // 複数セクション・複数キーの独立性テスト
        // --------------------------------------------------------------------
        [Fact]
        public void MultipleSectionsAndKeys_AreIsolatedAndIndependent()
        {
            _iniFile.WriteString("SectionA", "Key1", "ValueA1");
            _iniFile.WriteString("SectionA", "Key2", "ValueA2");
            _iniFile.WriteString("SectionB", "Key1", "ValueB1");
            _iniFile.WriteInt32("SectionB", "Key2", 999);

            Assert.Equal("ValueA1", _iniFile.ReadString("SectionA", "Key1", ""));
            Assert.Equal("ValueA2", _iniFile.ReadString("SectionA", "Key2", ""));
            Assert.Equal("ValueB1", _iniFile.ReadString("SectionB", "Key1", ""));
            Assert.Equal(999, _iniFile.ReadInt32("SectionB", "Key2", 0));
        }

        // --------------------------------------------------------------------
        // 非推奨（Obsolete）互換メソッドテスト
        // --------------------------------------------------------------------
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
        [Fact]
        public void ObsoleteMethods_GetValueString_And_GetValueInt_WorkCorrectly()
        {
            _iniFile.WriteString("LegacySection", "StringKey", "LegacyValue");
            _iniFile.WriteInt32("LegacySection", "IntKey", 12345);

            string strVal = _iniFile.GetValueString("LegacySection", "StringKey", "default");
            int intVal = _iniFile.GetValueInt("LegacySection", "IntKey", 0);

            Assert.Equal("LegacyValue", strVal);
            Assert.Equal(12345, intVal);
        }

        [Fact]
        public void ObsoleteMethods_SetValue_WorkCorrectly()
        {
            _iniFile.SetValue("LegacySection", "StringKey", "NewLegacyValue");
            _iniFile.SetValue("LegacySection", "IntKey", 54321);

            Assert.Equal("NewLegacyValue", _iniFile.ReadString("LegacySection", "StringKey", ""));
            Assert.Equal(54321, _iniFile.ReadInt32("LegacySection", "IntKey", 0));
        }
#pragma warning restore CS0618
    }
}

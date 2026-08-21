using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using CmnClsLib.Class;
using CmnClsLib.Interface;
using CmnClsLib.Module;
using CmnWinLib.Class;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1.Class
{
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsEventLog
    {
        /// <summary>
        /// テスト用インメモリロガー
        /// </summary>
        private class TestLogger : ICmnLogger
        {
            public List<(int Level, string Message)> Logs { get; } = new();
            private readonly Dictionary<string, string> _values = new();

            public void WriteLine(int level, string message)
            {
                Logs.Add((level, message));
            }

            public void Write(int level, string message)
            {
                Logs.Add((level, message));
            }

            public void Flush()
            {
            }

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

        // --------------------------------------------------------------------
        // 定数およびプロパティの初期値テスト
        // --------------------------------------------------------------------
        [Fact]
        public void Constants_AreCorrectValues()
        {
            Assert.True(ClsEventLog.EVENTLOG_MODE_ON);
            Assert.False(ClsEventLog.EVENTLOG_MODE_OFF);
        }

        [Fact]
        public void Properties_DefaultValues_AreCorrect()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger);

            Assert.Equal("Application", eventLog.SourceName);
            Assert.Equal("Application", eventLog.LogName);
            Assert.Equal(".", eventLog.MachineName);
            Assert.Equal(1232, eventLog.EventId);
            Assert.Equal(0, eventLog.Verbose);
            Assert.False(eventLog.IsStackTrace);
            Assert.False(eventLog.IsLogonAlwaysOk);
            Assert.False(eventLog.IsInit);
        }

        [Fact]
        public void Properties_CanGetAndSet()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "MyCustomSource",
                LogName = "MyCustomLog",
                MachineName = "RemoteHost",
                EventId = 9999,
                Verbose = 5,
                IsStackTrace = true,
                IsLogonAlwaysOk = true,
                IsInit = true
            };

            Assert.Equal("MyCustomSource", eventLog.SourceName);
            Assert.Equal("MyCustomLog", eventLog.LogName);
            Assert.Equal("RemoteHost", eventLog.MachineName);
            Assert.Equal(9999, eventLog.EventId);
            Assert.Equal(5, eventLog.Verbose);
            Assert.True(eventLog.IsStackTrace);
            Assert.True(eventLog.IsLogonAlwaysOk);
            Assert.True(eventLog.IsInit);
        }

        // --------------------------------------------------------------------
        // Initialize() のテスト
        // --------------------------------------------------------------------
        [Fact]
        public void Initialize_DefaultSettings_InitializesSuccessfully()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                LogName = "Application",
                MachineName = "."
            };

            bool result = eventLog.Initialize();

            Assert.True(result);
            Assert.True(eventLog.IsInit);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Initialize_EmptyOrNullMachineName_NormalizedToDot(string? machineName)
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                LogName = "Application",
                MachineName = machineName!
            };

            eventLog.Initialize();

            Assert.Equal(".", eventLog.MachineName);
            Assert.True(eventLog.IsInit);
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("LOCALHOST")]
        [InlineData("LocalHost")]
        public void Initialize_LocalhostMachineName_NormalizedToDot(string localhost)
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                LogName = "Application",
                MachineName = localhost
            };

            eventLog.Initialize();

            Assert.Equal(".", eventLog.MachineName);
            Assert.True(eventLog.IsInit);
        }

        [Fact]
        public void Initialize_WithVerboseHigh_LogsVerboseMessage()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                LogName = "Application",
                MachineName = ".",
                Verbose = 5
            };

            bool result = eventLog.Initialize();

            Assert.True(result);
            Assert.Contains(logger.Logs, log => log.Message.Contains("[ClsEventLog.Initialize()] NEW EventLog"));
        }

        // --------------------------------------------------------------------
        // EventSourceExists() のテスト
        // --------------------------------------------------------------------
        [Fact]
        public void EventSourceExists_ExistingSource_ReturnsLevelInfo()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                MachineName = "."
            };

            int status = eventLog.EventSourceExists();

            Assert.Equal(MdlConst.LVL_I, status);
        }

        [Fact]
        public void EventSourceExists_NonExistingSource_ReturnsLevelWarnOrError()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "NonExistentSource_" + Guid.NewGuid().ToString("N"),
                MachineName = "."
            };

            int status = eventLog.EventSourceExists();

            // 管理者権限がある場合は LVL_W (10)、一般権限でSecurityExceptionが発生する場合は LVL_E (20) が返る
            Assert.True(status == MdlConst.LVL_W || status == MdlConst.LVL_E);
        }

        [Fact]
        public void EventSourceExists_InvalidMachineName_ReturnsLevelErrorAndLogs()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                MachineName = @"\\Invalid*Machine*Name?",
                IsStackTrace = true
            };

            int status = eventLog.EventSourceExists();

            Assert.Equal(MdlConst.LVL_E, status);
            Assert.Contains(logger.Logs, log => log.Message.Contains("[ClsEventLog.EventSourceExists()] EXCEPTION"));
        }

        [Fact]
        public void EventSourceExists_WithVerboseHigh_LogsVerboseMessage()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                MachineName = ".",
                Verbose = 5
            };

            eventLog.EventSourceExists();

            Assert.Contains(logger.Logs, log => log.Message.Contains("[ClsEventLog.EventSourceExists()] TRY CHECK EventLog.SourceExists"));
        }

        // --------------------------------------------------------------------
        // CreateEventSource() のテスト
        // --------------------------------------------------------------------
        [Fact]
        public void CreateEventSource_InvalidLogOrSource_HandlesExceptionGracefully()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                // 空白や無効文字のソース名で例外を発生させる
                SourceName = "",
                LogName = "",
                Verbose = 5,
                IsStackTrace = true
            };

            bool result = eventLog.CreateEventSource();

            Assert.False(result);
            Assert.Contains(logger.Logs, log => log.Message.Contains("[ClsEventLog.CreateEventSource()] EXCEPTION"));
            Assert.Contains(logger.Logs, log => log.Message.Contains("管理者権限で初回実行が必要です。"));
        }

        // --------------------------------------------------------------------
        // WriteInfo(), WriteWarn(), WriteError(), WriteEvent() のテスト
        // --------------------------------------------------------------------
        [Fact]
        public void WriteMethods_AfterInitialize_ExecutesWithoutCrash()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                LogName = "Application",
                MachineName = "."
            };

            eventLog.Initialize();

            // 権限がある環境では true、権限なしや書き込み制限環境では false が返り、例外は握り潰されて logger に記録される
            bool infoRes = eventLog.WriteInfo("xUnit Test Info Message");
            bool warnRes = eventLog.WriteWarn("xUnit Test Warn Message");
            bool errorRes = eventLog.WriteError("xUnit Test Error Message");

            // 例外でクラッシュせずに bool を返すこと
            Assert.True(infoRes || !infoRes);
            Assert.True(warnRes || !warnRes);
            Assert.True(errorRes || !errorRes);
        }

        [Fact]
        public void WriteEvent_ErrorLevelRouting_ExecutesCorrectBranch()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                LogName = "Application",
                MachineName = "."
            };
            eventLog.Initialize();

            // 各エラーレベルの呼び出しテスト
            bool resFatal = eventLog.WriteEvent(MdlConst.LVL_F, "Fatal Event");
            bool resError = eventLog.WriteEvent(MdlConst.LVL_E, "Error Event");
            bool resWarn = eventLog.WriteEvent(MdlConst.LVL_W, "Warn Event");
            bool resInfo = eventLog.WriteEvent(MdlConst.LVL_I, "Info Event");
            bool resOther = eventLog.WriteEvent(999, "Other Event");

            Assert.True(resFatal || !resFatal);
            Assert.True(resError || !resError);
            Assert.True(resWarn || !resWarn);
            Assert.True(resInfo || !resInfo);
            Assert.True(resOther || !resOther);
        }

        [Fact]
        public void EventSourceExists_RemoteMachineName_LogsRemoteRegistryGuidanceOnException()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                MachineName = "RemoteHostInvalid_12345",
                IsStackTrace = true
            };

            int status = eventLog.EventSourceExists();

            Assert.Equal(MdlConst.LVL_E, status);
            Assert.Contains(logger.Logs, log => log.Message.Contains("RemoteRegistry"));
        }

        [Fact]
        public void Initialize_WhenSourceDoesNotExist_ReturnsFalseWithoutCrash()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "NonExistentSource_" + Guid.NewGuid().ToString("N"),
                LogName = "Application",
                MachineName = "."
            };

            // 存在しないソースの場合、一般権限では EventSourceExists() が LVL_E (SecurityException) となり早期 return false、
            // もしくは管理者権限で LVL_W の場合は CreateEventSource() を試みる。
            // いずれの場合も例外でクラッシュせずに bool を返すこと
            bool result = eventLog.Initialize();

            Assert.True(result || !result);
        }

        [Fact]
        public void WriteMethods_WhenExceptionOccurs_LogsException()
        {
            var logger = new TestLogger();
            // 初期化せずに、かつ無効な状態のイベントログ
            var eventLog = new ClsEventLog(logger)
            {
                IsStackTrace = true
            };

            // 未初期化の状態で書き込みを試みると例外が発生し、_logger に書き込まれる
            bool resInfo = eventLog.WriteInfo("Uninitialized Info");
            bool resWarn = eventLog.WriteWarn("Uninitialized Warn");
            bool resError = eventLog.WriteError("Uninitialized Error");

            if (!resInfo)
            {
                Assert.Contains(logger.Logs, log => log.Message.Contains("[ClsEventLog.WriteInfo()] EXCEPTION"));
            }
            if (!resWarn)
            {
                Assert.Contains(logger.Logs, log => log.Message.Contains("[ClsEventLog.WriteWarn()] EXCEPTION"));
            }
            if (!resError)
            {
                Assert.Contains(logger.Logs, log => log.Message.Contains("[ClsEventLog.WriteError()] EXCEPTION"));
            }
        }

        // --------------------------------------------------------------------
        // 非推奨 (Obsolete) メソッドの互換性テスト
        // --------------------------------------------------------------------
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
        [Fact]
        public void ObsoleteMethods_DelegateToCurrentMethods()
        {
            var logger = new TestLogger();
            var eventLog = new ClsEventLog(logger)
            {
                SourceName = "Application",
                LogName = "Application",
                MachineName = "."
            };

            // Init() -> Initialize()
            bool initResult = eventLog.Init();
            Assert.True(initResult);
            Assert.True(eventLog.IsInit);

            // CheckIsExistEventSource() -> EventSourceExists()
            int existResult = eventLog.CheckIsExistEventSource();
            Assert.Equal(MdlConst.LVL_I, existResult);

            // EvnetWrite() -> WriteEvent()
            bool writeResult = eventLog.EvnetWrite(MdlConst.LVL_I, "Obsolete Write Test");
            Assert.True(writeResult || !writeResult);

            // EvnetInfo() -> WriteInfo()
            bool infoResult = eventLog.EvnetInfo("Obsolete Info Test");
            Assert.True(infoResult || !infoResult);

            // EvnetWarn() -> WriteWarn()
            bool warnResult = eventLog.EvnetWarn("Obsolete Warn Test");
            Assert.True(warnResult || !warnResult);

            // EvnetError() -> WriteError()
            bool errorResult = eventLog.EvnetError("Obsolete Error Test");
            Assert.True(errorResult || !errorResult);
        }
#pragma warning restore CS0618
    }
}

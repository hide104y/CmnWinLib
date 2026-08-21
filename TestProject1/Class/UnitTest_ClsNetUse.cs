using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using CmnWinLib.Class;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1.Class
{
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsNetUse
    {
        #region プロパティおよび初期値テスト

        [Fact]
        public void DefaultProperties_HaveExpectedInitialValues()
        {
            // Arrange & Act
            var netUse = new ClsNetUse();

            // Assert
            Assert.NotNull(netUse.AllowedErrorCodes);
            Assert.Empty(netUse.AllowedErrorCodes);
            Assert.Equal(string.Empty, netUse.NetworkPath);
            Assert.Equal(string.Empty, netUse.DriveName);
            Assert.Equal(string.Empty, netUse.Username);
            Assert.Equal(string.Empty, netUse.Password);
            Assert.Equal(string.Empty, netUse.Domain);
            Assert.Equal(string.Empty, netUse.Message);
            Assert.False(netUse.IgnoreErrors);
        }

        [Fact]
        public void PropertyAccessors_GetAndSetExpectedValues()
        {
            // Arrange
            var netUse = new ClsNetUse();
            var errorCodes = new List<int> { 1219, 53 };

            // Act
            netUse.AllowedErrorCodes = errorCodes;
            netUse.NetworkPath = @"\\server\share";
            netUse.DriveName = "Z";
            netUse.Username = "admin";
            netUse.Password = "secret";
            netUse.Domain = "DOMAIN";
            netUse.Message = "Custom Message";
            netUse.IgnoreErrors = true;

            // Assert
            Assert.Same(errorCodes, netUse.AllowedErrorCodes);
            Assert.Equal(@"\\server\share", netUse.NetworkPath);
            Assert.Equal("Z", netUse.DriveName);
            Assert.Equal("admin", netUse.Username);
            Assert.Equal("secret", netUse.Password);
            Assert.Equal("DOMAIN", netUse.Domain);
            Assert.Equal("Custom Message", netUse.Message);
            Assert.True(netUse.IgnoreErrors);
        }

        [Fact]
        public void ObsoleteProperties_MapToNewProperties()
        {
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
            // Arrange
            var netUse = new ClsNetUse();
            var list = new List<int> { 1219 };

            // Act & Assert (Getter / Setter)
            netUse.NetUseOkErrNoList = list;
            Assert.Same(list, netUse.AllowedErrorCodes);
            Assert.Same(list, netUse.NetUseOkErrNoList);

            netUse.IsAlwaysLogonOk = true;
            Assert.True(netUse.IgnoreErrors);
            Assert.True(netUse.IsAlwaysLogonOk);

            netUse.IsAlwaysLogonOk = false;
            Assert.False(netUse.IgnoreErrors);
            Assert.False(netUse.IsAlwaysLogonOk);
#pragma warning restore CS0618
        }

        #endregion

        #region パラメータ正規化 (NormalizeParameters / FixParams) テスト

        [Theory]
        [InlineData(@"\\server\share\", @"\\server\share")]
        [InlineData(@"\\server\share/", @"\\server\share")]
        [InlineData(@"\\server\share", @"\\server\share")]
        [InlineData(@"C:\temp\", @"C:\temp")]
        public void NormalizeParameters_RemovesTrailingSeparators_FromNetworkPath(string input, string expected)
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = input
            };

            // Act
            bool result = netUse.NormalizeParameters();

            // Assert
            Assert.True(result);
            Assert.Equal(expected, netUse.NetworkPath);
        }

        [Theory]
        [InlineData("Z:", "Z")]
        [InlineData("Z:::", "Z")]
        [InlineData("Z", "Z")]
        [InlineData("", "")]
        public void NormalizeParameters_RemovesColons_FromDriveName(string input, string expected)
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                DriveName = input
            };

            // Act
            bool result = netUse.NormalizeParameters();

            // Assert
            Assert.True(result);
            Assert.Equal(expected, netUse.DriveName);
        }

        [Fact]
        public void NormalizeParameters_HandlesEmptyValuesSafely()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = "",
                DriveName = ""
            };

            // Act
            bool result = netUse.NormalizeParameters();

            // Assert
            Assert.True(result);
            Assert.Equal("", netUse.NetworkPath);
            Assert.Equal("", netUse.DriveName);
        }

        [Fact]
        public void FixParams_ObsoleteMethod_OperatesEquivalently()
        {
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = @"\\server\share\",
                DriveName = "Y:"
            };

            // Act
            bool result = netUse.FixParams();

            // Assert
            Assert.True(result);
            Assert.Equal(@"\\server\share", netUse.NetworkPath);
            Assert.Equal("Y", netUse.DriveName);
#pragma warning restore CS0618
        }

        #endregion

        #region 接続機能 (Connect) テスト

        [Fact]
        public void Connect_WhenPathIsEmpty_SetsDefaultMessageAndFails()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = ""
            };

            // Act
            bool result = netUse.Connect();

            // Assert
            Assert.False(result);
            Assert.Equal("パスが指定されていません。", netUse.NetworkPath);
            Assert.StartsWith("NG : 接続(", netUse.Message);
        }

        [Fact]
        public void Connect_InvalidNetworkPath_ReturnsFalseAndSetsNgMessage()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = @"\\invalid_server_dummy_9999\non_existent_share",
                Username = "testuser",
                Password = "dummy_password"
            };

            // Act
            bool result = netUse.Connect();

            // Assert
            Assert.False(result);
            Assert.StartsWith("NG : 接続(", netUse.Message);
            Assert.Contains(@"\\invalid_server_dummy_9999\non_existent_share", netUse.Message);
        }

        [Fact]
        public void Connect_WithIgnoreErrors_ReturnsTrueAndSetsIgnoredMessage()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = @"\\invalid_server_dummy_9999\non_existent_share",
                Username = "testuser",
                Password = "dummy_password",
                IgnoreErrors = true
            };

            // Act
            bool result = netUse.Connect();

            // Assert
            Assert.True(result);
            Assert.StartsWith("-- : 接続(", netUse.Message);
        }

        [Fact]
        public void Connect_WithAllowedErrorCodes_ReturnsTrueWhenErrorCodeMatches()
        {
            // Arrange
            var dummyPath = @"\\invalid_server_dummy_9999\non_existent_share";
            var netUseFirst = new ClsNetUse
            {
                NetworkPath = dummyPath,
                Username = "testuser",
                Password = "dummy_password"
            };
            netUseFirst.Connect();

            // エラーコードを抽出（例: "NG : 接続(53) => ..."）
            var match = Regex.Match(netUseFirst.Message, @"接続\((\d+)\)");
            Assert.True(match.Success, $"Message did not match regex: {netUseFirst.Message}");
            int errorCode = int.Parse(match.Groups[1].Value);

            var netUseSecond = new ClsNetUse
            {
                NetworkPath = dummyPath,
                Username = "testuser",
                Password = "dummy_password",
                AllowedErrorCodes = new List<int> { errorCode }
            };

            // Act
            bool result = netUseSecond.Connect();

            // Assert
            Assert.True(result);
            Assert.StartsWith("-- : 接続(", netUseSecond.Message);
        }

        [Fact]
        public void Connect_WithDomain_BuildsDomainUserAndExecutes()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = @"\\invalid_server_dummy_9999\non_existent_share",
                Domain = "TESTDOMAIN",
                Username = "testuser",
                Password = "dummy_password",
                IgnoreErrors = true
            };

            // Act
            bool result = netUse.Connect();

            // Assert
            Assert.True(result);
            Assert.StartsWith("-- : 接続(", netUse.Message);
        }

        [Fact]
        public void Connect_WithDriveName_NormalizesAndAttemptsConnection()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = @"\\invalid_server_dummy_9999\non_existent_share",
                DriveName = "X:",
                Username = "testuser",
                Password = "dummy_password",
                IgnoreErrors = true
            };

            // Act
            bool result = netUse.Connect();

            // Assert
            Assert.True(result);
            Assert.Equal("X", netUse.DriveName);
        }

        #endregion

        #region 切断機能 (Disconnect / DisConnect) テスト

        [Fact]
        public void Disconnect_WhenPathIsEmpty_SetsDefaultMessageAndAttempts()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = ""
            };

            // Act
            bool result = netUse.Disconnect();

            // Assert
            Assert.Equal("パスが指定されていません。", netUse.NetworkPath);
            // 2250 (ERROR_NOT_CONNECTED) の場合は true で "OK", それ以外のエラーの場合は false で "NG"
            if (result)
            {
                Assert.StartsWith("OK : 切断(", netUse.Message);
            }
            else
            {
                Assert.StartsWith("NG : 切断(", netUse.Message);
            }
        }

        [Fact]
        public void Disconnect_UnconnectedPath_HandlesErrorCode2250OrErrorProperly()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = @"\\invalid_server_dummy_9999\non_existent_share"
            };

            // Act
            bool result = netUse.Disconnect();

            // Assert
            // 2250 (ERROR_NOT_CONNECTED) は正常扱いになる仕様
            if (netUse.Message.Contains("切断(2250)") || netUse.Message.Contains("切断(0)"))
            {
                Assert.True(result);
                Assert.StartsWith("OK : 切断(", netUse.Message);
            }
            else
            {
                Assert.False(result);
                Assert.StartsWith("NG : 切断(", netUse.Message);
            }
        }

        [Fact]
        public void Disconnect_WithIgnoreErrors_AlwaysReturnsTrue()
        {
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = @"\\invalid_server_dummy_9999\non_existent_share",
                IgnoreErrors = true
            };

            // Act
            bool result = netUse.Disconnect();

            // Assert
            Assert.True(result);
            Assert.True(netUse.Message.StartsWith("OK : 切断(") || netUse.Message.StartsWith("-- : 切断("));
        }

        [Fact]
        public void Disconnect_WithAllowedErrorCodes_ReturnsTrueWhenErrorCodeMatches()
        {
            // Arrange
            var dummyPath = @"\\invalid_server_dummy_9999\non_existent_share";
            var netUseFirst = new ClsNetUse
            {
                NetworkPath = dummyPath
            };
            bool firstResult = netUseFirst.Disconnect();

            var match = Regex.Match(netUseFirst.Message, @"切断\((\d+)\)");
            Assert.True(match.Success, $"Message did not match regex: {netUseFirst.Message}");
            int errorCode = int.Parse(match.Groups[1].Value);

            if (errorCode == 0 || errorCode == 2250)
            {
                // 既にOK扱いされるコードの場合
                Assert.True(firstResult);
            }
            else
            {
                Assert.False(firstResult);

                var netUseSecond = new ClsNetUse
                {
                    NetworkPath = dummyPath,
                    AllowedErrorCodes = new List<int> { errorCode }
                };

                // Act
                bool secondResult = netUseSecond.Disconnect();

                // Assert
                Assert.True(secondResult);
                Assert.StartsWith("-- : 切断(", netUseSecond.Message);
            }
        }

        [Fact]
        public void DisConnect_ObsoleteMethod_WorksEquivalently()
        {
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
            // Arrange
            var netUse = new ClsNetUse
            {
                NetworkPath = @"\\invalid_server_dummy_9999\non_existent_share",
                IgnoreErrors = true
            };

            // Act
            bool result = netUse.DisConnect();

            // Assert
            Assert.True(result);
            Assert.True(netUse.Message.StartsWith("OK : 切断(") || netUse.Message.StartsWith("-- : 切断("));
#pragma warning restore CS0618
        }

        #endregion

        #region Enum 定義の検証

        [Fact]
        public void Enums_HaveExpectedValues()
        {
            // ResourceScope
            Assert.Equal(1, (int)ClsNetUse.ResourceScope.RESOURCE_CONNECTED);
            Assert.Equal(2, (int)ClsNetUse.ResourceScope.RESOURCE_GLOBALNET);
            Assert.Equal(3, (int)ClsNetUse.ResourceScope.RESOURCE_REMEMBERED);
            Assert.Equal(4, (int)ClsNetUse.ResourceScope.RESOURCE_RECENT);
            Assert.Equal(5, (int)ClsNetUse.ResourceScope.RESOURCE_CONTEXT);

            // ResourceType
            Assert.Equal(0, (int)ClsNetUse.ResourceType.RESOURCETYPE_ANY);
            Assert.Equal(1, (int)ClsNetUse.ResourceType.RESOURCETYPE_DISK);
            Assert.Equal(2, (int)ClsNetUse.ResourceType.RESOURCETYPE_PRINT);
            Assert.Equal(8, (int)ClsNetUse.ResourceType.RESOURCETYPE_RESERVED);

            // ResourceUsage
            Assert.Equal(0x00000001, (int)ClsNetUse.ResourceUsage.RESOURCEUSAGE_CONNECTABLE);
            Assert.Equal(0x00000002, (int)ClsNetUse.ResourceUsage.RESOURCEUSAGE_CONTAINER);
            Assert.Equal(0x00000004, (int)ClsNetUse.ResourceUsage.RESOURCEUSAGE_NOLOCALDEVICE);
            Assert.Equal(0x00000008, (int)ClsNetUse.ResourceUsage.RESOURCEUSAGE_SIBLING);
            Assert.Equal(0x00000010, (int)ClsNetUse.ResourceUsage.RESOURCEUSAGE_ATTACHED);
            Assert.Equal(0x00000013, (int)ClsNetUse.ResourceUsage.RESOURCEUSAGE_ALL);

            // ResourceDisplayType
            Assert.Equal(0, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_GENERIC);
            Assert.Equal(1, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_DOMAIN);
            Assert.Equal(2, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_SERVER);
            Assert.Equal(3, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_SHARE);
            Assert.Equal(4, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_FILE);
            Assert.Equal(5, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_GROUP);
            Assert.Equal(6, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_NETWORK);
            Assert.Equal(7, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_ROOT);
            Assert.Equal(8, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_SHAREADMIN);
            Assert.Equal(9, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_DIRECTORY);
            Assert.Equal(10, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_TREE);
            Assert.Equal(11, (int)ClsNetUse.ResourceDisplayType.RESOURCEDISPLAYTYPE_NDSCONTAINER);

            // AddConnectionOptions
            Assert.Equal(0x00000001, (int)ClsNetUse.AddConnectionOptions.CONNECT_UPDATE_PROFILE);
            Assert.Equal(0x00000002, (int)ClsNetUse.AddConnectionOptions.CONNECT_UPDATE_RECENT);
            Assert.Equal(0x00000004, (int)ClsNetUse.AddConnectionOptions.CONNECT_TEMPORARY);
            Assert.Equal(0x00000008, (int)ClsNetUse.AddConnectionOptions.CONNECT_INTERACTIVE);
            Assert.Equal(0x00000010, (int)ClsNetUse.AddConnectionOptions.CONNECT_PROMPT);
            Assert.Equal(0x00000020, (int)ClsNetUse.AddConnectionOptions.CONNECT_NEED_DRIVE);
            Assert.Equal(0x00000040, (int)ClsNetUse.AddConnectionOptions.CONNECT_REFCOUNT);
            Assert.Equal(0x00000080, (int)ClsNetUse.AddConnectionOptions.CONNECT_REDIRECT);
            Assert.Equal(0x00000100, (int)ClsNetUse.AddConnectionOptions.CONNECT_LOCALDRIVE);
            Assert.Equal(0x00000200, (int)ClsNetUse.AddConnectionOptions.CONNECT_CURRENT_MEDIA);
            Assert.Equal(0x00000400, (int)ClsNetUse.AddConnectionOptions.CONNECT_DEFERRED);
            Assert.Equal(unchecked((int)0xFF000000), (int)ClsNetUse.AddConnectionOptions.CONNECT_RESERVED);
            Assert.Equal(0x00000800, (int)ClsNetUse.AddConnectionOptions.CONNECT_COMMANDLINE);
            Assert.Equal(0x00001000, (int)ClsNetUse.AddConnectionOptions.CONNECT_CMD_SAVECRED);
            Assert.Equal(0x00002000, (int)ClsNetUse.AddConnectionOptions.CONNECT_CRED_RESET);
        }

        #endregion
    }
}

using System;
using System.Runtime.Versioning;
using System.Security.Principal;
using CmnWinLib.Class;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1.Class
{
    [SupportedOSPlatform("windows")]
    public class UnitTest_ClsUserRole
    {
        // ====================================================================
        // 1. コンストラクタ & プロパティのテスト
        // ====================================================================

        [Fact]
        public void Constructor_InitializesSuccessfully()
        {
            var userRole = new ClsUserRole();
            Assert.NotNull(userRole);
        }

        [Fact]
        public void IsOk_InitialValue_IsFalse()
        {
            var userRole = new ClsUserRole();
            Assert.False(userRole.IsOk);
        }

        [Fact]
        public void IsOk_Property_GetSet_WorksCorrectly()
        {
            var userRole = new ClsUserRole();

            userRole.IsOk = true;
            Assert.True(userRole.IsOk);

            userRole.IsOk = false;
            Assert.False(userRole.IsOk);
        }

        // ====================================================================
        // 2. ユーザー情報・認証関連メソッドのテスト
        // ====================================================================

        [Fact]
        public void GetUserName_ReturnsCurrentWindowsIdentityName()
        {
            var userRole = new ClsUserRole();
            string expected = WindowsIdentity.GetCurrent().Name;

            string actual = userRole.GetUserName();

            Assert.Equal(expected, actual);
            Assert.False(string.IsNullOrEmpty(actual));
        }

        [Fact]
        public void Username_Obsolete_ReturnsSameAsGetUserName()
        {
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
            var userRole = new ClsUserRole();
            string expected = userRole.GetUserName();

            string actual = userRole.Username();

            Assert.Equal(expected, actual);
#pragma warning restore CS0618
        }

        [Fact]
        public void GetAuthenticationType_ReturnsCurrentAuthenticationType()
        {
            var userRole = new ClsUserRole();
            string expected = WindowsIdentity.GetCurrent().AuthenticationType ?? "";

            string actual = userRole.GetAuthenticationType();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AuthenticationType_Obsolete_ReturnsSameAsGetAuthenticationType()
        {
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
            var userRole = new ClsUserRole();
            string expected = userRole.GetAuthenticationType();

            string actual = userRole.AuthenticationType();

            Assert.Equal(expected, actual);
#pragma warning restore CS0618
        }

        [Fact]
        public void IsAuthenticated_ReturnsCurrentAuthenticationStatus()
        {
            var userRole = new ClsUserRole();
            bool expected = WindowsIdentity.GetCurrent().IsAuthenticated;

            bool actual = userRole.IsAuthenticated();

            Assert.Equal(expected, actual);
            Assert.True(actual); // Windows ログイン環境では基本的に認証済み (true)
        }

        // ====================================================================
        // 3. ロール・管理者権限判定のテスト
        // ====================================================================

        [Theory]
        [InlineData(WindowsBuiltInRole.Administrator)]
        [InlineData(WindowsBuiltInRole.User)]
        [InlineData(WindowsBuiltInRole.Guest)]
        [InlineData(WindowsBuiltInRole.PowerUser)]
        public void IsInRole_MatchesWindowsPrincipal(WindowsBuiltInRole role)
        {
            var userRole = new ClsUserRole();
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool expected = principal.IsInRole(role);

            bool actual = userRole.IsInRole(role);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsAdministrator_MatchesWindowsBuiltInRoleAdministrator()
        {
            var userRole = new ClsUserRole();
            bool expected = userRole.IsInRole(WindowsBuiltInRole.Administrator);

            bool actual = userRole.IsAdministrator();

            Assert.Equal(expected, actual);
        }

        // ====================================================================
        // 4. トークン昇格状態 (TokenElevationType) のテスト
        // ====================================================================

        [Fact]
        public void GetTokenElevationType_ReturnsValidElevationType()
        {
            var userRole = new ClsUserRole();

            ClsUserRole.TokenElevationType elevationType = userRole.GetTokenElevationType();

            // Windows環境ではDefault(1), Full(2), Limited(3)のいずれか
            Assert.True(
                elevationType == ClsUserRole.TokenElevationType.TokenElevationTypeDefault ||
                elevationType == ClsUserRole.TokenElevationType.TokenElevationTypeFull ||
                elevationType == ClsUserRole.TokenElevationType.TokenElevationTypeLimited,
                $"Unexpected TokenElevationType: {elevationType}"
            );

            // 取得に成功した場合、IsOk は true になる
            Assert.True(userRole.IsOk);
        }

        // ====================================================================
        // 5. 列挙型 (Enum) 定義整合性のテスト
        // ====================================================================

        [Fact]
        public void TokenElevationType_EnumValues_AreDefinedCorrectly()
        {
            Assert.Equal(1, (int)ClsUserRole.TokenElevationType.TokenElevationTypeDefault);
            Assert.Equal(2, (int)ClsUserRole.TokenElevationType.TokenElevationTypeFull);
            Assert.Equal(3, (int)ClsUserRole.TokenElevationType.TokenElevationTypeLimited);
        }

        [Fact]
        public void TOKEN_ELEVATION_TYPE_Obsolete_EnumValues_MatchTokenElevationType()
        {
#pragma warning disable CS0618
            Assert.Equal((int)ClsUserRole.TokenElevationType.TokenElevationTypeDefault, (int)ClsUserRole.TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault);
            Assert.Equal((int)ClsUserRole.TokenElevationType.TokenElevationTypeFull, (int)ClsUserRole.TOKEN_ELEVATION_TYPE.TokenElevationTypeFull);
            Assert.Equal((int)ClsUserRole.TokenElevationType.TokenElevationTypeLimited, (int)ClsUserRole.TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited);
#pragma warning restore CS0618
        }

        [Fact]
        public void TokenInformationClass_EnumValues_AreDefinedCorrectly()
        {
            Assert.Equal(1, (int)ClsUserRole.TokenInformationClass.TokenUser);
            Assert.Equal(2, (int)ClsUserRole.TokenInformationClass.TokenGroups);
            Assert.Equal(3, (int)ClsUserRole.TokenInformationClass.TokenPrivileges);
            Assert.Equal(18, (int)ClsUserRole.TokenInformationClass.TokenElevationType);
            Assert.Equal(19, (int)ClsUserRole.TokenInformationClass.TokenLinkedToken);
            Assert.Equal(20, (int)ClsUserRole.TokenInformationClass.TokenElevation);
            Assert.Equal(29, (int)ClsUserRole.TokenInformationClass.MaxTokenInfoClass);
        }

        [Fact]
        public void TOKEN_INFORMATION_CLASS_Obsolete_EnumValues_MatchTokenInformationClass()
        {
#pragma warning disable CS0618
            Assert.Equal((int)ClsUserRole.TokenInformationClass.TokenUser, (int)ClsUserRole.TOKEN_INFORMATION_CLASS.TokenUser);
            Assert.Equal((int)ClsUserRole.TokenInformationClass.TokenElevationType, (int)ClsUserRole.TOKEN_INFORMATION_CLASS.TokenElevationType);
            Assert.Equal((int)ClsUserRole.TokenInformationClass.MaxTokenInfoClass, (int)ClsUserRole.TOKEN_INFORMATION_CLASS.MaxTokenInfoClass);
#pragma warning restore CS0618
        }
    }
}

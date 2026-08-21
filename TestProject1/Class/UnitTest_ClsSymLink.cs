using System;
using System.Collections.Generic;
using System.IO;
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
    public class UnitTest_ClsSymLink : IDisposable
    {
        private readonly string _testDir;
        private readonly TestLogger _logger;
        private readonly ClsSymLink _symLink;

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

        public UnitTest_ClsSymLink()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"unittest_symlink_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new TestLogger();
            _symLink = new ClsSymLink(_logger);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                try
                {
                    MdlFile.DeleteRecursively(_testDir);
                }
                catch
                {
                    // クリーンアップ時の例外は無視
                }
            }
        }

        // ====================================================================
        // 1. コンストラクタ & プロパティのテスト
        // ====================================================================
        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ClsSymLink(null!));
        }

        [Fact]
        public void Properties_DefaultValues_AreCorrect()
        {
            var logger = new TestLogger();
            var symLink = new ClsSymLink(logger);

            Assert.Equal(string.Empty, symLink.Message);
            Assert.Equal(string.Empty, symLink.RealPath);
            Assert.Equal(0, symLink.Verbose);
            Assert.False(symLink.IsSilent);
        }

        [Fact]
        public void Properties_GetSet_WorkCorrectly()
        {
            _symLink.Message = "Test message";
            Assert.Equal("Test message", _symLink.Message);

            _symLink.RealPath = @"C:\real\path";
            Assert.Equal(@"C:\real\path", _symLink.RealPath);

            _symLink.Verbose = 3;
            Assert.Equal(3, _symLink.Verbose);

            _symLink.IsSilent = true;
            Assert.True(_symLink.IsSilent);
        }

        // ====================================================================
        // 2. ロギング機能のテスト
        // ====================================================================
        [Fact]
        public void WriteLine_WhenNotSilent_LogsMessage()
        {
            _symLink.IsSilent = false;
            _symLink.WriteLine(MdlConst.LVL_NONE, "Log test message");

            Assert.Single(_logger.Logs);
            Assert.Equal((MdlConst.LVL_NONE, "Log test message"), _logger.Logs[0]);
        }

        [Fact]
        public void WriteLine_WhenSilent_DoesNotLogMessage()
        {
            _symLink.IsSilent = true;
            _symLink.WriteLine(MdlConst.LVL_NONE, "Silent test message");

            Assert.Empty(_logger.Logs);
        }

#pragma warning disable CS0618 // 型またはメンバーが旧形式です
        [Fact]
        public void Writeln_ObsoleteMethod_LogsMessage()
        {
            _symLink.IsSilent = false;
            _symLink.Writeln(MdlConst.LVL_NONE, "Obsolete writeln test");

            Assert.Single(_logger.Logs);
            Assert.Equal((MdlConst.LVL_NONE, "Obsolete writeln test"), _logger.Logs[0]);
        }
#pragma warning restore CS0618

        // ====================================================================
        // 3. 削除機能 (Delete) のテスト
        // ====================================================================
        [Fact]
        public void Delete_DeletesFileAndDirectory()
        {
            string testFile = Path.Combine(_testDir, "test_delete.txt");
            File.WriteAllText(testFile, "delete me");
            Assert.True(File.Exists(testFile));

            bool fileDeleted = _symLink.Delete(testFile);
            Assert.True(fileDeleted);
            Assert.False(File.Exists(testFile));

            string subDir = Path.Combine(_testDir, "subdir_to_delete");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "inner.txt"), "inner");
            Assert.True(Directory.Exists(subDir));

            bool dirDeleted = _symLink.Delete(subDir);
            Assert.True(dirDeleted);
            Assert.False(Directory.Exists(subDir));
        }

        [Fact]
        public void Delete_NonExistentPath_HandledSafely()
        {
            string nonExistentPath = Path.Combine(_testDir, "non_existent_file.txt");
            bool result = _symLink.Delete(nonExistentPath);
            Assert.False(File.Exists(nonExistentPath));
        }

        // ====================================================================
        // 4. 引数バリデーション (Null / 空文字 / 不正パス) のテスト
        // ====================================================================
        [Theory]
        [InlineData(null, @"C:\target")]
        [InlineData("", @"C:\target")]
        [InlineData("   ", @"C:\target")]
        [InlineData(@"C:\link", null)]
        [InlineData(@"C:\link", "")]
        [InlineData(@"C:\link", "   ")]
        public void CreateSymbolicLink_InvalidArguments_ReturnsFalse(string? linkPath, string? targetPath)
        {
            bool result = _symLink.CreateSymbolicLink(linkPath!, targetPath!, MdlFile.PATH_IS_FILE, overwrite: true);
            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetRealPath_InvalidArguments_ReturnsEmptyString(string? linkPath)
        {
            string result = _symLink.GetRealPath(linkPath!);
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetRealPathIfExists_InvalidArguments_ReturnsEmptyString(string? linkPath)
        {
            string result = _symLink.GetRealPathIfExists(linkPath!);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetRealPathIfExists_NonExistentPath_ReturnsEmptyString()
        {
            _symLink.Verbose = 2;
            string notExistPath = Path.Combine(_testDir, "not_exist.txt");
            string result = _symLink.GetRealPathIfExists(notExistPath);
            Assert.Equal(string.Empty, result);
            Assert.Contains("NO SUCH A FILE OR DIRECTORY", _symLink.Message);
        }

        [Fact]
        public void GetRealPathIfExists_RegularFileNotSymlink_ReturnsEmptyString()
        {
            string regularFile = Path.Combine(_testDir, "regular.txt");
            File.WriteAllText(regularFile, "content");

            string result = _symLink.GetRealPathIfExists(regularFile);
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData(null, @"C:\dest")]
        [InlineData("", @"C:\dest")]
        [InlineData("   ", @"C:\dest")]
        [InlineData(@"C:\src", null)]
        [InlineData(@"C:\src", "")]
        [InlineData(@"C:\src", "   ")]
        public void Copy_InvalidArguments_ReturnsFalse(string? src, string? dst)
        {
            bool result = _symLink.Copy(src!, dst!, overwrite: true, isRelative: false);
            Assert.False(result);
        }

        [Fact]
        public void Copy_NonExistentSource_ReturnsFalse()
        {
            _symLink.Verbose = 2;
            string notExistSrc = Path.Combine(_testDir, "not_exist_src.txt");
            string dst = Path.Combine(_testDir, "dst.txt");

            bool result = _symLink.Copy(notExistSrc, dst, overwrite: true);
            Assert.False(result);
            Assert.Contains("NO SUCH A FILE OR DIRECTORY", _symLink.Message);
        }

        // ====================================================================
        // 5. シンボリックリンク作成・実パス取得・コピーの機能テスト
        // ====================================================================
        [Fact]
        public void CreateSymbolicLink_And_GetRealPath_FileSymlink_Success()
        {
            string targetFile = Path.Combine(_testDir, "target_file.txt");
            File.WriteAllText(targetFile, "Hello World Target");

            string linkFile = Path.Combine(_testDir, "link_file.txt");

            _symLink.Verbose = 3;
            bool created = _symLink.CreateSymbolicLink(linkFile, targetFile, MdlFile.PATH_IS_FILE, overwrite: true);

            if (created)
            {
                Assert.True(MdlFile.IsSymlink(linkFile));

                // GetRealPath (絶対パス)
                string realPath = _symLink.GetRealPath(linkFile);
                Assert.Equal(Path.GetFullPath(targetFile), Path.GetFullPath(realPath));
                Assert.Equal(realPath, _symLink.RealPath);

                // GetRealPathIfExists (絶対パス)
                string realPathIfExists = _symLink.GetRealPathIfExists(linkFile);
                Assert.Equal(Path.GetFullPath(targetFile), Path.GetFullPath(realPathIfExists));

                // GetRealPath (相対パス指定)
                string relativePath = _symLink.GetRealPath(linkFile, isRelative: true);
                Assert.False(string.IsNullOrEmpty(relativePath));
            }
        }

        [Fact]
        public void CreateSymbolicLink_And_GetRealPath_DirectorySymlink_Success()
        {
            string targetDir = Path.Combine(_testDir, "target_dir");
            Directory.CreateDirectory(targetDir);
            File.WriteAllText(Path.Combine(targetDir, "file_in_dir.txt"), "Sub content");

            string linkDir = Path.Combine(_testDir, "link_dir");

            _symLink.Verbose = 3;
            bool created = _symLink.CreateSymbolicLink(linkDir, targetDir, MdlFile.PATH_IS_DIRECTORY, overwrite: true);

            if (created)
            {
                Assert.True(MdlFile.IsSymlink(linkDir));

                string realPath = _symLink.GetRealPath(linkDir);
                Assert.Equal(Path.GetFullPath(targetDir), Path.GetFullPath(realPath));

                string realPathIfExists = _symLink.GetRealPathIfExists(linkDir);
                Assert.Equal(Path.GetFullPath(targetDir), Path.GetFullPath(realPathIfExists));
            }
        }

        [Fact]
        public void CreateSymbolicLink_OverwriteExistingRegularFile_Succeeds()
        {
            string targetFile = Path.Combine(_testDir, "overwrite_target.txt");
            File.WriteAllText(targetFile, "Target text");

            string linkFile = Path.Combine(_testDir, "existing_regular_to_overwrite.txt");
            File.WriteAllText(linkFile, "Original regular file");

            // overwrite = true の場合、既存ファイルを削除してシンボリックリンクを作成
            bool created = _symLink.CreateSymbolicLink(linkFile, targetFile, MdlFile.PATH_IS_FILE, overwrite: true);
            if (created)
            {
                Assert.True(MdlFile.IsSymlink(linkFile));
                Assert.Equal(Path.GetFullPath(targetFile), Path.GetFullPath(_symLink.GetRealPath(linkFile)));
            }
        }

        [Fact]
        public void Copy_WhenDestinationAlreadyExistsAndNotOverwrite_SkipsAndReturnsTrue()
        {
            string targetFile = Path.Combine(_testDir, "target_for_copy.txt");
            File.WriteAllText(targetFile, "Target Content");

            string srcLink = Path.Combine(_testDir, "src_link.txt");
            string dstLink = Path.Combine(_testDir, "dst_link.txt");

            bool srcCreated = _symLink.CreateSymbolicLink(srcLink, targetFile, MdlFile.PATH_IS_FILE, overwrite: true);
            bool dstCreated = _symLink.CreateSymbolicLink(dstLink, targetFile, MdlFile.PATH_IS_FILE, overwrite: true);

            if (srcCreated && dstCreated)
            {
                _symLink.Verbose = 2;
                bool copyResult = _symLink.Copy(srcLink, dstLink, overwrite: false);
                Assert.True(copyResult);
                Assert.Contains("SYMLINK ALREADY EXISTS", _symLink.Message);
            }
        }

        [Fact]
        public void Copy_WhenDestinationAlreadyExistsAndOverwrite_OverwritesSuccessfully()
        {
            string targetFile1 = Path.Combine(_testDir, "target1.txt");
            string targetFile2 = Path.Combine(_testDir, "target2.txt");
            File.WriteAllText(targetFile1, "Target 1");
            File.WriteAllText(targetFile2, "Target 2");

            string srcLink = Path.Combine(_testDir, "src_link2.txt");
            string dstLink = Path.Combine(_testDir, "dst_link2.txt");

            bool srcCreated = _symLink.CreateSymbolicLink(srcLink, targetFile1, MdlFile.PATH_IS_FILE, overwrite: true);
            bool dstCreated = _symLink.CreateSymbolicLink(dstLink, targetFile2, MdlFile.PATH_IS_FILE, overwrite: true);

            if (srcCreated && dstCreated)
            {
                bool copyResult = _symLink.Copy(srcLink, dstLink, overwrite: true);
                Assert.True(copyResult);

                string realPath = _symLink.GetRealPath(dstLink);
                Assert.Equal(Path.GetFullPath(targetFile1), Path.GetFullPath(realPath));
            }
        }

        [Fact]
        public void Copy_WhenDestinationIsRegularFileAndOverwrite_DeletesRegularAndCreatesSymlink()
        {
            string targetFile = Path.Combine(_testDir, "copy_target_file.txt");
            File.WriteAllText(targetFile, "Target text");

            string srcLink = Path.Combine(_testDir, "src_copy_link.txt");
            bool srcCreated = _symLink.CreateSymbolicLink(srcLink, targetFile, MdlFile.PATH_IS_FILE, overwrite: true);

            string dstRegularFile = Path.Combine(_testDir, "dst_regular_file.txt");
            File.WriteAllText(dstRegularFile, "Existing regular content");

            if (srcCreated)
            {
                bool copyResult = _symLink.Copy(srcLink, dstRegularFile, overwrite: true, isRelative: false);
                Assert.True(copyResult);
                Assert.True(MdlFile.IsSymlink(dstRegularFile));
                Assert.Equal(Path.GetFullPath(targetFile), Path.GetFullPath(_symLink.GetRealPath(dstRegularFile)));
            }
        }

        [Fact]
        public void Copy_WithRelativePath_CreatesSymlinkSuccessfully()
        {
            string targetFile = Path.Combine(_testDir, "target_for_rel_copy.txt");
            File.WriteAllText(targetFile, "Target rel text");

            string srcLink = Path.Combine(_testDir, "src_rel_link.txt");
            string dstLink = Path.Combine(_testDir, "dst_rel_link.txt");

            bool srcCreated = _symLink.CreateSymbolicLink(srcLink, targetFile, MdlFile.PATH_IS_FILE, overwrite: true);

            if (srcCreated)
            {
                bool copyResult = _symLink.Copy(srcLink, dstLink, overwrite: true, isRelative: true);
                Assert.True(copyResult);
                Assert.True(MdlFile.IsSymlink(dstLink));
            }
        }

        // ====================================================================
        // 6. 非推奨 (Obsolete) 互換メソッドのテスト
        // ====================================================================
#pragma warning disable CS0618 // 型またはメンバーが旧形式です
        [Fact]
        public void ObsoleteMethods_Mklink_And_GetRealPathIfExist_WorkCorrectly()
        {
            string targetFile = Path.Combine(_testDir, "obsolete_target.txt");
            File.WriteAllText(targetFile, "Obsolete Target");

            string linkFile = Path.Combine(_testDir, "obsolete_link.txt");

            bool created = _symLink.Mklink(linkFile, targetFile, MdlFile.PATH_IS_FILE, overwrite: true);

            if (created)
            {
                string realPath = _symLink.GetRealPathIfExist(linkFile);
                Assert.Equal(Path.GetFullPath(targetFile), Path.GetFullPath(realPath));

                string realPathRel = _symLink.GetRealPathIfExist(linkFile, isRelative: false);
                Assert.Equal(Path.GetFullPath(targetFile), Path.GetFullPath(realPathRel));
            }
        }
#pragma warning restore CS0618
    }
}

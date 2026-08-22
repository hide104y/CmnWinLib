# CmnWinLib

## 事前作業
1. .NET SDKがインストールされていない場合はインストール：winget install -e --id Microsoft.DotNet.SDK.10
1. Github CLIがインストールされていない場合はインストール：winget install -e --id GitHub.cli
1. Powershellプロンプトを開く

## リポジトリ作成（未作成の場合）
```shell
# サインイン状態の確認
gh auth status
# 初回サインインしていない場合はサインイン
gh auth login
# 削除権限付与
gh auth refresh -h github.com -s delete_repo
# 作成
gh repo create CmnWinLib --private
# 確認
gh repo list | Select-String CmnWinLib
```

## リモートリポジトリ（mainブランチ）の取得
```shell
# CD
cd D:\Github\Projects
# フォルダが存在する場合は削除
if (Test-Path -Path .\CmnWinLib){rm -Recurse -Force .\CmnWinLib}
# クローン実行
git clone https://github.com/hide104y/CmnWinLib.git
```

## リモートリポジトリ（mainブランチ）にREADME.mdが存在しない場合
```shell
# CD
cd D:\Github\Projects\CmnWinLib
# ファイル作成
ruby -e "File.write('README.md', '# CmnWinLib', encoding: 'UTF-8')"
# コミット
git add README.md
git commit -m "add README.md"
# プッシュ
git push -u origin main
# ブランチの一覧表示
git branch -a
```

## ブランチの作成
```shell
# ブランチをmainに切り替え・復元
git checkout main
# ブランチ作成
git checkout -b dotnet10
# 作成したブランチをリモートにプッシュ
git push -u origin dotnet10
```

## プロジェクトの作成
```shell
# クラスライブラリ：.net 10.0
cd D:\Github\Projects\CmnWinLib
dotnet new classlib --framework net10.0 -o CmnWinLib
```

## ソリューションファイルの作成
.\CmnWinLib\CmnWinLib.slnx
```xml
<Solution>
  <Project Path="CmnWinLib/CmnWinLib.csproj" />
  <Project Path="TestProject1/TestProject1.csproj" />
</Solution>
```

## プロジェクトファイルの修正
.\CmnWinLib\CmnWinLib\CmnWinLib.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <InvariantGlobalization>false</InvariantGlobalization>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\CmnClsLib\CmnClsLib\CmnClsLib.csproj" />
    <PackageReference Include="System.Diagnostics.EventLog" Version="10.0.11" />
    <FluentValidationExcludedCultures Include="ar;el;he;hi;no;ro;sk;be;cs;cs-CZ;da;de;es;fa;fi;fr;it;ko;mk;nl;pl;pt;ru;sv;tr;uk;zh-CN;zh-CHS;zh-CHT;zh;zh-Hans;zh-Hant;pt-BR;">
      <InProject>false</InProject>
    </FluentValidationExcludedCultures>
  </ItemGroup>

  <Target Name="RemoveTranslationsAfterBuild" AfterTargets="AfterBuild">
    <RemoveDir Directories="@(FluentValidationExcludedCultures->'$(OutputPath)%(Filename)')" />
  </Target>

</Project>
```
※現状、「AllowUnsafeBlocks」を許可しているので注意

## 依存パッケージ
```shell
# CD
cd D:\Github\Projects
# 依存プロジェクト参照の追加
dotnet add .\FsMkShortCut\FsMkShortCut\FsMkShortCut.csproj reference .\CmnClsLib\CmnClsLib\CmnClsLib.csproj
# 依存パッケージのインストール
dotnet add .\FsMkShortCut\FsMkShortCut\FsMkShortCut.csproj package System.Diagnostics.EventLog --version 10.0.11
```

## コーディング
(省略)

## AIレビュー
```shell
# CD
cd D:\Github\Projects
agy
.\CmnWinLib\CmnWinLib\Class\ClsIniFile.csに対して、スキル「source-review」を実行して
/clear
.\CmnWinLib\CmnWinLib\Class\ClsEventLog.csに対して、スキル「source-review」を実行して
/exit
```

## ビルド
```shell
# CD
cd D:\Github\Projects
# ビルド
dotnet build .\CmnWinLib\CmnWinLib.slnx -c Release -p:InvariantGlobalization=false
dotnet build .\CmnWinLib\TestProject1\TestProject1.csproj
# 単体テスト
dotnet test .\CmnWinLib\TestProject1\TestProject1.csproj
```

## リポジトリにコミット
```shell
cd D:\Github\Projects\CmnWinLib
git switch dotnet10
git add .
git commit -m "README.mdの修正"
git push -u origin dotnet10
```

## デプロイ
```shell
dotnet publish .\CmnWinLib\CmnWinLib\CmnWinLib.csproj -c Release -o D:\Github\bin.n10 -r win-x64 --self-contained=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:PublishTrimmed=false -p:PublishAot=false -p:InvariantGlobalization=false
```

## リモートリポジトリの確認
- https://github.com/hide104y/CmnWinLib/tree/dotnet10
<br>※GitHubの画面で「Compare & pull request」が表示されるが放置

## リモートリポジトリ（dotnet10ブランチ）の取得
```shell
# CD
cd D:\Github\Projects
# フォルダが存在する場合は削除
if (Test-Path -Path .\CmnWinLib){rm -Recurse -Force .\CmnWinLib}
# クローン実行
git clone -b dotnet10 https://github.com/hide104y/CmnWinLib.git
```

## License
- These codes are licensed under CC0.
- http://creativecommons.org/publicdomain/zero/1.0/deed.ja

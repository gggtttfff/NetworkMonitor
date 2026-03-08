# 网络监控工具

## 功能说明
这是一个 Windows 桌面应用程序，用于自动监控网络连接状态。当检测到网络断开时，会自动打开浏览器访问校园网登录页面（默认 `http://2.2.2.2`）。

## 主要特性
- 定时检查网络连接状态（可自定义检查间隔）
- 网络断开时自动打开浏览器
- 图形化界面，支持手动启动/停止监控
- 支持日志保存与日志保留天数配置

## 构建与打包

### 前置要求
- Windows 10/11
- .NET SDK 9.0（项目目标框架：`net9.0-windows`）
- （可选）Inno Setup 6，用于生成安装包 `.exe`

### 本地构建
```powershell
# 在项目根目录执行
dotnet restore
dotnet build -c Release
```

### 本地运行
```powershell
dotnet run
```

### 发布（自包含单文件）
```powershell
dotnet publish .\NetworkMonitor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\out\publish\win-x64
```

发布输出目录：
- `out\publish\win-x64\NetworkMonitor.exe`

### 生成安装包（.exe）
项目已提供脚本 `build-installer.ps1`：

```powershell
# 示例：生成 V1.0.0 安装包
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1 -Version "1.0.0"
```

可选参数：
- `-Configuration`（默认 `Release`）
- `-Runtime`（默认 `win-x64`）
- `-InnoSetupCompiler`（自定义 `ISCC.exe` 路径）

安装包输出目录：
- `out\installer\NetworkMonitor-Setup-<版本号>.exe`

## GitHub 自动部署
仓库已配置 GitHub Actions 工作流 [`.github/workflows/dotnet-desktop.yml`](./.github/workflows/dotnet-desktop.yml)：

- 推送到 `master` 时自动构建并上传发布产物
- 推送 `v*` 标签时自动生成安装包并创建 GitHub Release
- Release 附件包含 zip 包和 Inno Setup 安装包

发布示例：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

说明：
- 自动发布依赖 GitHub 默认提供的 `GITHUB_TOKEN`
- 工作流会在 `windows-latest` 上安装 Inno Setup 后再生成安装包

## 使用说明
1. 启动程序。
2. 打开设置，配置登录地址、账号和密码。
3. 在设置中测试登录通过后保存。
4. 点击“启动监控”开始检测网络。
5. 检测到断网后会自动打开登录页面。

## 卸载说明
- 卸载时会弹出“是否同时删除配置文件、日志和测试结果”的选项。
- 选择“是”后会删除安装目录下的运行期数据（如 `appsettings.json`、`logs`、`test_results`、`debug`）。

## 技术实现
- WinForms UI
- 网络连通性检测与自动登录流程
- 本地 JSON 配置持久化
- 日志文件轮转与保留策略

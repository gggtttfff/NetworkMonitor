# 网络监控工具

## 功能说明
这是一个Windows桌面应用程序,用于自动监控网络连接状态。当检测到网络断开时,会自动打开浏览器访问 http://2.2.2.2 登录页面。

## 主要特性
- 定时检查网络连接状态(可自定义检查间隔)
- 网络断开时自动打开浏览器
- 图形化界面,操作简单
- 可随时启动/停止监控

## 编译和运行

### 前置要求
- .NET 6.0 SDK 或更高版本
- Windows 10 或更高版本

### 编译
```bash
cd NetworkMonitor
dotnet build
```

### 运行
```bash
dotnet run
```

### 发布为独立可执行文件
```bash
dotnet publish -c Release -r win10-x64 --self-contained true -p:PublishSingleFile=true
```

生成的exe文件位于: `bin\Release\net6.0-windows\win10-x64\publish\NetworkMonitor.exe`

## 使用说明
1. 启动程序
2. 设置检查间隔(默认5秒)
3. 点击"启动监控"按钮
4. 程序会定时检查网络状态
5. 检测到网络断开时自动打开浏览器访问登录页面

## 技术实现
- 使用 Ping 检测网络连接(目标: 8.8.8.8 和 114.114.114.114)
- 使用 Timer 实现定时检查
- 使用 Process.Start 打开默认浏览器
- WinForms 实现图形界面

## 注意事项
- 需要管理员权限以确保网络检测准确
- 防火墙可能会阻止 ICMP 请求
- 首次运行可能需要允许程序通过防火墙

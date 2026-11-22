# 考试指令系统

基于 **C# / WPF (.NET 8)** 的考试大屏指令桌面应用：蓝底液晶风格显示考试科目、开考/结束时间、系统时钟与倒计时，到点自动播放铃声。

## 功能

- **参数设置页**（白底黑字）：首行标题、考试科目、开考日期时间、考试时长、底部落款
- **时分秒**：可下拉选择，也可直接键盘输入
- **状态机**
  - 考试前：`[科目] 准备` + 距离开始倒计时
  - 开始瞬间：播放约 10 秒铃声
  - 进行中：`[科目] 进行中...` + 剩余时间
  - 结束瞬间：再次播放铃声
  - 结束后：`[科目] 结束` + 停笔提示语
- **主界面**：Viewbox 等比缩放；默认窗口化，可拖动；右上角图标按钮支持全屏切换 / 返回设置（Esc 同样可切换全屏）
- **关于页**：应用名称与版本、作者、圆形头像、可点击 GitHub 链接
- **字体**：内嵌 MiSans（Normal / Demibold）

## 运行

### 开发

```powershell
dotnet run
```

### 发布单文件 exe

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

生成：`publish\ExamCommand.exe`

> 目标机器无需另装 .NET 运行时（自包含）。若改为 `--self-contained false`，需已安装 .NET 8 Desktop Runtime。

## 项目结构

```
├── ExamCommand.csproj      # net8.0-windows / WPF
├── App.xaml(.cs)           # 应用资源（字体、画笔）
├── SettingsWindow.xaml(.cs)# 参数设置
├── MainWindow.xaml(.cs)    # 大屏主界面与状态机
├── AboutWindow.xaml(.cs)   # 关于对话框
├── ExamSettings.cs         # 参数模型
└── Resources/              # MiSans、铃声、头像
```

## 使用说明

1. 启动后填写考试参数，点击 **开始考试**
2. 进入大屏界面；右上角 **←** 返回设置，**⤢** 切换全屏
3. 关闭设置窗口即退出整个应用

## 作者

- **lyx** · GitHub：[https://github.com/lyx-star](https://github.com/lyx-star)

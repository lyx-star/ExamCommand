using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ExamCommand;

public partial class MainWindow : Window
{
    private enum ExamPhase
    {
        Before,
        Running,
        Finished,
    }

    private readonly ExamSettings _settings;
    private readonly DispatcherTimer _timer;
    private readonly MediaPlayer _player = new();
    private readonly System.Timers.Timer _bellStopTimer = new(10_000) { AutoReset = false };

    private ExamPhase _phase;
    private bool _bellPlayedForBefore;
    private bool _bellPlayedForFinish;

    public MainWindow(ExamSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        TitleText.Text = settings.Title;
        StartTimeText.Text = settings.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
        EndTimeText.Text = settings.EndTime.ToString("yyyy-MM-dd HH:mm:ss");
        FooterLeftText.Text = settings.FooterLeft;
        FooterRightText.Text = settings.FooterRight;

        // 初始相位
        var now = DateTime.Now;
        _phase = now < settings.StartTime ? ExamPhase.Before
            : now < settings.EndTime ? ExamPhase.Running
            : ExamPhase.Finished;

        _bellStopTimer.Elapsed += (_, _) =>
        {
            // MediaPlayer 应在拥有线程（UI）上操作
            Dispatcher.BeginInvoke(() => _player.Close());
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        Refresh();
    }

    private void Refresh()
    {
        var now = DateTime.Now;
        ClockText.Text = now.ToString("HH:mm:ss");

        var next = now < _settings.StartTime ? ExamPhase.Before
            : now < _settings.EndTime ? ExamPhase.Running
            : ExamPhase.Finished;

        // 跨越触发点 → 播放铃声
        if (next != _phase)
        {
            if (next == ExamPhase.Running && !_bellPlayedForBefore)
            {
                PlayBell();
                _bellPlayedForBefore = true;
            }
            else if (next == ExamPhase.Finished && !_bellPlayedForFinish)
            {
                PlayBell();
                _bellPlayedForFinish = true;
            }
            _phase = next;
        }

        switch (_phase)
        {
            case ExamPhase.Before:
                SubjectText.Text = $"{_settings.Subject} 准备";
                StatusText.Text = "距离开始：" + FormatSpan(_settings.StartTime - now);
                break;
            case ExamPhase.Running:
                SubjectText.Text = $"{_settings.Subject} 进行中...";
                StatusText.Text = "剩余：" + FormatSpan(_settings.EndTime - now);
                break;
            case ExamPhase.Finished:
                SubjectText.Text = $"{_settings.Subject} 结束";
                StatusText.Text = "请考生立即停笔！全体起立，经检查无误后方可离开！";
                break;
        }
    }

    /// <summary>不足 1 小时省略“xx小时”。</summary>
    private static string FormatSpan(TimeSpan span)
    {
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;
        var totalSeconds = (long)span.TotalSeconds;
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;
        return hours > 0
            ? $"{hours}小时{minutes}分钟{seconds}秒"
            : $"{minutes}分钟{seconds}秒";
    }

    private void PlayBell()
    {
        try
        {
            var path = EnsureBellOnDisk();
            if (path is null) return;
            _player.Open(new Uri(path, UriKind.Absolute));
            _player.Volume = 1.0;
            _player.Play();
            _bellStopTimer.Stop();
            _bellStopTimer.Start();
        }
        catch
        {
            // 铃声缺失或设备不可用时静默忽略，不影响考试显示
        }
    }

    /// <summary>将内嵌 bell.mp3 解包到临时文件（MediaPlayer 需要真实路径）。</summary>
    private static string? EnsureBellOnDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), "ExamCommand_bell.mp3");
        if (File.Exists(path)) return path;

        try
        {
            var uri = new Uri("pack://application:,,,/Resources/bell.mp3", UriKind.Absolute);
            var info = Application.GetResourceStream(uri)
                       ?? throw new InvalidOperationException("铃声资源未找到。");
            using (var src = info.Stream)
            using (var dst = File.Create(path))
            {
                src.CopyTo(dst);
            }
            return path;
        }
        catch
        {
            return null;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ToggleFullscreen();
        }
        else if (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.Alt)
        {
            Application.Current.Shutdown();
        }
    }

    /// <summary>非全屏时按住窗口空白处可拖动；点击按钮不受影响。</summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (WindowState != WindowState.Normal) return;
        if (e.OriginalSource is DependencyObject d && FindInteractive(d) is not null) return;
        try
        {
            DragMove();
        }
        catch
        {
            // 未按住鼠标左键等异常情况忽略
        }
    }

    private static DependencyObject? FindInteractive(DependencyObject current)
    {
        while (current is not null)
        {
            if (current is System.Windows.Controls.Button or System.Windows.Controls.Primitives.ToggleButton
                or System.Windows.Controls.ComboBox or System.Windows.Controls.TextBox
                or System.Windows.Controls.Calendar or System.Windows.Controls.Primitives.Popup
                or System.Windows.Controls.ScrollViewer)
            {
                return current;
            }
            current = current switch
            {
                FrameworkElement fe => fe.Parent,
                FrameworkContentElement fce => fce.Parent,
                _ => VisualTreeHelper.GetParent(current),
            };
        }
        return null;
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            // 四角向外：进入全屏提示
            FullscreenIcon.Data = Geometry.Parse("M2,6 V2 H6 M14,2 H18 V6 M18,14 V18 H14 M6,18 H2 V14");
        }
        else
        {
            WindowState = WindowState.Maximized;
            // 四角向内：退出全屏提示
            FullscreenIcon.Data = Geometry.Parse("M6,2 V6 H2 M14,2 V6 H18 M18,14 V18 H14 M6,18 V14 H2");
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        SettingsWindow? settings = null;
        foreach (Window w in Application.Current.Windows)
        {
            if (w is SettingsWindow sw)
            {
                settings = sw;
                break;
            }
        }

        settings ??= new SettingsWindow();
        settings.Show();
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _bellStopTimer.Dispose();
        _player.Close();
        Application.Current.Shutdown();
        base.OnClosed(e);
    }
}

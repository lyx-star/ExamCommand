using System.Windows;
using System.Windows.Controls;

namespace ExamCommand;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        for (var i = 0; i < 24; i++) StartHour.Items.Add($"{i:D2}");
        for (var i = 0; i < 60; i++) StartMinute.Items.Add($"{i:D2}");
        for (var i = 0; i < 60; i++) StartSecond.Items.Add($"{i:D2}");

        // 默认：下一个整点
        var next = DateTime.Now.Date.AddHours(DateTime.Now.Hour + 1);
        StartDate.SelectedDate = next.Date;
        SetTimeValue(StartHour, next.Hour);
        SetTimeValue(StartMinute, 0);
        SetTimeValue(StartSecond, 0);

        // 键入后失焦时规范化为两位数字
        StartHour.LostFocus += (_, _) => NormalizeTimeBox(StartHour, 23);
        StartMinute.LostFocus += (_, _) => NormalizeTimeBox(StartMinute, 59);
        StartSecond.LostFocus += (_, _) => NormalizeTimeBox(StartSecond, 59);
    }

    /// <summary>
    /// 关闭设置窗即退出整个应用：开始考试后主界面仅 Hide，
    /// 若不显式 Shutdown，隐藏的主窗口会让进程残留在后台。
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        Application.Current.Shutdown();
        base.OnClosed(e);
    }

    private static void SetTimeValue(ComboBox box, int value)
    {
        var text = $"{value:D2}";
        box.SelectedIndex = -1;
        box.Text = text;
        for (var i = 0; i < box.Items.Count; i++)
        {
            if ((string)box.Items[i] == text)
            {
                box.SelectedIndex = i;
                break;
            }
        }
    }

    private static void NormalizeTimeBox(ComboBox box, int max)
    {
        var text = box.Text?.Trim() ?? string.Empty;
        if (text.Length == 0) return;
        if (!int.TryParse(text, out var v))
        {
            box.Text = string.Empty;
            return;
        }
        if (v < 0) v = 0;
        if (v > max) v = max;
        box.Text = $"{v:D2}";
        for (var i = 0; i < box.Items.Count; i++)
        {
            if ((string)box.Items[i] == box.Text)
            {
                box.SelectedIndex = i;
                return;
            }
        }
        box.SelectedIndex = -1;
    }

    private static bool TryGetTimeValue(ComboBox box, int max, out int value, out string error)
    {
        error = string.Empty;
        value = 0;

        // 优先读键入文本，其次读选中项
        var raw = box.Text?.Trim();
        if (string.IsNullOrEmpty(raw) && box.SelectedItem is string s)
            raw = s;

        if (string.IsNullOrEmpty(raw))
        {
            error = $"请输入 0–{max} 之间的数字。";
            return false;
        }

        if (!int.TryParse(raw, out value) || value < 0 || value > max)
        {
            error = $"「{raw}」无效，请输入 0–{max} 之间的数字。";
            return false;
        }

        return true;
    }

    private void Duration45_Click(object sender, RoutedEventArgs e) => DurationBox.Text = "45";
    private void Duration75_Click(object sender, RoutedEventArgs e) => DurationBox.Text = "75";
    private void Duration90_Click(object sender, RoutedEventArgs e) => DurationBox.Text = "90";
    private void Duration120_Click(object sender, RoutedEventArgs e) => DurationBox.Text = "120";

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (StartDate.SelectedDate is not { } date)
        {
            MessageBox.Show("请选择开考日期。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetTimeValue(StartHour, 23, out var hour, out var hourErr))
        {
            MessageBox.Show("开考时间 · 时：" + hourErr, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            StartHour.Focus();
            return;
        }

        if (!TryGetTimeValue(StartMinute, 59, out var minute, out var minErr))
        {
            MessageBox.Show("开考时间 · 分：" + minErr, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            StartMinute.Focus();
            return;
        }

        if (!TryGetTimeValue(StartSecond, 59, out var second, out var secErr))
        {
            MessageBox.Show("开考时间 · 秒：" + secErr, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            StartSecond.Focus();
            return;
        }

        if (!int.TryParse(DurationBox.Text?.Trim(), out var minutes) || minutes <= 0 || minutes > 24 * 60)
        {
            MessageBox.Show("请输入有效的考试时长（1–1440 分钟）。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            DurationBox.Focus();
            return;
        }

        var start = date.Date.AddHours(hour).AddMinutes(minute).AddSeconds(second);

        var settings = new ExamSettings
        {
            Title = TitleBox.Text?.Trim() is { Length: > 0 } t ? t : "考试指令系统",
            Subject = SubjectBox.Text?.Trim() is { Length: > 0 } s ? s : "科目",
            StartTime = start,
            EndTime = start.AddMinutes(minutes),
            FooterLeft = FooterLeftBox.Text?.Trim() ?? string.Empty,
            FooterRight = FooterRightBox.Text?.Trim() ?? string.Empty,
        };

        var main = new MainWindow(settings);
        Application.Current.MainWindow = main;
        main.Show();
        Hide();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow { Owner = this };
        about.ShowDialog();
    }
}

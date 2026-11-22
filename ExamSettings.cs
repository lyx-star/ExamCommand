namespace ExamCommand;

public sealed class ExamSettings
{
    public string Title { get; set; } = "考试指令系统";
    public string Subject { get; set; } = "科目";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string FooterLeft { get; set; } = string.Empty;
    public string FooterRight { get; set; } = string.Empty;
}

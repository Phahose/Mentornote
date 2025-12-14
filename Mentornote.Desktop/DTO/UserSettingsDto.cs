namespace Mentornote.Desktop.DTO
{
    public class UserSettingsDto
    {
        public int RecentUtteranceCount { get; set; }
        public ResponseFormat ResponseFormat { get; set; } = ResponseFormat.Guided;
        public ResponseTone ResponseTone { get; set; } = ResponseTone.Professional;
        public ResumeUsage ResumeUsage { get; set; } = ResumeUsage.RelevantOnly;
        public Theme Theme { get; set; } = Theme.Dark;
        public double Creativity { get; set; } = 0.6;
    }

    public enum ResponseFormat
    {
        BulletPoints = 0,
        Guided = 1,
        FullScript = 2
    }
    public enum ResumeUsage
    {
        RelevantOnly = 0,
        PreferResume = 1,
        AlwaysUseResume = 2
    }
    public enum ResponseTone
    {
        Professional = 0,
        Polite = 1,
        Casual = 2,
        Executive = 3
    }

    public enum Theme
    {
        Dark = 0,
        Light = 1
    }


}

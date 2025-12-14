namespace Mentornote.Backend.Models
{
    public class AppSettings
    {
        public int RecentUtteranceCount { get; set; }  
        public ResponseFormat ResponseFormat { get; set; }
        public ResponseTone ResponseTone { get; set; } 
        public ResumeUsage ResumeUsage { get; set; }
        public Theme Theme { get; set; } 
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

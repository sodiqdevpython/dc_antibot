namespace dc_antibot.AntiBot.Core
{
    public enum Severity
    {
        Info,       
        Low,       
        Medium,    
        High,       
        Critical    
    }

    public static class SeverityHelper
    {
        public static Severity FromScore(byte score)
        {
            if (score <= 3) return Severity.Info;
            if (score <= 5) return Severity.Low;
            if (score <= 7) return Severity.Medium;
            if (score <= 9) return Severity.High;
            return Severity.Critical;
        }
    }
}

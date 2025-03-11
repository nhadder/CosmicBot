namespace CosmicBot.Helpers
{
    public enum TimeZoneEnum
    {
        UTC,
        PacificStandardTime,
        MountainStandardTime,
        CentralStandardTime,
        EasternStandardTime,
        GreenwichMeanTime,
        CentralEuropeanStandardTime,
        IndiaStandardTime,
        ChinaStandardTime,
        JapanStandardTime,
        AustralianEasternStandardTime
    }

    public static class TimeZoneHelper
    {
        public static string GetTimeZoneId(TimeZoneEnum timeZone)
        {
            return timeZone switch
            {
                TimeZoneEnum.UTC => "UTC",
                TimeZoneEnum.PacificStandardTime => "Pacific Standard Time",
                TimeZoneEnum.MountainStandardTime => "Mountain Standard Time",
                TimeZoneEnum.CentralStandardTime => "Central Standard Time",
                TimeZoneEnum.EasternStandardTime => "Eastern Standard Time",
                TimeZoneEnum.GreenwichMeanTime => "GMT Standard Time",
                TimeZoneEnum.CentralEuropeanStandardTime => "Central European Standard Time",
                TimeZoneEnum.IndiaStandardTime => "India Standard Time",
                TimeZoneEnum.ChinaStandardTime => "China Standard Time",
                TimeZoneEnum.JapanStandardTime => "Tokyo Standard Time",
                TimeZoneEnum.AustralianEasternStandardTime => "AUS Eastern Standard Time",
                _ => throw new ArgumentOutOfRangeException(nameof(timeZone), "Invalid time zone selected")
            };
        }

        public static DateTime GetGuildTimeNow(string timeZoneId)
        {
            try
            {
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.UtcNow;
            }
            catch (Exception)
            {
                return DateTime.UtcNow;
            }
        }
    }
}

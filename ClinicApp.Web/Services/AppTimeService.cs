namespace ClinicApp.Web.Services
{
    public class AppTimeService : IAppTimeService
    {
        private readonly TimeZoneInfo _tz;

        public AppTimeService(IConfiguration configuration)
        {
            var tzId = configuration["APP_TIME_ZONE"] ?? "Asia/Jerusalem";
            try
            {
                _tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            }
            catch
            {
                _tz = TimeZoneInfo.FindSystemTimeZoneById(
                    OperatingSystem.IsWindows() ? "Israel Standard Time" : "Asia/Jerusalem");
            }
        }

        public DateTime UtcNow() => DateTime.UtcNow;

        public DateTime LocalNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);

        public DateTime LocalToday() => LocalNow().Date;

        public DateTime ToLocal(DateTime utcDateTime)
        {
            var utc = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, _tz);
        }

        public DateTime ToUtc(DateTime localDateTime)
        {
            var unspec = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(unspec, _tz);
        }

        public DateTime? ToLocal(DateTime? utcDateTime)
            => utcDateTime.HasValue ? ToLocal(utcDateTime.Value) : null;

        public DateTime? ToUtc(DateTime? localDateTime)
            => localDateTime.HasValue ? ToUtc(localDateTime.Value) : null;

        public (DateTime start, DateTime end) TodayUtcRange()
            => DateUtcRange(LocalToday());

        public (DateTime start, DateTime end) DateUtcRange(DateTime localDate)
        {
            var localStart = localDate.Date;
            return (ToUtc(localStart), ToUtc(localStart.AddDays(1)));
        }
    }
}

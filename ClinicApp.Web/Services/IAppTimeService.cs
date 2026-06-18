namespace ClinicApp.Web.Services
{
    public interface IAppTimeService
    {
        DateTime UtcNow();
        DateTime LocalNow();
        DateTime LocalToday();
        DateTime ToLocal(DateTime utcDateTime);
        DateTime ToUtc(DateTime localDateTime);
        DateTime? ToLocal(DateTime? utcDateTime);
        DateTime? ToUtc(DateTime? localDateTime);
        (DateTime start, DateTime end) TodayUtcRange();
        (DateTime start, DateTime end) DateUtcRange(DateTime localDate);
    }
}

public static class Reloj
{
    public static DateTime Ahora()
    {
        foreach (var id in new[] { "Argentina Standard Time", "America/Argentina/Buenos_Aires" })
            try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(id)); }
            catch { }
        return DateTime.UtcNow.AddHours(-3);
    }
}
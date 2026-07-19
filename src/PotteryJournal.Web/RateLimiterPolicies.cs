namespace PotteryJournal.Web
{
    /// <summary>
    /// Names of the rate limiter policies configured in <c>Program.cs</c>, shared with the page
    /// classes that reference them via <c>[EnableRateLimiting]</c>.
    /// </summary>
    public static class RateLimiterPolicies
    {
        public const string DataEndpoints = "DataEndpoints";

        public const string LoginAttempts = "LoginAttempts";

        public const string ClassBooking = "ClassBooking";
    }
}

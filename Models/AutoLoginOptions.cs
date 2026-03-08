namespace NetworkMonitor
{
    public class AutoLoginOptions
    {
        public string LoginUrl { get; set; } = "http://2.2.2.2";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
    }

    public class AutoLoginResult
    {
        public bool Success { get; set; }
        public bool Canceled { get; set; }
        public int AttemptCount { get; set; }
        public string LastErrorMessage { get; set; } = "";
    }
}

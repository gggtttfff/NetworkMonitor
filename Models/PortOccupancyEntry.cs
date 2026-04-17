namespace NetworkMonitor
{
    public class PortOccupancyEntry
    {
        public string Protocol { get; set; } = string.Empty;
        public string LocalAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string State { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
    }

    public class ProcessTerminationResult
    {
        public bool Success { get; set; }
        public bool RequiresElevation { get; set; }
        public bool UserCanceledElevation { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

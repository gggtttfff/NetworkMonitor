using System;
using System.Collections.Generic;

namespace NetworkMonitor
{
    public class NetworkAdapterInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }

        public string GetDisplayText()
        {
            string state = IsEnabled ? "已启用" : "已禁用";
            return $"{Name} ({state})";
        }

        public override string ToString()
        {
            return $"{Name} | {Description} | {(IsEnabled ? "已启用" : "已禁用")}";
        }
    }

    public class AdapterScheduleEntry
    {
        public DayOfWeek Day { get; set; }
        public bool Enabled { get; set; }
        public bool AllDay { get; set; }
        public string StartTime { get; set; } = "00:00:00";
        public string EndTime { get; set; } = "23:59:59";

        public static List<AdapterScheduleEntry> CreateDefaultWeek()
        {
            return new List<AdapterScheduleEntry>
            {
                Create(DayOfWeek.Monday, true, true),
                Create(DayOfWeek.Tuesday, true, true),
                Create(DayOfWeek.Wednesday, true, true),
                Create(DayOfWeek.Thursday, true, true),
                Create(DayOfWeek.Friday, true, true),
                Create(DayOfWeek.Saturday, true, true),
                Create(DayOfWeek.Sunday, true, true)
            };
        }

        public TimeSpan GetStartTimeOrDefault()
        {
            return TimeSpan.TryParse(StartTime, out var value) ? value : TimeSpan.Zero;
        }

        public TimeSpan GetEndTimeOrDefault()
        {
            return TimeSpan.TryParse(EndTime, out var value) ? value : new TimeSpan(23, 59, 59);
        }

        private static AdapterScheduleEntry Create(DayOfWeek day, bool enabled, bool allDay)
        {
            return new AdapterScheduleEntry
            {
                Day = day,
                Enabled = enabled,
                AllDay = allDay,
                StartTime = "00:00:00",
                EndTime = "23:59:59"
            };
        }
    }
}

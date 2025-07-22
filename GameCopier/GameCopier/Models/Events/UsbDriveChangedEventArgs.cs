using System;
using System.Collections.Generic;

namespace GameCopier.Models.Events
{
    public class UsbDriveChangedEventArgs : EventArgs
    {
        public List<string> AddedDrives { get; set; } = new();
        public List<string> RemovedDrives { get; set; } = new();
        public string? MostRecentDrive { get; set; }
    }
}
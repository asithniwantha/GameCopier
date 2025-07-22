using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GameCopier.Models.Configuration
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class DriveDisplaySettings
    {
        public bool ShowRemovableDrives { get; set; } = true;
        public bool ShowFixedDrives { get; set; } = true; // Changed to true for better USB detection
        public bool ShowNetworkDrives { get; set; } = false;
        public bool ShowCdRomDrives { get; set; } = false;
        public bool ShowRamDrives { get; set; } = false;
        public bool ShowUnknownDrives { get; set; } = false;
        public bool HideSystemDrive { get; set; } = true;
        public List<string> HiddenDriveLetters { get; set; } = new();

        // Copy performance settings - Now defaults to Windows Explorer dialog
        public Services.CopyMethod PreferredCopyMethod { get; set; } = Services.CopyMethod.ExplorerDialog;
        public bool UseLargeDiskBuffer { get; set; } = true;
        public int MaxConcurrentCopyJobs { get; set; } = 1; // Sequential for Explorer dialog visibility
    }
}
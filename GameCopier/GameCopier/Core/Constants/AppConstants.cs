namespace GameCopier.Core.Constants
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class AppConstants
    {
        public const string APP_NAME = "GameDeploy Kiosk";
        public const string CONFIG_FOLDER_NAME = "GameCopier";
        
        // File names
        public const string GAME_FOLDERS_CONFIG_FILE = "gamefolders.json";
        public const string SOFTWARE_FOLDERS_CONFIG_FILE = "softwarefolders.json";
        public const string DRIVE_SETTINGS_CONFIG_FILE = "drivesettings.json";
        
        // Default folders
        public const string DEFAULT_GAME_LIBRARY_PATH = "C:\\GameLibrary";
        
        // Timer intervals (in milliseconds)
        public const int DRIVE_MONITOR_INTERVAL = 30000; // 30 seconds
        public const int USB_DETECTION_INTERVAL = 1000;  // 1 second
        
        // UI delays
        public const int STATUS_RESET_DELAY = 3000;     // 3 seconds
        public const int AUTO_START_DELAY = 500;        // 0.5 seconds
        public const int INITIALIZATION_DELAY = 2000;   // 2 seconds
        
        // Copy operation settings
        public const int COPY_DIALOG_DELAY = 750;       // 0.75 seconds
        public const int TEST_OPERATIONS_COUNT = 3;
        
        // Demo drive settings
        public const long DEMO_DRIVE_SIZE_GB = 32L * 1024 * 1024 * 1024;
        public const long DEMO_DRIVE_FREE_GB = 30L * 1024 * 1024 * 1024;
        public const string DEMO_DRIVE_LETTER = "X:";
        public const string DEMO_DRIVE_LABEL = "TEST_USB";
        public const string DEMO_DRIVE_FILE_SYSTEM = "NTFS";
    }
}
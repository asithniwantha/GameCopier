namespace GameCopier.Core.Constants
{
    /// <summary>
    /// File and path related constants
    /// </summary>
    public static class FileConstants
    {
        // File extensions
        public const string JSON_EXTENSION = ".json";
        public const string TEXT_EXTENSION = ".txt";
        public const string LOG_EXTENSION = ".log";
        
        // System folders
        public const string SYSTEM_DRIVE_PREFIX = "C:";
        public const string TEMP_FOLDER_PREFIX = "GameCopier";
        
        // Test file names
        public const string TEST_FILE_NAME = "test.txt";
        public const string TEST_FOLDER_BASE = "GameCopierTest";
        public const string TEST_FOLDER_TARGET = "GameCopierTestTarget";
        public const string TEST_SEQUENTIAL_BASE = "GameCopierSequentialTest";
        
        // File size units
        public static readonly string[] SIZE_SUFFIXES = { "B", "KB", "MB", "GB", "TB" };
        public const int SIZE_UNIT_THRESHOLD = 1024;
        
        // Search patterns
        public const string ALL_FILES_PATTERN = "*";
        public const string RECURSIVE_SEARCH = "*.*";
        
        // USB drive detection patterns
        public static readonly string[] USB_PREFIXES = { "USB ", "USB" };
        public static readonly string[] USB_SUFFIXES = { " USB DEVICE", " USB" };
        public static readonly string[] EXCLUDED_LABELS = { "NEW VOLUME", "USB Drive" };
        
        // Drive brands for detection
        public static readonly string[] KNOWN_USB_BRANDS = {
            "SanDisk", "Kingston", "Samsung", "Lexar", "PNY",
            "Corsair", "Transcend", "Verbatim", "Toshiba", "Sony", "ADATA",
            "Seagate", "Western Digital", "WD", "Crucial", "Patriot", "Micron"
        };
    }
}
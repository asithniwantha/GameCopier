namespace GameCopier.Services
{
    public enum CopyMethod
    {
        ExplorerDialog,    // Windows Explorer copy with progress dialog (default)
        ExplorerSilent,    // Windows Explorer copy without progress dialog  
        Robocopy,          // Use robocopy command line tool
        Xcopy              // Use xcopy command line tool
    }
}
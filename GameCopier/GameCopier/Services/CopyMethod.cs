using System;

namespace GameCopier.Services
{
    public enum CopyMethod
    {
        ExplorerDialog, // Windows Explorer copy with progress dialog (default)
        ExplorerSilent, // Windows Explorer copy without dialog
        Robocopy,       // Windows Robocopy utility (fallback)
        Xcopy          // Windows Xcopy utility (final fallback)
    }
}
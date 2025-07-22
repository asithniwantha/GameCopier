using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameCopier.Services.Business
{
    public class FastCopyService
    {
        // Windows Shell API for Explorer-style copy operations
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        // Modern COM-based file operations for better dialog control
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            IntPtr pbc, ref Guid riid, out IntPtr shellItem);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string? lpszProgressTitle;
        }

        // File operation constants
        private const uint FO_COPY = 0x0002;

        // Enhanced flag constants for better dialog control
        private const ushort FOF_ALLOWUNDO = 0x0040;           // Allow undo
        private const ushort FOF_FILESONLY = 0x0080;           // Copy files only
        private const ushort FOF_NOCONFIRMATION = 0x0010;      // No confirmation dialogs for overwrites
        private const ushort FOF_NOCONFIRMMKDIR = 0x0200;      // No confirmation for creating directories
        private const ushort FOF_RENAMEONCOLLISION = 0x0008;   // Rename on collision
        private const ushort FOF_SILENT = 0x0004;              // Don't show progress dialog
        private const ushort FOF_NOERRORUI = 0x0400;           // Don't show error dialogs
        private const ushort FOF_WANTMAPPINGHANDLE = 0x0020;   // Want mapping handle for progress
        private const ushort FOF_SIMPLEPROGRESS = 0x0100;      // Simple progress dialog

        /// <summary>
        /// Copy directory using Windows Explorer's enhanced copy behavior with reliable progress dialog
        /// </summary>
        public static async Task<bool> CopyDirectoryWithExplorerDialogAsync(
            string sourceDir,
            string targetDir,
            IntPtr parentWindow = default,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Debug.WriteLine($"🚀 Starting Enhanced Explorer copy: {sourceDir} -> {targetDir}");

                    // Verify source directory exists
                    if (!Directory.Exists(sourceDir))
                    {
                        Debug.WriteLine($"❌ Source directory does not exist: {sourceDir}");
                        return false;
                    }

                    // Ensure target parent directory exists
                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                    {
                        Directory.CreateDirectory(targetParent);
                        Debug.WriteLine($"📁 Created target parent directory: {targetParent}");
                    }

                    // Format source for SHFileOperation (must end with double null)
                    var source = sourceDir.TrimEnd('\\') + "\\*.*\0\0";
                    var target = targetDir.TrimEnd('\\') + "\0\0";

                    var fileOp = new SHFILEOPSTRUCT
                    {
                        hwnd = parentWindow,
                        wFunc = FO_COPY,
                        pFrom = source,
                        pTo = target,
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR, // Show progress dialog with undo capability
                        lpszProgressTitle = "Copying Game Files..."
                    };

                    Debug.WriteLine($"🔧 Copy flags: {fileOp.fFlags} (Dialog enabled, Undo allowed)");

                    int result = SHFileOperation(ref fileOp);

                    if (result == 0 && !fileOp.fAnyOperationsAborted)
                    {
                        Debug.WriteLine($"✅ Enhanced Explorer copy completed successfully");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"❌ Enhanced Explorer copy failed - Result: {result}, Aborted: {fileOp.fAnyOperationsAborted}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Enhanced Explorer copy exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Copy directory silently using Windows Explorer (no progress dialog)
        /// </summary>
        public static async Task<bool> CopyDirectoryWithExplorerSilentAsync(
            string sourceDir,
            string targetDir,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Debug.WriteLine($"🔇 Starting Silent Explorer copy: {sourceDir} -> {targetDir}");

                    if (!Directory.Exists(sourceDir))
                    {
                        Debug.WriteLine($"❌ Source directory does not exist: {sourceDir}");
                        return false;
                    }

                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                    {
                        Directory.CreateDirectory(targetParent);
                    }

                    var source = sourceDir.TrimEnd('\\') + "\\*.*\0\0";
                    var target = targetDir.TrimEnd('\\') + "\0\0";

                    var fileOp = new SHFILEOPSTRUCT
                    {
                        hwnd = IntPtr.Zero,
                        wFunc = FO_COPY,
                        pFrom = source,
                        pTo = target,
                        fFlags = FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR | FOF_NOERRORUI, // Silent operation
                        lpszProgressTitle = null
                    };

                    int result = SHFileOperation(ref fileOp);

                    if (result == 0 && !fileOp.fAnyOperationsAborted)
                    {
                        Debug.WriteLine($"✅ Silent Explorer copy completed successfully");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"❌ Silent Explorer copy failed - Result: {result}, Aborted: {fileOp.fAnyOperationsAborted}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Silent Explorer copy exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Enhanced copy method with notification callbacks and forced dialog display
        /// </summary>
        public static async Task<bool> CopyDirectoryWithDialogNotificationAsync(
            string sourceDir,
            string targetDir,
            IntPtr parentWindow,
            Action<string> statusCallback,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    statusCallback?.Invoke("Preparing copy operation...");
                    Debug.WriteLine($"📋 Starting Copy with Dialog Notification: {sourceDir} -> {targetDir}");

                    if (!Directory.Exists(sourceDir))
                    {
                        statusCallback?.Invoke("Error: Source directory not found");
                        return false;
                    }

                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                    {
                        Directory.CreateDirectory(targetParent);
                        statusCallback?.Invoke("Created target directory");
                    }

                    var source = sourceDir.TrimEnd('\\') + "\\*.*\0\0";
                    var target = targetDir.TrimEnd('\\') + "\0\0";

                    statusCallback?.Invoke("Starting Windows Explorer copy dialog...");

                    var fileOp = new SHFILEOPSTRUCT
                    {
                        hwnd = parentWindow,
                        wFunc = FO_COPY,
                        pFrom = source,
                        pTo = target,
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR, // Ensure dialog is shown
                        lpszProgressTitle = "GameCopier - Copying Files"
                    };

                    int result = SHFileOperation(ref fileOp);

                    if (result == 0 && !fileOp.fAnyOperationsAborted)
                    {
                        statusCallback?.Invoke("Copy completed successfully");
                        Debug.WriteLine($"✅ Copy with Dialog Notification completed successfully");
                        return true;
                    }
                    else
                    {
                        statusCallback?.Invoke($"Copy failed (Code: {result})");
                        Debug.WriteLine($"❌ Copy with Dialog Notification failed - Result: {result}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"Copy error: {ex.Message}");
                    Debug.WriteLine($"❌ Copy with Dialog Notification exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Enhanced copy method with forced dialog display and improved reliability
        /// </summary>
        public static async Task<bool> CopyDirectoryWithForcedDialogAsync(
            string sourceDir,
            string targetDir,
            IntPtr parentWindow,
            Action<string> statusCallback,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    statusCallback?.Invoke("Initializing enhanced copy operation...");
                    Debug.WriteLine($"🎯 Starting Forced Dialog Copy: {sourceDir} -> {targetDir}");

                    if (!Directory.Exists(sourceDir))
                    {
                        statusCallback?.Invoke("❌ Source directory not found");
                        return false;
                    }

                    // Ensure target directory structure exists
                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent))
                    {
                        Directory.CreateDirectory(targetParent);
                        statusCallback?.Invoke("📁 Target directory prepared");
                    }

                    // Strategic delay to ensure Windows Shell API is ready
                    Thread.Sleep(200);

                    var source = sourceDir.TrimEnd('\\') + "\\*.*\0\0";
                    var target = targetDir.TrimEnd('\\') + "\0\0";

                    statusCallback?.Invoke("🚀 Launching Windows copy dialog...");

                    var fileOp = new SHFILEOPSTRUCT
                    {
                        hwnd = parentWindow,
                        wFunc = FO_COPY,
                        pFrom = source,
                        pTo = target,
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR | FOF_WANTMAPPINGHANDLE, // Force dialog with progress tracking
                        lpszProgressTitle = "GameCopier - File Transfer"
                    };

                    Debug.WriteLine($"🔧 Forced dialog flags: {fileOp.fFlags}");

                    int result = SHFileOperation(ref fileOp);

                    if (result == 0 && !fileOp.fAnyOperationsAborted)
                    {
                        statusCallback?.Invoke("✅ Transfer completed successfully");
                        Debug.WriteLine($"✅ Forced Dialog Copy completed successfully");
                        return true;
                    }
                    else
                    {
                        statusCallback?.Invoke($"❌ Transfer failed (Error code: {result})");
                        Debug.WriteLine($"❌ Forced Dialog Copy failed - Result: {result}, Aborted: {fileOp.fAnyOperationsAborted}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"❌ Transfer error: {ex.Message}");
                    Debug.WriteLine($"❌ Forced Dialog Copy exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }
    }
}
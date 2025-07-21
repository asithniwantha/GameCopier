using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameCopier.Services
{
    public class FastCopyService
    {
        // Windows Shell API for Explorer-style copy operations
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

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
            public string lpszProgressTitle;
        }

        // File operation constants
        private const uint FO_COPY = 0x0002;
        
        // Flag constants for Windows Explorer copy behavior
        private const ushort FOF_ALLOWUNDO = 0x0040;           // Allow undo
        private const ushort FOF_FILESONLY = 0x0080;           // Copy files only
        private const ushort FOF_NOCONFIRMATION = 0x0010;      // No confirmation dialogs for overwrites
        private const ushort FOF_NOCONFIRMMKDIR = 0x0200;      // No confirmation for creating directories
        private const ushort FOF_RENAMEONCOLLISION = 0x0008;   // Rename on collision
        private const ushort FOF_SILENT = 0x0004;              // Don't show progress dialog
        private const ushort FOF_NOERRORUI = 0x0400;           // Don't show error dialogs

        /// <summary>
        /// Copy directory using Windows Explorer's default copy behavior with native progress dialog
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
                    Debug.WriteLine($"Starting Explorer copy: {sourceDir} -> {targetDir}");
                    
                    // Verify source directory exists
                    if (!Directory.Exists(sourceDir))
                    {
                        Debug.WriteLine($"Source directory does not exist: {sourceDir}");
                        return false;
                    }
                    
                    // Ensure target directory exists
                    Directory.CreateDirectory(targetDir);

                    // FIXED: Prepare paths correctly for SHFileOperation
                    // Source: use \\* to copy contents, double null-terminated
                    var sourcePath = sourceDir.TrimEnd('\\') + "\\*\0";
                    // Target: just the directory path, double null-terminated
                    var targetPath = targetDir.TrimEnd('\\') + "\0";

                    Debug.WriteLine($"SHFileOperation paths: FROM='{sourcePath.Replace("\0", "\\0")}' TO='{targetPath.Replace("\0", "\\0")}'");

                    var shf = new SHFILEOPSTRUCT
                    {
                        hwnd = parentWindow,
                        wFunc = FO_COPY,
                        pFrom = sourcePath,
                        pTo = targetPath,
                        // Use Windows Explorer default behavior - shows progress dialog
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR,
                        fAnyOperationsAborted = false,
                        hNameMappings = IntPtr.Zero,
                        lpszProgressTitle = null
                    };

                    Debug.WriteLine("Calling SHFileOperation with Explorer progress dialog...");
                    int result = SHFileOperation(ref shf);
                    
                    bool success = result == 0 && !shf.fAnyOperationsAborted;
                    
                    if (success)
                    {
                        Debug.WriteLine("Explorer copy completed successfully");
                    }
                    else
                    {
                        Debug.WriteLine($"Explorer copy failed: Result={result} (0x{result:X}), Aborted={shf.fAnyOperationsAborted}");
                        // Log common error codes
                        switch (result)
                        {
                            case 2: Debug.WriteLine("Error: File not found"); break;
                            case 3: Debug.WriteLine("Error: Path not found"); break;
                            case 5: Debug.WriteLine("Error: Access denied"); break;
                            case 26: Debug.WriteLine("Error: Not ready"); break;
                            case 183: Debug.WriteLine("Error: File already exists"); break;
                            default: Debug.WriteLine($"Error: Unknown error code {result}"); break;
                        }
                    }
                    
                    return success;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Explorer copy failed with exception: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Copy directory using Windows Explorer's silent copy (no dialog) for background operations
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
                    Debug.WriteLine($"Starting Explorer silent copy: {sourceDir} -> {targetDir}");
                    
                    // Verify source directory exists
                    if (!Directory.Exists(sourceDir))
                    {
                        Debug.WriteLine($"Source directory does not exist: {sourceDir}");
                        return false;
                    }
                    
                    // Ensure target directory exists
                    Directory.CreateDirectory(targetDir);

                    // FIXED: Prepare paths correctly for SHFileOperation
                    var sourcePath = sourceDir.TrimEnd('\\') + "\\*\0";
                    var targetPath = targetDir.TrimEnd('\\') + "\0";

                    var shf = new SHFILEOPSTRUCT
                    {
                        hwnd = IntPtr.Zero,
                        wFunc = FO_COPY,
                        pFrom = sourcePath,
                        pTo = targetPath,
                        // Silent operation with no dialogs
                        fFlags = FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR,
                        fAnyOperationsAborted = false,
                        hNameMappings = IntPtr.Zero,
                        lpszProgressTitle = null
                    };

                    Debug.WriteLine("Calling SHFileOperation in silent mode...");
                    int result = SHFileOperation(ref shf);
                    
                    bool success = result == 0 && !shf.fAnyOperationsAborted;
                    
                    if (success)
                    {
                        Debug.WriteLine("Explorer silent copy completed successfully");
                    }
                    else
                    {
                        Debug.WriteLine($"Explorer silent copy failed: Result={result} (0x{result:X}), Aborted={shf.fAnyOperationsAborted}");
                    }
                    
                    return success;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Explorer silent copy failed with exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Legacy method - kept for compatibility but now uses Explorer copy
        /// </summary>
        [Obsolete("Use CopyDirectoryWithExplorerDialogAsync instead")]
        public static bool CopyDirectoryWithShellAPI(string sourceDir, string targetDir)
        {
            var task = CopyDirectoryWithExplorerSilentAsync(sourceDir, targetDir);
            task.Wait();
            return task.Result;
        }
    }
}
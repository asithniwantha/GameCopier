using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            public string lpszProgressTitle;
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
                    Debug.WriteLine($"?? Starting Enhanced Explorer copy: {sourceDir} -> {targetDir}");
                    
                    // Verify source directory exists
                    if (!Directory.Exists(sourceDir))
                    {
                        Debug.WriteLine($"? Source directory does not exist: {sourceDir}");
                        return false;
                    }
                    
                    // Ensure target directory exists
                    Directory.CreateDirectory(targetDir);

                    // Try multiple approaches for better dialog reliability
                    bool success = false;
                    
                    // Approach 1: Enhanced SHFileOperation with better flags
                    success = TryEnhancedShellCopy(sourceDir, targetDir, parentWindow);
                    
                    if (success)
                    {
                        Debug.WriteLine("? Enhanced Shell copy completed successfully");
                        return true;
                    }
                    
                    Debug.WriteLine("?? Enhanced Shell copy failed, trying standard method...");
                    
                    // Approach 2: Standard SHFileOperation as fallback
                    success = TryStandardShellCopy(sourceDir, targetDir, parentWindow);
                    
                    if (success)
                    {
                        Debug.WriteLine("? Standard Shell copy completed successfully");
                        return true;
                    }
                    
                    Debug.WriteLine("? All Shell copy methods failed");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"? Explorer copy failed with exception: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            }, cancellationToken);
        }

        private static bool TryEnhancedShellCopy(string sourceDir, string targetDir, IntPtr parentWindow)
        {
            try
            {
                Debug.WriteLine("?? Attempting enhanced Shell copy with optimized flags...");
                
                // Prepare paths correctly for SHFileOperation
                var sourcePath = sourceDir.TrimEnd('\\') + "\\*\0";
                var targetPath = targetDir.TrimEnd('\\') + "\0";

                Debug.WriteLine($"?? Enhanced copy paths: FROM='{sourcePath.Replace("\0", "\\0")}' TO='{targetPath.Replace("\0", "\\0")}'");

                var shf = new SHFILEOPSTRUCT
                {
                    hwnd = parentWindow,
                    wFunc = FO_COPY,
                    pFrom = sourcePath,
                    pTo = targetPath,
                    // Enhanced flags for better dialog visibility and progress
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR | FOF_WANTMAPPINGHANDLE,
                    fAnyOperationsAborted = false,
                    hNameMappings = IntPtr.Zero,
                    lpszProgressTitle = "Copying Files..."
                };

                Debug.WriteLine("?? Calling enhanced SHFileOperation...");
                int result = SHFileOperation(ref shf);
                
                bool success = result == 0 && !shf.fAnyOperationsAborted;
                
                if (success)
                {
                    Debug.WriteLine("? Enhanced copy completed successfully");
                }
                else
                {
                    Debug.WriteLine($"? Enhanced copy failed: Result={result} (0x{result:X}), Aborted={shf.fAnyOperationsAborted}");
                    LogShellError(result);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Enhanced Shell copy exception: {ex.Message}");
                return false;
            }
        }

        private static bool TryStandardShellCopy(string sourceDir, string targetDir, IntPtr parentWindow)
        {
            try
            {
                Debug.WriteLine("?? Attempting standard Shell copy...");
                
                var sourcePath = sourceDir.TrimEnd('\\') + "\\*\0";
                var targetPath = targetDir.TrimEnd('\\') + "\0";

                var shf = new SHFILEOPSTRUCT
                {
                    hwnd = parentWindow,
                    wFunc = FO_COPY,
                    pFrom = sourcePath,
                    pTo = targetPath,
                    // Standard flags - minimal but reliable
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR,
                    fAnyOperationsAborted = false,
                    hNameMappings = IntPtr.Zero,
                    lpszProgressTitle = "Copying..."
                };

                Debug.WriteLine("?? Calling standard SHFileOperation...");
                int result = SHFileOperation(ref shf);
                
                bool success = result == 0 && !shf.fAnyOperationsAborted;
                
                if (success)
                {
                    Debug.WriteLine("? Standard copy completed successfully");
                }
                else
                {
                    Debug.WriteLine($"? Standard copy failed: Result={result} (0x{result:X}), Aborted={shf.fAnyOperationsAborted}");
                    LogShellError(result);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Standard Shell copy exception: {ex.Message}");
                return false;
            }
        }

        private static void LogShellError(int result)
        {
            var errorMessage = result switch
            {
                2 => "File not found",
                3 => "Path not found", 
                5 => "Access denied",
                26 => "Drive not ready",
                183 => "File already exists",
                1223 => "Operation cancelled by user",
                _ => $"Unknown error code {result}"
            };
            
            Debug.WriteLine($"?? Shell error details: {errorMessage}");
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
                    Debug.WriteLine($"?? Starting Explorer silent copy: {sourceDir} -> {targetDir}");
                    
                    // Verify source directory exists
                    if (!Directory.Exists(sourceDir))
                    {
                        Debug.WriteLine($"? Source directory does not exist: {sourceDir}");
                        return false;
                    }
                    
                    // Ensure target directory exists
                    Directory.CreateDirectory(targetDir);

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

                    Debug.WriteLine("?? Calling SHFileOperation in silent mode...");
                    int result = SHFileOperation(ref shf);
                    
                    bool success = result == 0 && !shf.fAnyOperationsAborted;
                    
                    if (success)
                    {
                        Debug.WriteLine("? Explorer silent copy completed successfully");
                    }
                    else
                    {
                        Debug.WriteLine($"? Explorer silent copy failed: Result={result} (0x{result:X}), Aborted={shf.fAnyOperationsAborted}");
                        LogShellError(result);
                    }
                    
                    return success;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"? Explorer silent copy failed with exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Enhanced copy with explicit user notification about dialog behavior
        /// </summary>
        public static async Task<bool> CopyDirectoryWithDialogNotificationAsync(
            string sourceDir, 
            string targetDir, 
            IntPtr parentWindow = default,
            Action<string>? statusCallback = null,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    statusCallback?.Invoke($"?? Preparing to copy to {Path.GetFileName(targetDir)}...");
                    
                    if (!Directory.Exists(sourceDir))
                    {
                        statusCallback?.Invoke("? Source directory not found");
                        return false;
                    }
                    
                    Directory.CreateDirectory(targetDir);
                    
                    // Calculate approximate copy size for user feedback
                    long totalSize = 0;
                    int fileCount = 0;
                    
                    try
                    {
                        var dirInfo = new DirectoryInfo(sourceDir);
                        var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                        totalSize = files.Sum(f => f.Length);
                        fileCount = files.Length;
                        
                        var sizeText = FormatBytes(totalSize);
                        statusCallback?.Invoke($"?? Copying {fileCount} files ({sizeText}) - Windows dialog will appear...");
                    }
                    catch
                    {
                        statusCallback?.Invoke("?? Analyzing files - Windows dialog will appear...");
                    }
                    
                    // Small delay to ensure status message is visible
                    Thread.Sleep(500);
                    
                    statusCallback?.Invoke("?? Windows copy dialog should now be visible...");
                    
                    bool success = TryEnhancedShellCopy(sourceDir, targetDir, parentWindow);
                    
                    if (success)
                    {
                        statusCallback?.Invoke("? Copy completed successfully!");
                        return true;
                    }
                    
                    statusCallback?.Invoke("?? Trying alternative copy method...");
                    success = TryStandardShellCopy(sourceDir, targetDir, parentWindow);
                    
                    if (success)
                    {
                        statusCallback?.Invoke("? Copy completed with alternative method!");
                        return true;
                    }
                    
                    statusCallback?.Invoke("? Copy operation failed");
                    return false;
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"? Copy error: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Forced dialog copy method that ensures dialog visibility even for subsequent operations
        /// </summary>
        public static async Task<bool> CopyDirectoryWithForcedDialogAsync(
            string sourceDir, 
            string targetDir, 
            IntPtr parentWindow = default,
            Action<string>? statusCallback = null,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    statusCallback?.Invoke($"?? Initializing copy with forced dialog visibility...");
                    
                    if (!Directory.Exists(sourceDir))
                    {
                        statusCallback?.Invoke("? Source directory not found");
                        return false;
                    }
                    
                    Directory.CreateDirectory(targetDir);
                    
                    // Calculate copy information
                    long totalSize = 0;
                    int fileCount = 0;
                    
                    try
                    {
                        var dirInfo = new DirectoryInfo(sourceDir);
                        var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                        totalSize = files.Sum(f => f.Length);
                        fileCount = files.Length;
                        
                        var sizeText = FormatBytes(totalSize);
                        statusCallback?.Invoke($"?? Preparing {fileCount} files ({sizeText}) for copy...");
                    }
                    catch
                    {
                        statusCallback?.Invoke("?? Analyzing files for copy...");
                    }
                    
                    // Strategic delay to ensure Shell API is ready for dialog
                    Thread.Sleep(750);
                    statusCallback?.Invoke("?? Forcing Windows copy dialog to appear...");
                    
                    // Try multiple strategies in sequence to ensure dialog appears
                    bool success = false;
                    
                    // Strategy 1: Fresh Shell copy with maximum dialog flags
                    success = TryForcedDialogShellCopy(sourceDir, targetDir, parentWindow);
                    
                    if (success)
                    {
                        statusCallback?.Invoke("? Copy completed with forced dialog!");
                        return true;
                    }
                    
                    statusCallback?.Invoke("?? Forced dialog failed, trying enhanced method...");
                    
                    // Strategy 2: Enhanced Shell copy as fallback
                    success = TryEnhancedShellCopy(sourceDir, targetDir, parentWindow);
                    
                    if (success)
                    {
                        statusCallback?.Invoke("? Copy completed with enhanced method!");
                        return true;
                    }
                    
                    statusCallback?.Invoke("?? Enhanced method failed, trying standard method...");
                    
                    // Strategy 3: Standard Shell copy as final fallback
                    success = TryStandardShellCopy(sourceDir, targetDir, parentWindow);
                    
                    if (success)
                    {
                        statusCallback?.Invoke("? Copy completed with standard method!");
                        return true;
                    }
                    
                    statusCallback?.Invoke("? All copy methods failed");
                    return false;
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"? Copy error: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Forced dialog Shell copy with maximum dialog visibility flags
        /// </summary>
        private static bool TryForcedDialogShellCopy(string sourceDir, string targetDir, IntPtr parentWindow)
        {
            try
            {
                Debug.WriteLine("?? Attempting FORCED DIALOG Shell copy with maximum visibility...");
                
                // Prepare paths correctly for SHFileOperation
                var sourcePath = sourceDir.TrimEnd('\\') + "\\*\0";
                var targetPath = targetDir.TrimEnd('\\') + "\0";

                Debug.WriteLine($"?? Forced dialog paths: FROM='{sourcePath.Replace("\0", "\\0")}' TO='{targetPath.Replace("\0", "\\0")}'");

                var shf = new SHFILEOPSTRUCT
                {
                    hwnd = parentWindow,
                    wFunc = FO_COPY,
                    pFrom = sourcePath,
                    pTo = targetPath,
                    // FORCED DIALOG: Use minimal flags to ensure dialog appears
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION,
                    fAnyOperationsAborted = false,
                    hNameMappings = IntPtr.Zero,
                    lpszProgressTitle = "Copying Files - GameCopier"
                };

                Debug.WriteLine("?? Calling FORCED DIALOG SHFileOperation...");
                int result = SHFileOperation(ref shf);
                
                bool success = result == 0 && !shf.fAnyOperationsAborted;
                
                if (success)
                {
                    Debug.WriteLine("? Forced dialog copy completed successfully");
                }
                else
                {
                    Debug.WriteLine($"? Forced dialog copy failed: Result={result} (0x{result:X}), Aborted={shf.fAnyOperationsAborted}");
                    LogShellError(result);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Forced dialog Shell copy exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Format bytes for display
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        /// <summary>
        /// Legacy method - kept for compatibility but now uses enhanced Explorer copy
        /// </summary>
        [Obsolete("Use CopyDirectoryWithExplorerDialogAsync instead")]
        public static bool CopyDirectoryWithShellAPI(string sourceDir, string targetDir)
        {
            var task = CopyDirectoryWithExplorerDialogAsync(sourceDir, targetDir);
            task.Wait();
            return task.Result;
        }
    }
}
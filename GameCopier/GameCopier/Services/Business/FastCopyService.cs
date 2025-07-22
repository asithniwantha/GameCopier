using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameCopier.Services.Business
{
    /// <summary>
    /// FastCopyService provides multiple copy methods with progress tracking and dialog support.
    /// Includes modern IFileOperation COM interfaces and legacy SHFileOperation methods.
    /// </summary>
    public class FastCopyService
    {
        #region Windows API Declarations

        // Legacy Windows Shell API for Explorer-style copy operations
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

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
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_NOCONFIRMMKDIR = 0x0200;
        private const ushort FOF_RENAMEONCOLLISION = 0x0008;
        private const ushort FOF_SILENT = 0x0004;
        private const ushort FOF_NOERRORUI = 0x0400;
        private const ushort FOF_SIMPLEPROGRESS = 0x0100;
        private const ushort FOF_WANTMAPPINGHANDLE = 0x0020;

        // Modern IFileOperation flags
        private const uint FOF_NO_UI = 0x04;
        private const uint FOF_ALLOWUNDO_FLAG = 0x40;
        private const uint FOF_NOERRORUI_FLAG = 0x400;
        private const uint FOF_NOCONFIRMATION_FLAG = 0x10;

        #endregion

        #region Modern IFileOperation COM Interface

        [ComImport, Guid("3AD05575-8857-4850-9277-11B85BDB8E09")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IFileOperation
        {
            void Advise(IFileOperationProgressSink pfops, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOperationFlags(uint dwOperationFlags);
            void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
            void SetProgressDialog(IOperationsProgressDialog popd);
            void SetProperties(IntPtr pproparray);
            void SetOwnerWindow(IntPtr hwndOwner);
            void ApplyPropertiesToItem(IntPtr psiItem);
            void ApplyPropertiesToItems(IntPtr punkItems);
            void RenameItem(IntPtr psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink pfopsItem);
            void RenameItems(IntPtr punkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void MoveItem(IntPtr psiItem, IntPtr psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink pfopsItem);
            void MoveItems(IntPtr punkItems, IntPtr psiDestinationFolder);
            void CopyItem(IntPtr psiItem, IntPtr psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszCopyName, IFileOperationProgressSink pfopsItem);
            void CopyItems(IntPtr punkItems, IntPtr psiDestinationFolder);
            void DeleteItem(IntPtr psiItem, IFileOperationProgressSink pfopsItem);
            void DeleteItems(IntPtr punkItems);
            void NewItem(IntPtr psiDestinationFolder, uint dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, IFileOperationProgressSink pfopsItem);
            void PerformOperations();
            void GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool pfAnyOperationsAborted);
        }

        [ComImport, Guid("0C9FB851-E5C9-43EB-A370-F0677B13874C")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IFileOperationProgressSink
        {
            void StartOperations();
            void FinishOperations(int hrResult);
            void PreRenameItem(uint dwFlags, IntPtr psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void PostRenameItem(uint dwFlags, IntPtr psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrRename, IntPtr psiNewlyCreated);
            void PreMoveItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void PostMoveItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrMove, IntPtr psiNewlyCreated);
            void PreCopyItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void PostCopyItem(uint dwFlags, IntPtr psiItem, IntPtr psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrCopy, IntPtr psiNewlyCreated);
            void PreDeleteItem(uint dwFlags, IntPtr psiItem);
            void PostDeleteItem(uint dwFlags, IntPtr psiItem, int hrDelete, IntPtr psiNewlyCreated);
            void PreNewItem(uint dwFlags, IntPtr psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void PostNewItem(uint dwFlags, IntPtr psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, uint dwFileAttributes, int hrNew, IntPtr psiNewItem);
            void UpdateProgress(uint iWorkTotal, uint iWorkSoFar);
            void ResetTimer();
            void PauseTimer();
            void ResumeTimer();
        }

        [ComImport, Guid("0947AAB5-0AA1-44E2-88E0-83AFCFC85CD3")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOperationsProgressDialog
        {
            void StartProgressDialog(IntPtr hwndOwner, uint flags);
            void StopProgressDialog();
            void SetOperation(uint action);
            void SetMode(uint mode);
            void UpdateProgress(ulong ullPointsCurrent, ulong ullPointsTotal, ulong ullSizesCurrent, ulong ullSizesTotal, ulong ullItemsCurrent, ulong ullItemsTotal);
            void UpdateLocations(IntPtr psiSource, IntPtr psiTarget, IntPtr psiItem);
            void ResetTimer();
            void PauseTimer();
            void ResumeTimer();
            void GetMilliseconds(out ulong pullElapsed, out ulong pullRemaining);
            void GetOperationStatus(out uint popstatus);
        }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport, Guid("3AD05575-8857-4850-9277-11B85BDB8E09")]
        public class FileOperationClass { }

        [ComImport, Guid("0947AAB5-0AA1-44E2-88E0-83AFCFC85CD3")]
        public class ProgressDialogClass { }

        #endregion

        #region Primary Copy Methods

        /// <summary>
        /// Enhanced copy method with both visible progress window AND UI progress tracking.
        /// This is the recommended method for GameCopier operations.
        /// FUTURE IMPROVEMENT: This method will be enhanced for parallel multi-drive operations.
        /// </summary>
        public static async Task<bool> CopyDirectoryWithDualProgressAsync(
            string sourceDir,
            string targetDir,
            IntPtr parentWindow,
            Action<string>? statusCallback,
            Action<double>? progressCallback,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    statusCallback?.Invoke("🚀 Initializing dual progress copy operation...");
                    Debug.WriteLine($"🎯 Starting Dual Progress Copy: {sourceDir} -> {targetDir}");

                    if (!Directory.Exists(sourceDir))
                    {
                        statusCallback?.Invoke("❌ Source directory not found");
                        return false;
                    }

                    // Count total files for progress calculation
                    statusCallback?.Invoke("📊 Analyzing source files...");
                    int totalFiles = CountFilesRecursively(sourceDir);
                    long totalSize = GetDirectorySize(sourceDir);
                    statusCallback?.Invoke($"📊 Found {totalFiles} files ({FormatBytes(totalSize)}) to copy");

                    // Ensure target directory structure exists
                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent))
                    {
                        Directory.CreateDirectory(targetParent);
                        statusCallback?.Invoke("📁 Target directory prepared");
                    }

                    statusCallback?.Invoke("🚀 Starting dual progress copy operation...");

                    // Start robocopy with visible window AND capture progress
                    var visibleStartInfo = new ProcessStartInfo
                    {
                        FileName = "robocopy",
                        Arguments = $"\"{sourceDir}\" \"{targetDir}\" /E /IS /IT /IM /XJ /R:3 /W:1",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal,
                        CreateNoWindow = false
                    };

                    using var visibleProcess = Process.Start(visibleStartInfo);

                    // Monitor progress by checking target directory size
                    var progressTask = Task.Run(async () =>
                    {
                        long lastSize = 0;
                        int stableCount = 0;

                        while (visibleProcess != null && !visibleProcess.HasExited)
                        {
                            try
                            {
                                if (Directory.Exists(targetDir))
                                {
                                    long currentSize = GetDirectorySize(targetDir);
                                    double progress = totalSize > 0 ? (double)currentSize / totalSize * 100.0 : 0;
                                    progress = Math.Min(progress, 99);
                                    
                                    progressCallback?.Invoke(progress);
                                    statusCallback?.Invoke($"📊 Progress: {FormatBytes(currentSize)} / {FormatBytes(totalSize)} ({progress:F1}%)");

                                    if (currentSize == lastSize)
                                    {
                                        stableCount++;
                                    }
                                    else
                                    {
                                        stableCount = 0;
                                        lastSize = currentSize;
                                    }

                                    if (stableCount > 10 && currentSize > 0)
                                        break;
                                }

                                await Task.Delay(1000);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"⚠️ Progress monitoring error: {ex.Message}");
                                await Task.Delay(2000);
                            }
                        }
                    });

                    if (visibleProcess != null)
                    {
                        visibleProcess.WaitForExit();
                        await progressTask;

                        bool success = visibleProcess.ExitCode <= 7;
                        
                        if (success)
                        {
                            progressCallback?.Invoke(100);
                            statusCallback?.Invoke("✅ Dual progress copy completed successfully");
                            return true;
                        }
                        else
                        {
                            statusCallback?.Invoke($"❌ Robocopy failed with exit code {visibleProcess.ExitCode}");
                            return false;
                        }
                    }
                    else
                    {
                        statusCallback?.Invoke("❌ Failed to start robocopy process");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"❌ Dual progress copy error: {ex.Message}");
                    Debug.WriteLine($"❌ CopyDirectoryWithDualProgressAsync: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Enhanced copy method with enhanced reliability and fallback mechanisms.
        /// FUTURE IMPROVEMENT: Will be replaced by intelligent parallel copy manager.
        /// </summary>
        public static async Task<bool> CopyDirectoryWithEnhancedReliabilityAsync(
            string sourceDir,
            string targetDir,
            IntPtr parentWindow,
            Action<string>? statusCallback,
            CancellationToken cancellationToken = default)
        {
            // Use the dual progress method as the primary approach
            return await CopyDirectoryWithDualProgressAsync(
                sourceDir,
                targetDir,
                parentWindow,
                statusCallback,
                progress => statusCallback?.Invoke($"📊 Progress: {progress:F1}%"),
                cancellationToken);
        }

        #endregion

        #region Legacy Methods (Preserved for Compatibility)

        /// <summary>
        /// Copy directory using Windows Explorer with progress dialog.
        /// LEGACY METHOD: Kept for compatibility, will be phased out in v2.0.
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
                    Debug.WriteLine($"🚀 Starting Explorer copy: {sourceDir} -> {targetDir}");

                    if (!Directory.Exists(sourceDir))
                        return false;

                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                        Directory.CreateDirectory(targetParent);

                    var source = sourceDir.TrimEnd('\\') + "\\*.*\0\0";
                    var target = targetDir.TrimEnd('\\') + "\0\0";

                    var fileOp = new SHFILEOPSTRUCT
                    {
                        hwnd = parentWindow,
                        wFunc = FO_COPY,
                        pFrom = source,
                        pTo = target,
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR,
                        lpszProgressTitle = "Copying Game Files..."
                    };

                    int result = SHFileOperation(ref fileOp);
                    return result == 0 && !fileOp.fAnyOperationsAborted;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Explorer copy exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Silent copy operation without user interface.
        /// LEGACY METHOD: Used for background operations.
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
                    if (!Directory.Exists(sourceDir))
                        return false;

                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                        Directory.CreateDirectory(targetParent);

                    var source = sourceDir.TrimEnd('\\') + "\\*.*\0\0";
                    var target = targetDir.TrimEnd('\\') + "\0\0";

                    var fileOp = new SHFILEOPSTRUCT
                    {
                        hwnd = IntPtr.Zero,
                        wFunc = FO_COPY,
                        pFrom = source,
                        pTo = target,
                        fFlags = FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR | FOF_NOERRORUI,
                        lpszProgressTitle = null
                    };

                    int result = SHFileOperation(ref fileOp);
                    return result == 0 && !fileOp.fAnyOperationsAborted;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Silent copy exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Count files recursively for progress calculation.
        /// FUTURE IMPROVEMENT: Will be optimized for large directories with background scanning.
        /// </summary>
        private static int CountFilesRecursively(string directory)
        {
            try
            {
                int count = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length;
                Debug.WriteLine($"📊 Counted {count} files in {directory}");
                return count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error counting files in {directory}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get directory size recursively.
        /// FUTURE IMPROVEMENT: Will be cached and updated incrementally.
        /// </summary>
        private static long GetDirectorySize(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return 0;

                return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error getting directory size for {directory}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Format bytes to human readable string.
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

        #endregion

        #region Future Implementation Stubs

        // TODO: Implement in Phase 1 of roadmap
        // /// <summary>
        // /// FUTURE METHOD: Parallel copy to multiple drives simultaneously
        // /// </summary>
        // public static async Task<Dictionary<string, bool>> CopyDirectoryToMultipleDrivesAsync(
        //     string sourceDir, 
        //     IEnumerable<string> targetDrives,
        //     IProgress<ParallelCopyProgress> progress,
        //     CancellationToken cancellationToken = default)
        // {
        //     // Implementation planned for Phase 1
        //     throw new NotImplementedException("Parallel multi-drive copy - planned for v2.0");
        // }

        // TODO: Implement in Phase 2 of roadmap  
        // /// <summary>
        // /// FUTURE METHOD: Smart copy with deduplication and incremental updates
        // /// </summary>
        // public static async Task<bool> CopyDirectoryWithIntelligentOptimizationAsync(
        //     string sourceDir,
        //     string targetDir, 
        //     CopyOptimizationOptions options,
        //     CancellationToken cancellationToken = default)
        // {
        //     // Implementation planned for Phase 2
        //     throw new NotImplementedException("Smart copy optimization - planned for v2.0");
        // }

        #endregion
    }
}

// FUTURE IMPROVEMENTS SUMMARY:
// =============================
// 1. Parallel multi-drive operations (Phase 1)
// 2. Dynamic queue management during operations (Phase 1) 
// 3. Advanced progress tracking with ETA (Phase 2)
// 4. Smart copy optimization with deduplication (Phase 2)
// 5. Resource management and bandwidth control (Phase 3)
// 6. Cloud integration and remote management (Phase 4)
//
// See FUTURE_IMPROVEMENTS.md for detailed roadmap
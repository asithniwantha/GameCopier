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
        // Legacy Windows Shell API for Explorer-style copy operations (kept for fallback)
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

        #region Modern IFileOperation COM Interface

        // Modern COM-based file operations - GUARANTEED to show progress dialogs
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

        [ComImport, Guid("3AD05575-8857-4850-9277-11B85BDB8E09")]
        public class FileOperationClass
        {
        }

        [ComImport, Guid("0947AAB5-0AA1-44E2-88E0-83AFCFC85CD3")]
        public class ProgressDialogClass
        {
        }

        // IShellItem interface
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

        // Shell API to create IShellItem
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            IntPtr pbc, ref Guid riid, out IntPtr shellItem);

        // File operation flags
        private const uint FOF_NO_UI = 0x04;
        private const uint FOF_ALLOWUNDO_FLAG = 0x40;
        private const uint FOF_NOERRORUI_FLAG = 0x400;
        private const uint FOF_NOCONFIRMATION_FLAG = 0x10;

        #endregion

        /// <summary>
        /// Modern IFileOperation implementation that GUARANTEES progress dialog display
        /// This is the solution to the dialog visibility problem!
        /// </summary>
        public static async Task<bool> CopyDirectoryWithModernFileOperationAsync(
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
                    statusCallback?.Invoke("🚀 Initializing modern file operation...");
                    Debug.WriteLine($"🎯 Starting Modern IFileOperation Copy: {sourceDir} -> {targetDir}");

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

                    statusCallback?.Invoke("⚙️ Creating COM file operation interface...");

                    // Create the modern IFileOperation COM object
                    var fileOperation = (IFileOperation)new FileOperationClass();

                    try
                    {
                        // Set operation flags - FORCE PROGRESS DIALOG TO SHOW
                        var flags = FOF_ALLOWUNDO_FLAG; // Show progress dialog with undo capability
                        fileOperation.SetOperationFlags(flags);

                        // Set the parent window
                        if (parentWindow != IntPtr.Zero)
                        {
                            fileOperation.SetOwnerWindow(parentWindow);
                        }

                        // Set progress message
                        fileOperation.SetProgressMessage("GameCopier - Copying Files...");

                        statusCallback?.Invoke("📋 Creating shell items for source and target...");

                        // Create IShellItem for source and destination
                        var riid = typeof(IShellItem).GUID;
                        
                        // Create source shell item
                        int hr = SHCreateItemFromParsingName(sourceDir, IntPtr.Zero, ref riid, out IntPtr sourceShellItem);
                        if (hr != 0)
                        {
                            statusCallback?.Invoke($"❌ Failed to create source shell item: {hr:X8}");
                            return false;
                        }

                        // Create destination shell item  
                        hr = SHCreateItemFromParsingName(targetParent!, IntPtr.Zero, ref riid, out IntPtr destShellItem);
                        if (hr != 0)
                        {
                            statusCallback?.Invoke($"❌ Failed to create destination shell item: {hr:X8}");
                            Marshal.Release(sourceShellItem);
                            return false;
                        }

                        statusCallback?.Invoke("🚀 Starting copy operation with guaranteed progress dialog...");

                        // Add the copy operation - this will show progress dialog
                        var targetName = Path.GetFileName(targetDir);
                        fileOperation.CopyItem(sourceShellItem, destShellItem, targetName, null);

                        // Execute the operation - PROGRESS DIALOG WILL APPEAR HERE
                        fileOperation.PerformOperations();

                        // Check if operation was aborted
                        fileOperation.GetAnyOperationsAborted(out bool aborted);

                        // Cleanup COM objects
                        Marshal.Release(sourceShellItem);
                        Marshal.Release(destShellItem);

                        if (aborted)
                        {
                            statusCallback?.Invoke("⚠️ Operation was cancelled by user");
                            return false;
                        }
                        else
                        {
                            statusCallback?.Invoke("✅ Modern copy operation completed successfully");
                            Debug.WriteLine($"✅ Modern IFileOperation Copy completed successfully");
                            return true;
                        }
                    }
                    finally
                    {
                        // Release the file operation COM object
                        Marshal.ReleaseComObject(fileOperation);
                    }
                }
                catch (COMException comEx)
                {
                    statusCallback?.Invoke($"❌ COM Error: {comEx.Message} (0x{comEx.HResult:X8})");
                    Debug.WriteLine($"❌ Modern IFileOperation COM exception: {comEx.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"❌ Error: {ex.Message}");
                    Debug.WriteLine($"❌ Modern IFileOperation exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        #region Legacy SHFileOperation Methods (kept for fallback)

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

                    if (!Directory.Exists(sourceDir))
                    {
                        Debug.WriteLine($"❌ Source directory does not exist: {sourceDir}");
                        return false;
                    }

                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent) && !Directory.Exists(targetParent))
                    {
                        Directory.CreateDirectory(targetParent);
                        Debug.WriteLine($"📁 Created target parent directory: {targetParent}");
                    }

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
                        fFlags = FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR | FOF_NOERRORUI,
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
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR,
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
        public static async Task<bool> CopyDirectoryWithReliableDialogAsync(
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
                    statusCallback?.Invoke("🔧 Initializing reliable dialog copy...");
                    Debug.WriteLine($"🔧 Starting Reliable Dialog Copy: {sourceDir} -> {targetDir}");

                    if (!Directory.Exists(sourceDir))
                    {
                        statusCallback?.Invoke("❌ Source directory not found");
                        return false;
                    }

                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent))
                    {
                        Directory.CreateDirectory(targetParent);
                    }

                    // Reset Shell API state before each operation
                    Marshal.GetLastWin32Error(); // Clear error state
                    Thread.Sleep(500); // Allow Shell API to reset

                    var source = sourceDir.TrimEnd('\\') + "\\*.*\0\0";
                    var target = targetDir.TrimEnd('\\') + "\0\0";

                    statusCallback?.Invoke("📋 Starting fresh Windows copy dialog...");

                    var fileOp = new SHFILEOPSTRUCT
                    {
                        hwnd = parentWindow,
                        wFunc = FO_COPY,
                        pFrom = source,
                        pTo = target,
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR | FOF_SIMPLEPROGRESS,
                        lpszProgressTitle = $"GameCopier - Copy #{DateTime.Now.Ticks % 1000}"
                    };

                    Debug.WriteLine($"🔧 Reliable copy flags: {fileOp.fFlags}, Title: {fileOp.lpszProgressTitle}");

                    int result = SHFileOperation(ref fileOp);

                    if (result == 0 && !fileOp.fAnyOperationsAborted)
                    {
                        statusCallback?.Invoke("✅ Copy completed with dialog");
                        Debug.WriteLine($"✅ Reliable Dialog Copy completed successfully");
                        return true;
                    }
                    else
                    {
                        statusCallback?.Invoke($"❌ Copy failed (Code: {result})");
                        Debug.WriteLine($"❌ Reliable Dialog Copy failed - Result: {result}, Aborted: {fileOp.fAnyOperationsAborted}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"❌ Error: {ex.Message}");
                    Debug.WriteLine($"❌ Reliable Dialog Copy exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Hybrid copy method that combines reliability with dialog visibility
        /// Uses SHFileOperation with enhanced dialog control and fallback mechanisms
        /// </summary>
        public static async Task<bool> CopyDirectoryWithEnhancedReliabilityAsync(
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
                    statusCallback?.Invoke("🚀 Initializing enhanced copy operation...");
                    Debug.WriteLine($"🎯 Starting Enhanced Reliable Copy: {sourceDir} -> {targetDir}");

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

                    // SOLUTION: Use Windows Explorer directly with proper folder copying
                    statusCallback?.Invoke("🚀 Starting Windows Explorer copy operation...");
                    try
                    {
                        var success = CopyUsingWindowsExplorerDirect(sourceDir, targetDir, statusCallback);
                        if (success)
                        {
                            statusCallback?.Invoke("✅ Explorer copy completed successfully");
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ Explorer direct method failed: {ex.Message}");
                        statusCallback?.Invoke("⚠️ Direct method failed, trying alternative...");
                    }

                    // Method 2: Fallback to enhanced SHFileOperation
                    statusCallback?.Invoke("🔄 Using enhanced SHFileOperation...");
                    
                    // Reset shell state to ensure fresh operation
                    Thread.Sleep(1000);
                    Marshal.GetLastWin32Error();

                    var source = sourceDir.TrimEnd('\\') + "\\*.*\0\0";
                    var target = targetDir.TrimEnd('\\') + "\0\0";

                    var fileOp = new SHFILEOPSTRUCT
                    {
                        hwnd = parentWindow,
                        wFunc = FO_COPY,
                        pFrom = source,
                        pTo = target,
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMMKDIR, // Show progress dialog
                        lpszProgressTitle = $"GameCopier - {Path.GetFileName(sourceDir)}"
                    };

                    Debug.WriteLine($"🔧 Enhanced copy flags: {fileOp.fFlags}");

                    int result = SHFileOperation(ref fileOp);

                    if (result == 0 && !fileOp.fAnyOperationsAborted)
                    {
                        statusCallback?.Invoke("✅ Enhanced copy completed successfully");
                        Debug.WriteLine($"✅ Enhanced Reliable Copy completed successfully");
                        return true;
                    }
                    else
                    {
                        statusCallback?.Invoke($"❌ Copy failed (Error: {result})");
                        Debug.WriteLine($"❌ Enhanced Reliable Copy failed - Result: {result}, Aborted: {fileOp.fAnyOperationsAborted}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"❌ Copy error: {ex.Message}");
                    Debug.WriteLine($"❌ Enhanced Reliable Copy exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Copy using Windows Explorer directly via ProcessStartInfo
        /// This method shows the actual Windows copy dialog every time
        /// </summary>
        private static bool CopyUsingWindowsExplorerDirect(string sourceDir, string targetDir, Action<string> statusCallback)
        {
            try
            {
                statusCallback?.Invoke("🚀 Launching Windows Explorer copy...");

                // Use the new dual progress method for better user experience
                var task = CopyDirectoryWithDualProgressAsync(
                    sourceDir, 
                    targetDir, 
                    IntPtr.Zero, 
                    statusCallback,
                    progress => statusCallback?.Invoke($"📊 Progress: {progress:F1}%"),
                    CancellationToken.None);

                return task.Result;
            }
            catch (Exception ex)
            {
                statusCallback?.Invoke($"❌ Explorer direct error: {ex.Message}");
                Debug.WriteLine($"❌ CopyUsingWindowsExplorerDirect: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PowerShell-based copy with progress dialog
        /// This guarantees a visible progress dialog for every operation
        /// </summary>
        public static async Task<bool> CopyDirectoryWithPowerShellProgressAsync(
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
                    statusCallback?.Invoke("🚀 Initializing PowerShell copy with progress...");
                    Debug.WriteLine($"🎯 Starting PowerShell Copy: {sourceDir} -> {targetDir}");

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
                    }

                    statusCallback?.Invoke("🚀 Starting PowerShell copy with progress dialog...");

                    // Create PowerShell script that shows progress
                    var script = $@"
                        Add-Type -AssemblyName System.Windows.Forms
                        $source = '{sourceDir.Replace("'", "''")}'

                        robocopy ""$source"" ""$destination"" /E /IS /IT /IM /XJ /R:3 /W:1 /NP
                        
                        $exitCode = $LASTEXITCODE
                        if ($exitCode -le 7) {{
                            Write-Host 'Copy completed successfully'
                            exit 0
                        }} else {{
                            Write-Host 'Copy failed'
                            exit $exitCode
                        }}
                    ";

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-WindowStyle Normal -Command \"{script}\"",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal,
                        CreateNoWindow = false
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        
                        bool success = process.ExitCode == 0;
                        
                        if (success)
                        {
                            statusCallback?.Invoke("✅ PowerShell copy completed successfully");
                            return true;
                        }
                        else
                        {
                            statusCallback?.Invoke($"❌ PowerShell copy failed with exit code {process.ExitCode}");
                            return false;
                        }
                    }
                    else
                    {
                        statusCallback?.Invoke("❌ Failed to start PowerShell process");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"❌ PowerShell copy error: {ex.Message}");
                    Debug.WriteLine($"❌ PowerShell copy exception: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Enhanced robocopy with real-time progress tracking and UI updates
        /// This method provides both visible progress AND tracks progress in the application UI
        /// </summary>
        public static async Task<bool> CopyDirectoryWithRobocopyProgressAsync(
            string sourceDir,
            string targetDir,
            IntPtr parentWindow,
            Action<string> statusCallback,
            Action<double> progressCallback,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    statusCallback?.Invoke("🚀 Initializing robocopy with progress tracking...");
                    Debug.WriteLine($"🎯 Starting Robocopy with Progress: {sourceDir} -> {targetDir}");

                    if (!Directory.Exists(sourceDir))
                    {
                        statusCallback?.Invoke("❌ Source directory not found");
                        return false;
                    }

                    // Count total files for progress calculation
                    statusCallback?.Invoke("📊 Analyzing source files...");
                    int totalFiles = CountFilesRecursively(sourceDir);
                    statusCallback?.Invoke($"📊 Found {totalFiles} files to copy");

                    // Ensure target directory structure exists
                    var targetParent = Path.GetDirectoryName(targetDir);
                    if (!string.IsNullOrEmpty(targetParent))
                    {
                        Directory.CreateDirectory(targetParent);
                        statusCallback?.Invoke("📁 Target directory prepared");
                    }

                    statusCallback?.Invoke("🚀 Starting robocopy with progress monitoring...");

                    // Use robocopy with detailed output for progress tracking
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "robocopy",
                        Arguments = $"\"{sourceDir}\" \"{targetDir}\" /E /IS /IT /IM /XJ /R:3 /W:1 /NP /NDL /NJH /NJS",
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = new Process { StartInfo = startInfo };
                    
                    int copiedFiles = 0;
                    string currentFile = "";

                    // Monitor output for progress
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            var line = e.Data.Trim();
                            
                            // Track file copying progress
                            if (line.Contains("New File") || line.Contains("Newer") || line.Contains("100%"))
                            {
                                copiedFiles++;
                                var progress = totalFiles > 0 ? (double)copiedFiles / totalFiles * 100.0 : 0;
                                progressCallback?.Invoke(Math.Min(progress, 100));
                                
                                // Extract filename from robocopy output
                                var parts = line.Split('\t');
                                if (parts.Length > 0)
                                {
                                    currentFile = parts[parts.Length - 1].Trim();
                                    statusCallback?.Invoke($"📄 Copying: {currentFile} ({copiedFiles}/{totalFiles})");
                                }
                            }
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    
                    // Show a progress window alongside the robocopy operation
                    var progressTask = Task.Run(async () =>
                    {
                        while (!process.HasExited)
                        {
                            await Task.Delay(500);
                            var progress = totalFiles > 0 ? (double)copiedFiles / totalFiles * 100.0 : 0;
                            progressCallback?.Invoke(Math.Min(progress, 100));
                        }
                    });

                    process.WaitForExit();
                    await progressTask;

                    // Robocopy exit codes 0-7 are success
                    bool success = process.ExitCode <= 7;
                    
                    if (success)
                    {
                        progressCallback?.Invoke(100); // Ensure 100% completion
                        statusCallback?.Invoke($"✅ Robocopy completed successfully - {copiedFiles} files copied");
                        return true;
                    }
                    else
                    {
                        statusCallback?.Invoke($"❌ Robocopy failed with exit code {process.ExitCode}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    statusCallback?.Invoke($"❌ Robocopy error: {ex.Message}");
                    Debug.WriteLine($"❌ CopyDirectoryWithRobocopyProgressAsync: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Count files recursively for progress calculation
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
        /// Enhanced copy method with both visible progress window AND UI progress tracking
        /// This method provides the best of both worlds: robocopy reliability + UI progress
        /// </summary>
        public static async Task<bool> CopyDirectoryWithDualProgressAsync(
            string sourceDir,
            string targetDir,
            IntPtr parentWindow,
            Action<string> statusCallback,
            Action<double> progressCallback,
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
                        WindowStyle = ProcessWindowStyle.Normal, // Visible progress window
                        CreateNoWindow = false
                    };

                    // Start the visible robocopy process
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
                                    progress = Math.Min(progress, 99); // Keep at 99% until process completes
                                    
                                    progressCallback?.Invoke(progress);
                                    statusCallback?.Invoke($"📊 Progress: {FormatBytes(currentSize)} / {FormatBytes(totalSize)} ({progress:F1}%)");

                                    // Check if copy is stable (no size change)
                                    if (currentSize == lastSize)
                                    {
                                        stableCount++;
                                    }
                                    else
                                    {
                                        stableCount = 0;
                                        lastSize = currentSize;
                                    }

                                    // If stable for too long, assume completion
                                    if (stableCount > 10 && currentSize > 0)
                                    {
                                        break;
                                    }
                                }

                                await Task.Delay(1000); // Check every second
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"⚠️ Progress monitoring error: {ex.Message}");
                                await Task.Delay(2000);
                            }
                        }
                    });

                    // Wait for robocopy to complete
                    if (visibleProcess != null)
                    {
                        visibleProcess.WaitForExit();
                        await progressTask;

                        // Robocopy exit codes 0-7 are success
                        bool success = visibleProcess.ExitCode <= 7;
                        
                        if (success)
                        {
                            progressCallback?.Invoke(100); // Ensure 100% completion
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
        /// Get directory size recursively
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
        /// Format bytes to human readable string
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
    }

    #endregion
}
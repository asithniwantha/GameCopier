using System;
using System.IO;

namespace GameCopier.Services
{
    public class LoggingService
    {
        private readonly string _logFilePath;

        public LoggingService(string logDirectory = @"C:\GameDeployLogs")
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            _logFilePath = Path.Combine(logDirectory, $"GameDeploy_{DateTime.Now:yyyyMMdd}.log");
        }

        public void LogDeploymentStarted(string games, string drives)
        {
            LogMessage($"DEPLOYMENT STARTED - Games: {games} | Drives: {drives}");
        }

        public void LogDeploymentCompleted(string jobId, string game, string drive, bool success, string? error = null)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var message = $"DEPLOYMENT {status} - Job: {jobId} | Game: {game} | Drive: {drive}";
            
            if (!success && !string.IsNullOrEmpty(error))
            {
                message += $" | Error: {error}";
            }
            
            LogMessage(message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var logEntry = $"ERROR - {message}";
            if (exception != null)
            {
                logEntry += $" | Exception: {exception.Message}";
            }
            
            LogMessage(logEntry);
        }

        private void LogMessage(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = $"[{timestamp}] {message}";
                
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Silent fail - don't let logging errors break the application
            }
        }
    }
}
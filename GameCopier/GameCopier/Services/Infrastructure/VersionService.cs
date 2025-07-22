using System;
using System.Reflection;

namespace GameCopier.Services.Infrastructure
{
    /// <summary>
    /// Service for retrieving application version information
    /// </summary>
    public static class VersionService
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly AssemblyName _assemblyName = _assembly.GetName();

        /// <summary>
        /// Gets the application version in format "Major.Minor.Build"
        /// </summary>
        public static string ApplicationVersion => _assemblyName.Version?.ToString(3) ?? "1.0.0";

        /// <summary>
        /// Gets the full version including revision
        /// </summary>
        public static string FullVersion => _assemblyName.Version?.ToString() ?? "1.0.0.0";

        /// <summary>
        /// Gets the informational version (includes pre-release info)
        /// </summary>
        public static string InformationalVersion
        {
            get
            {
                var attribute = _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                return attribute?.InformationalVersion ?? ApplicationVersion;
            }
        }

        /// <summary>
        /// Gets the product name
        /// </summary>
        public static string ProductName
        {
            get
            {
                var attribute = _assembly.GetCustomAttribute<AssemblyProductAttribute>();
                return attribute?.Product ?? "GameCopier";
            }
        }

        /// <summary>
        /// Gets the application title
        /// </summary>
        public static string ApplicationTitle
        {
            get
            {
                var attribute = _assembly.GetCustomAttribute<AssemblyTitleAttribute>();
                return attribute?.Title ?? "GameCopier";
            }
        }

        /// <summary>
        /// Gets the application description
        /// </summary>
        public static string ApplicationDescription
        {
            get
            {
                var attribute = _assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
                return attribute?.Description ?? "Game and Software Deployment Tool";
            }
        }

        /// <summary>
        /// Gets the copyright information
        /// </summary>
        public static string Copyright
        {
            get
            {
                var attribute = _assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
                return attribute?.Copyright ?? $"Copyright © {DateTime.Now.Year}";
            }
        }

        /// <summary>
        /// Gets the company name
        /// </summary>
        public static string Company
        {
            get
            {
                var attribute = _assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
                return attribute?.Company ?? "GameCopier Development Team";
            }
        }

        /// <summary>
        /// Gets the build configuration (Debug/Release)
        /// </summary>
        public static string Configuration
        {
            get
            {
                var attribute = _assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
                return attribute?.Configuration ?? "Unknown";
            }
        }

        /// <summary>
        /// Gets the build date from the assembly
        /// </summary>
        public static DateTime BuildDate
        {
            get
            {
                var version = _assemblyName.Version;
                if (version != null)
                {
                    // Calculate build date from version (days since 2000-01-01)
                    var buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
                    return buildDate;
                }
                return DateTime.Now;
            }
        }

        /// <summary>
        /// Gets a formatted version string for display
        /// </summary>
        public static string GetDisplayVersion()
        {
            var config = Configuration.Equals("Debug", StringComparison.OrdinalIgnoreCase) ? " (Debug)" : "";
            return $"{ApplicationTitle} v{InformationalVersion}{config}";
        }

        /// <summary>
        /// Gets detailed version information for about dialogs
        /// </summary>
        public static string GetDetailedVersionInfo()
        {
            return $"""
                {ApplicationTitle}
                Version: {InformationalVersion}
                Build: {FullVersion}
                Configuration: {Configuration}
                Build Date: {BuildDate:yyyy-MM-dd HH:mm:ss}
                
                {ApplicationDescription}
                
                {Copyright}
                {Company}
                """;
        }

        /// <summary>
        /// Checks if this is a pre-release version
        /// </summary>
        public static bool IsPreRelease => InformationalVersion.Contains("-");

        /// <summary>
        /// Gets the major version number
        /// </summary>
        public static int MajorVersion => _assemblyName.Version?.Major ?? 1;

        /// <summary>
        /// Gets the minor version number
        /// </summary>
        public static int MinorVersion => _assemblyName.Version?.Minor ?? 0;

        /// <summary>
        /// Gets the build number
        /// </summary>
        public static int BuildNumber => _assemblyName.Version?.Build ?? 0;

        /// <summary>
        /// Gets the revision number
        /// </summary>
        public static int RevisionNumber => _assemblyName.Version?.Revision ?? 0;
    }
}
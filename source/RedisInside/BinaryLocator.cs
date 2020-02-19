using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RedisInside
{
    internal class BinaryLocator
    {
        private readonly string _nugetPrefix = Path.Combine("packages", "RedisInside*");
        private readonly string _nugetCachePrefix = Path.Combine("packages", "RedisInside", "*");
        private const string DefaultWindowsSearchPattern = @"tools\redis";
        private const string DefaultLinuxSearchPattern = "*/tools/redis";
        private const string WindowsNugetCacheLocation = @"%USERPROFILE%\.nuget\packages";
        private static readonly string OsxAndLinuxNugetCacheLocation = Environment.GetEnvironmentVariable("HOME") + "/.nuget/packages/RedisInside";
        private string _binFile = string.Empty;
        private readonly string _searchPattern;
        private readonly string _nugetCacheDirectory;

        public BinaryLocator()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _searchPattern = DefaultLinuxSearchPattern;
                _nugetCacheDirectory = OsxAndLinuxNugetCacheLocation;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _searchPattern = DefaultWindowsSearchPattern;
                _nugetCacheDirectory = Environment.ExpandEnvironmentVariables(WindowsNugetCacheLocation);
            }
            else
            {
                throw new Exception($"Unknown OS: {RuntimeInformation.OSDescription}");
            }
        }

        public string Folder
        {
            get
            {
                if (string.IsNullOrEmpty(_binFile))
                {
                    return _binFile = ResolveBinary();
                }
                else
                {
                    return _binFile;
                }
            }
        }

        private string ResolveBinary()
        {
            var searchDirectories = new[]
            {
                // Then search from the project directory
                FolderSearch.CurrentExecutingDirectory(),
                // Finally search from the nuget cache directory
                _nugetCacheDirectory
            };
            return FindBinariesDirectory(searchDirectories.Where(x => !string.IsNullOrWhiteSpace(x)).ToList());
        }

        private string FindBinariesDirectory(IList<string> searchDirectories)
        {
            foreach (var directory in searchDirectories)
            {
                var binaryFolder =
                    // First try just the search pattern
                    directory.FindFolderUpwards(_searchPattern) ??
                    // Next try the search pattern with nuget installation prefix
                    directory.FindFolderUpwards(Path.Combine(_nugetPrefix, _searchPattern)) ??
                    // Finally try the search pattern with the nuget cache prefix
                    directory.FindFolderUpwards(Path.Combine(_nugetCachePrefix, _searchPattern));
                if (binaryFolder != null)
                {
                    return binaryFolder;
                }
            }
            throw new Exception(
                $"Could not find Redis binaries using the search patterns \"{_searchPattern}\", \"{Path.Combine(_nugetPrefix, _searchPattern)}\", and \"{Path.Combine(_nugetCachePrefix, _searchPattern)}\".  " +
                $"We walked up to root directory from the following locations.\n {string.Join("\n", searchDirectories)}");
        }
    }
}

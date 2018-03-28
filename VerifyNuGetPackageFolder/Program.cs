using NuGet.Protocol;
using NuGet.Common;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace VerifyNuGetPackageFolder
{
    class Program
    {
        static void Main(string[] args)
        {
            var dotnetRootPath = Environment.GetEnvironmentVariable("UserProfile");
            var dotnetFallback = Path.Combine(dotnetRootPath, ".dotnet", "NuGetPackagesFallback");
            var rootPath = @"C:\Program Files (x86)\Microsoft SDKs";
            var packages = System.IO.Path.Combine(rootPath, "NuGetPackages");
            var packagesFallback = System.IO.Path.Combine(rootPath, "NuGetPackagesFallback");
            var invalidFolders = VerifyPackageFolderIntegrity(packages);
            invalidFolders.Concat(VerifyPackageFolderIntegrity(packagesFallback));
            invalidFolders.Concat(VerifyPackageFolderIntegrity(dotnetFallback));

            foreach (var folder in invalidFolders)
            {
                Console.WriteLine(folder);
            }

            Console.ReadLine();
        }


        private static IList<string> VerifyPackageFolderIntegrity(string folderRoot)
        {
            var listOfInvalidFolders = new List<string>();
            if (!Directory.Exists(folderRoot))
            {
                Console.WriteLine($"Could not find folder: {folderRoot}");
                return listOfInvalidFolders;
            }

            var packageInfos = LocalFolderUtility.GetPackagesV3(folderRoot, NullLogger.Instance);            
            var numExtractedFolders = 0;
            var numNotExtractedFolders = 0;
            var numIncorrectExtractedFolders = 0;
            foreach (var packageInfo in packageInfos)
            {
                var directory = Path.GetDirectoryName(packageInfo.Path);
                var sha512 = packageInfo.Path + ".sha512";
                if (File.Exists(sha512))
                {
                    var packageReader = packageInfo.GetReader();
                    var filesInPackage = packageReader.GetFiles().Where(t => !IsNotExtractableFile(t));
                    foreach (var file in filesInPackage)
                    {
                        var fullPath = Path.GetFullPath(Path.Combine(directory, file));
                        if (!File.Exists(fullPath))
                        {
                            listOfInvalidFolders.Add(directory);
                            numIncorrectExtractedFolders++;
                            break;
                        }
                    }
                    numExtractedFolders++;
                }
                else
                {
                    numNotExtractedFolders++;
                    // if no sha512 file exists, we are good.
                    continue;
                }
            }

            Console.WriteLine($"Total number of extracted folders in {folderRoot}: {numExtractedFolders}");
            Console.WriteLine($"Total number of incorrectly extracted folders in {folderRoot}: {numIncorrectExtractedFolders}");
            Console.WriteLine($"Total number of unextracted folders in {folderRoot}: {numNotExtractedFolders}");

            return listOfInvalidFolders;
        }

        private static bool IsNotExtractableFile(string packageFile)
        {
            return packageFile.StartsWith("package/") || packageFile.StartsWith("_rels/") || packageFile.StartsWith("[Content_Types].xml") || packageFile.EndsWith('/');
        }
    }
}

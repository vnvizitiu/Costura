﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;
using NUnit.Framework;

public static class Decompiler
{
    public static string Decompile(string assemblyPath, string identifier = "")
    {
        var exePath = GetPathToILDasm();

        if (!string.IsNullOrEmpty(identifier))
            identifier = "/item:" + identifier;

        using (var process = Process.Start(new ProcessStartInfo(exePath, $"\"{assemblyPath}\" /text /linenum {identifier}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            var projectFolder = Path.GetFullPath(Path.GetDirectoryName(assemblyPath) + "\\..\\..\\..").Replace("\\", "\\\\");
            projectFolder = String.Format("{0}{1}\\\\", Char.ToUpper(projectFolder[0]), projectFolder.Substring(1));

            process.WaitForExit(10000);

            var checksumPattern = new Regex(@"^(\s*IL_[0123456789abcdef]{4}:  ldstr\s*"")[0123456789ABCDEF]{32,40}""", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            return string.Join(Environment.NewLine, Regex.Split(process.StandardOutput.ReadToEnd(), Environment.NewLine)
                    .Where(l => !l.StartsWith("// ", StringComparison.CurrentCulture) && !string.IsNullOrEmpty(l))
                    .Select(l => l.Replace(projectFolder, ""))
                    .Select(l => checksumPattern.Replace(l, e => e.Groups[1].Value + "[CHECKSUM]\""))
                    .ToList());
        }
    }

    private static string GetPathToILDasm()
    {
        var path = ToolLocationHelper.GetPathToDotNetFrameworkSdkFile("ildasm.exe", TargetDotNetFrameworkVersion.VersionLatest);
        if (!File.Exists(path))
            Assert.Ignore("ILDasm could not be found");
        return path;
    }
}
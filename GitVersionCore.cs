//
// Copyright (C) 2010-2020 Kody Brown (thewizard@wasatchwizard.com).
//
// MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class GitVersionCore : Task
{
    public string GitBinPath { get; set; } = null;

    public string CurrentPath { get; set; } = null;

    public string OutputFile { get; set; } = null;

    public string Copyright { get; set; } = null;

    [Output]
    public int MajorVersion { get; set; }

    [Output]
    public int MinorVersion { get; set; }

    [Output]
    public DateTime BuildDate { get; set; }

    [Output]
    public int Year { get; set; }

    [Output]
    public string CommitHash { get; set; }

    [Output]
    public int CommitCount { get; set; }


    private readonly string _GitFile = null;

    public GitVersionCore()
    {
        _GitFile = "git.exe";
        if (!string.IsNullOrEmpty(GitBinPath)) {
            _GitFile = Path.Combine(GitBinPath, "git.exe");
            if (!File.Exists(_GitFile)) {
                _GitFile = "git.exe";
            }
        }
    }

    public override bool Execute()
    {
        //Console.WriteLine("GitVersionCore(): running..");

        if (string.IsNullOrEmpty(OutputFile)) {
            OutputFile = "obj/Debug/netcoreapp2.2/ApiServer.AssemblyInfo.cs";
        }

        if (string.IsNullOrEmpty(Copyright)) {
            Copyright = "Copyright (C) {Year}";
        }

        if (string.IsNullOrEmpty(CurrentPath)) {
            CurrentPath = ".";
        }
        Environment.CurrentDirectory = CurrentPath;
        CurrentPath = Environment.CurrentDirectory;

        BuildDate = DateTime.Now;
        Year = BuildDate.Year;
        CommitHash = GetCommitHash();
        CommitCount = GetCommitCount();

        WritePropertiesToFile();

        return true;
    }

    private void WritePropertiesToFile()
    {
        var aiFile = Path.Combine(Environment.CurrentDirectory, OutputFile);
        if (!File.Exists(aiFile)) {
            Console.WriteLine($"Could not find AssemblyInfo.cs file '{aiFile}'");
            return;
        }

        var lines = File.ReadAllLines(aiFile);

        var title = "";
        var copyright = Copyright.Replace("{Year}", BuildDate.Year.ToString());
        var fullVersion = "";

        for (var i = 0; i < lines.Length; i++) {
            var line = lines[i].Trim();
            if (line.StartsWith("[assembly: System.Reflection.AssemblyVersionAttribute(")) {
                var version = line.Substring(line.IndexOf('"') + 1, line.LastIndexOf('"') - line.IndexOf('"') - 1);
                var vparts = version.Split('.');
                fullVersion = $"{vparts[0]}.{vparts[1]}.{BuildDate.ToString("yyMMdd").Substring(1)}.{CommitCount}";
                //Console.WriteLine($"fullVersion == '{fullVersion}'");
            }
            if (line.StartsWith("[assembly: System.Reflection.AssemblyTitleAttribute(")) {
                title = line.Substring(line.IndexOf('"') + 1, line.LastIndexOf('"') - line.IndexOf('"') - 1);
                //Console.WriteLine($"title == '{title}'");
            }
        }

        for (var i = 0; i < lines.Length; i++) {
            var line = lines[i].Trim();

            if (line.StartsWith("[assembly: System.Reflection.AssemblyProductAttribute(")) {
                lines[i] = $"[assembly: System.Reflection.AssemblyProductAttribute(\"{title} v:{fullVersion} #{CommitHash}\")]";
            } else if (line.StartsWith("[assembly: System.Reflection.AssemblyCopyrightAttribute(")) {
                lines[i] = $"[assembly: System.Reflection.AssemblyCopyrightAttribute(\"{copyright}\")]";
            } else if (line.StartsWith("[assembly: System.Reflection.AssemblyFileVersionAttribute(")) {
                lines[i] = $"[assembly: System.Reflection.AssemblyFileVersionAttribute(\"{fullVersion}\")]";
            } else if (line.StartsWith("[assembly: System.Reflection.AssemblyInformationalVersionAttribute(")) {
                lines[i] = $"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(\"{fullVersion}\")]";
            } else if (line.StartsWith("[assembly: System.Reflection.AssemblyVersionAttribute(")) {
                lines[i] = $"[assembly: System.Reflection.AssemblyVersionAttribute(\"{fullVersion}\")]";
            }
        }

        File.WriteAllLines(aiFile, lines, Encoding.UTF8);

        Console.WriteLine($"  GitVersionCore(): {title} version: {fullVersion} hash: {CommitHash}");
    }

    private string GetCommitHash()
    {
        var startInfo = new ProcessStartInfo(_GitFile, "log --oneline -1") {
            UseShellExecute = false,
            ErrorDialog = false,
            //CreateNoWindow = true,
            CreateNoWindow = true,
            WorkingDirectory = CurrentPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var p = Process.Start(startInfo);
        //p.WaitForExit(30000);

        string val;

        using (var reader = p.StandardOutput) {
            val = reader.ReadToEnd();
        }

        using (var reader = p.StandardError) {
            while (!reader.EndOfStream) {
                var l = reader.ReadLine();
                Console.WriteLine("ERR:" + l);
            }
        }

        return val != null
            ? val.Split(' ')[0]
            : "not found";
    }

    private int GetCommitCount()
    {
        var startInfo = new ProcessStartInfo(_GitFile, "log --pretty=format:%h") {
            UseShellExecute = false,
            ErrorDialog = false,
            //CreateNoWindow = true,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var p = Process.Start(startInfo);
        //p.WaitForExit(30000);

        var count = 0;

        using (var reader = p.StandardOutput) {
            while (!reader.EndOfStream) {
                reader.ReadLine();
                count++;
            }
        }

        using (var reader = p.StandardError) {
            while (!reader.EndOfStream) {
                var l = reader.ReadLine();
                Console.WriteLine("ERR:" + l);
            }
        }

        return count;
    }
}

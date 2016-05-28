/*
Copyright(c) 2016, Vladyslav Taranov

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;
using NUnit.Framework;

public static class PEVerify
{
    public static bool AssertValid(string path)
    {
        string sdkRootPath;
#if ANDROID
        sdkRootPath = Environment.GetEnvironmentVariable(Environment.Is64BitOperatingSystem ? "ProgramFiles" : "ProgramFiles(x86)");
        sdkRootPath += @"\Microsoft SDKs\Windows\v8.1A";
#else
        sdkRootPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows\\v8.1A", "InstallationFolder", null) as string;
        if (null == sdkRootPath)
            throw new InvalidOperationException("Could not find Windows SDK 8.1A installation folder.");
#endif

        // note; PEVerify can be found %ProgramFiles%\Microsoft SDKs\Windows\
        string exePath = Path.Combine(sdkRootPath, "bin", "NETFX 4.5.1 Tools", "PEVerify.exe");
        var startInfo = new ProcessStartInfo(exePath, '"' + path + '"');
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;
        startInfo.StandardOutputEncoding = Encoding.GetEncoding(866);
        startInfo.CreateNoWindow = true;
        using (Process proc = Process.Start(startInfo))
        {
            bool ok = proc.WaitForExit(10000);
            string output = proc.StandardOutput.ReadToEnd();
            if (ok)
            {
                Assert.AreEqual(0, proc.ExitCode, path + "\r\n" + output);
                return proc.ExitCode == 0;
            }
            else
            {
                try
                {
                    proc.Kill();
                }
                catch
                {
                }
                Assert.Fail("PEVerify timeout: " + path + "\r\n" + output);
                return false;
            }
        }
    }
}
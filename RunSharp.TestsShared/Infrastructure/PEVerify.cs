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
        var sdkRootPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows\\v8.1A", "InstallationFolder", null) as string;
        if (null == sdkRootPath)
            throw new InvalidOperationException("Could not find Windows SDK 8.1A installation folder.");


        // note; PEVerify can be found %ProgramFiles%\Microsoft SDKs\Windows\
        string exePath = Path.Combine(sdkRootPath, "bin", "NETFX 4.5.1 Tools", "PEVerify.exe");
        var startInfo = new ProcessStartInfo(exePath, '"' + path + '"');
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;
        startInfo.StandardOutputEncoding = Encoding.GetEncoding(866);
        using (Process proc = Process.Start(startInfo))
        {
            bool ok = proc.WaitForExit(10000);
            string output = proc.StandardOutput.ReadToEnd();
            if (ok)
            {
                Assert.AreEqual(0, proc.ExitCode, path, output);
                return proc.ExitCode == 0;
            }
            else
            {
                proc.Kill();
                throw new TimeoutException();
            }
        }
    }
}
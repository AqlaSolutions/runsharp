@"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MsBuild" all.build /p:WarningLevel=0
@packages\NuGet.CommandLine.2.0.40000\tools\NuGet.exe pack Nuget\runsharp.nuspec
pause
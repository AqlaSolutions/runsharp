del *.nupkg
del RunSharp\bin\Release\*.nupkg
@"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild" all.build /p:WarningLevel=0
@"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild" /t:pack /p:Configuration=Release RunSharp\RunSharp.csproj
copy /y RunSharp\bin\Release\*.nupkg .
nuget pack RunSharpIKVM\package.nuspec -Prop Configuration=Release
pause
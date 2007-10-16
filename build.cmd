@echo off
pushd "%~dp0"
"%SystemRoot%\Microsoft.NET\Framework\v2.0.50727\msbuild.exe" RunSharp.sln %*
popd

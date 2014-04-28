@echo OFF

set target=%1
if "%target%" == "" (
   set target=Default
)

if "%target%" == "NuGetPack" (
	if "%BUILD_NUMBER%" == "" (
	 	echo BUILD_NUMBER environment variable is not set.
		exit;
	)
)
if NOT "%BuildRunner%" == "MyGet" (
	set PatchVersion=1
)

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Build\build.proj /target:%target% /v:normal /fl /flp:LogFile=msbuild.log;Verbosity=normal /nr:false
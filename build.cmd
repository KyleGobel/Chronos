@echo OFF

set target=%1
if "%target%" == "" (
   set target=UnitTests
   %BUILD_NUMBER% = 1
    echo "%BUILD_NUMBER%"
)

if "%target%" == "NuGetPack" (
	if "%BUILD_NUMBER%" == "" (
	 	echo BUILD_NUMBER environment variable is not set.
		exit;
	)
)

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Build\build.proj /target:%target% /v:normal /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false
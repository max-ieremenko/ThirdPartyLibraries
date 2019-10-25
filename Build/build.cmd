@echo Off

set MsBuildExe=
if "%MsBuildExe%" == "" (
	set "MsBuildExe=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
)

"%MsBuildExe%" /target:main build/build.targets

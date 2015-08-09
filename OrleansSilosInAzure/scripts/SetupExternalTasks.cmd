if "%EMULATED%"=="true" goto SKIP
if "%EXTERNALTASKURL%"=="" goto SKIP

:NOSKIP

cd %ROLEROOT%\approot\scripts
md %ROLEROOT%\approot\scripts\external

reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f
powershell .\SetupExternalTasks.ps1 -tasksUrl "%EXTERNALTASKURL%" >> ExternalTasks.log 2>> ExternalTasks_err.log

:SKIP
EXIT /B 0
if "%EMULATED%"=="true" goto SKIP
if "%EXTERNALTASKURL%"=="" goto SKIP

:NOSKIP

cd %ROLEROOT%\approot\scripts
md %ROLEROOT%\approot\scripts\external

reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f
echo HOLA >> c:\Temp\sal.txt
powershell .\SetupExternalTasks.ps1 -tasksUrl "%EXTERNALTASKURL%" >> ExternalTasks.log 2>> ExternalTasks_err.log

:SKIP
echo CHAU >> c:\Temp\sal.txt
EXIT /B 0
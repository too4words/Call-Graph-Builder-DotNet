if "%EMULATED%"=="true" goto SKIP
REM if "%EXTERNALTASKURL%"=="" goto SKIP


set Source=https://orleanstorage2.blob.core.windows.net/project-files 
REM https://orleansdatastorage.blob.core.windows.net/project-files 
set SourceKey=ilzOub7LFk5zQ7drJFkfoxdwN1rritlSWAJ9Vl35g/TG4rZWxCXWNTJV20vZLTL/D2LK065cG8AozDg8CGOKQQ==
REM w1Ue+xPB9xEZt5A1WzXWjXNUtZ1pSkykC1VtbJElMmCjK4bmkrLCtiZ4k0N7o8IEzHWDAp4kn0P+X+HhqsttdQ== 

goto START

:NOSKIP

set Source=http://127.0.0.1:10000/devstoreaccount1/project-files
set SourceKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==

:START

md c:\temp
icacls c:\temp /grant Everyone:F

cd %ROLEROOT%\approot\scripts
md %ROLEROOT%\approot\scripts\external

reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f

powershell .\SetupExternalTasks.ps1 -tasksUrl "%EXTERNALTASKURL%" >> ExternalTasks.log 2>> ExternalTasks_err.log

REM powershell .\SetupExternalTasks.ps1 -tasksUrl "%EXTERNALTASKURL%" >> ExternalTasks.log 2>> ExternalTasks_err.log

:SKIP
EXIT /B 
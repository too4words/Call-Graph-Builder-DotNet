if "%EMULATED%"=="true" goto SKIP

set Source=https://orleanstorage2.blob.core.windows.net/project-files 
REM https://orleansdatastorage.blob.core.windows.net/project-files 
set SourceKey=ilzOub7LFk5zQ7drJFkfoxdwN1rritlSWAJ9Vl35g/TG4rZWxCXWNTJV20vZLTL/D2LK065cG8AozDg8CGOKQQ==
REM w1Ue+xPB9xEZt5A1WzXWjXNUtZ1pSkykC1VtbJElMmCjK4bmkrLCtiZ4k0N7o8IEzHWDAp4kn0P+X+HhqsttdQ== 

goto START

:NOSKIP

set Source=http://127.0.0.1:10000/devstoreaccount1/project-files
set SourceKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==

:START

set TEMP=c:\Temp

md c:\temp

cd %ROLEROOT%\approot\scripts
md %ROLEROOT%\approot\scripts\external

ECHO Root is: %RoleRoot% >> "%TEMP%\StartupLog.txt" 2>&1

REM Check if VS was INSTALLED

IF EXIST "%RoleRoot%\NET46_Success.txt" (
   ECHO NET is already installed. Exiting. >> "%TEMP%\StartupLog.txt" 2>&1
    GOTO SKIP
)

REM  We add a share to access solution files
REM net use X: \\orleansstorage2.file.core.windows.net\solutions  /u:orleansstorage2  ilzOub7LFk5zQ7drJFkfoxdwN1rritlSWAJ9Vl35g/TG4rZWxCXWNTJV20vZLTL/D2LK065cG8AozDg8CGOKQQ==
net use X: \\orleansstoragedg.file.core.windows.net\solutions  /u:orleansstoragedg 0up2Sc/EYfYVeP0Hueim/bUSh63Jqdt/LCQTA0jPKX+KNtSNh1LnJdB0ODD3OnTVXMbqe+NQRZkE0mGuXpgi4Q== 


ECHO "Check mount X:" "%TEMP%\StartupLog.txt" 2>&1

net use >> "%TEMP%\StartupLog.txt" 2>&1


ECHO NET46 in %NETSOURCEDIR% >> "%TEMP%\StartupLog.txt" 2>&1

X:
CD %NETSOURCEDIR% 

START /WAIT  NDP46-KB3045557-x86-x64-AllOS-ENU.exe /q /Log "%TEMP%\net46-log.txt"

IF %ERRORLEVEL% EQU 0 (
  REM   The application installed without error. Create a file to indicate that the application 
  REM   does not need to be installed again.

  ECHO This line will create a file to indicate that VS 2015 installed correctly. > "%RoleRoot%\NET46_Success.txt"
) ELSE (
  REM   An error occurred. Log the error and exit with the error code.

  DATE /T >> "%TEMP%\StartupLog.txt" 2>&1
  TIME /T >> "%TEMP%\StartupLog.txt" 2>&1
  ECHO  An error occurred installing Application 1. Errorlevel = %ERRORLEVEL%. >> "%TEMP%\StartupLog.txt" 2>&1
  EXIT %ERRORLEVEL%
)

REM reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f
REM powershell Restart-Computer

:SKIP
EXIT /B 0
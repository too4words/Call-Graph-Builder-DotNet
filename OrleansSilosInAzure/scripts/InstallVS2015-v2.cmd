if "%EMULATED%"=="true" goto SKIP

ECHO Starting main script > c:\temp\sal.txt



ECHO Trying to create task >> c:\temp\sal.txt

net user scheduser Secr3tC0de /add
net localgroup Administrators scheduser /add
 
schtasks /CREATE /TN "Install-VS" /SC ONCE /SD 01/01/2020 /ST 00:00:00 /RL HIGHEST /RU scheduser /RP Secr3tC0de /TR "%ROLEROOT%\approot\scripts\InstallVS2015.cmd %ROLEROOT%" /F

ECHO Running task  >> c:\temp\sal.txt
schtasks /RUN /TN "Install-VS"

ECHO Finish main script >> c:\temp\sal.txt

:SKIP
EXIT /B 0
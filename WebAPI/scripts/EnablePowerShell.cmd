
REM *********************************************************
REM 
REM     Copyright (c) Microsoft. All rights reserved.
REM     This code is licensed under the Microsoft Public License.
REM     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
REM     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
REM     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
REM     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
REM 
REM *********************************************************

REM Check if the script is running in the Azure Emulator, and if so, do not run
ECHO Enabling the execution of powershell scripts

IF "%IsEmulated%"=="true" goto :EOF 

reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f

md c:\temp
icacls c:\temp /grant Everyone:F

Exit /b 

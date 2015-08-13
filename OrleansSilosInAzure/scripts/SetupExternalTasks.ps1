#
# SetupExternalTask.ps1
#
 param (
    [string]$tasksUrl = $(throw "-taskUrl is required."),
    [string]$localFolder = ""
 )


 # Check a registry path
function Test-RegistryValue {

	param (
	 [parameter(Mandatory=$true)]$Path
	)

	try {
	Get-Item -Path $Path -ErrorAction Stop
	return $true
	}

	catch {

	return $false

	}

}

# Function to unzip file contents
function Expand-ZIPFile($file, $destination)
{
    $shell = new-object -com shell.application
    $zip = $shell.NameSpace($file)
    foreach($item in $zip.items())
    {
        # Unzip the file with 0x14 (overwrite silently)
        $shell.Namespace($destination).copyhere($item, 0x14)
    }
}

# Function to write a log
function Write-Log($message) {
    $date = get-date -Format "yyyy-MM-dd HH:mm:ss"
    $content = "`n$date - $message"
    Add-Content $localfolder\SetupExternalTasks.log $content
	Add-Content c:\Temp\sal.txt $content
}

# Add-Content C:\Temp\sal.txt "Executing script" 

## if ($tasksUrl -eq "") {
##    exit
##  }

if ($localFolder -eq "") {
    $localFolder = "$pwd\External"
}

# Create folder if does not exist
Write-Log "Creating folder $localFolder"
New-Item -ItemType Directory -Force -Path $localFolder
cd $localFolder

#####################


 # To install Build tools. 
 ## Write-Log "Installing Build tools"
 ## $file = "$localFolder\BuildTools_Full.exe"
 ## & $file /Q
 ## Write-Log "Finished installing Build tools"
cd .. 


Write-Log "Mounting share on X:"

# save account pass to reconnect
#cmdkey /add:orleansstorage2.file.core.windows.net /user:orleansstorage2 /pass:ilzOub7LFk5zQ7drJFkfoxdwN1rritlSWAJ9Vl35g/TG4rZWxCXWNTJV20vZLTL/D2LK065cG8AozDg8CGOKQQ==

# We add a share to access solution files
net use X: \\orleansstorage2.file.core.windows.net\solutions  /u:orleansstorage2  ilzOub7LFk5zQ7drJFkfoxdwN1rritlSWAJ9Vl35g/TG4rZWxCXWNTJV20vZLTL/D2LK065cG8AozDg8CGOKQQ==

#net use z: \\orleansstorage2.file.core.windows.net\solutions /u:orleansstorage2  ilzOub7LFk5zQ7drJFkfoxdwN1rritlSWAJ9Vl35g/TG4rZWxCXWNTJV20vZLTL/D2LK065cG8AozDg8CGOKQQ== /persistent:yes

Write-Log "Check mount X:"

net use >> C:\temp\sal.txt

Write-Log "Checking if VS15 is installed "

##$vs = Test-RegistryValue 'HKLM:\Software\Microsoft\VisualStudio\14.0'
$vs = Test-Path c:\vs2015

$dorestart = $false;

if($vs -eq $false)
{
	Write-Log "Installing VS"

	# we need to install VS
	$vsdir = 'X:\IDEinstall\VS15-parts'
	$vsinstall = './vs_enterprise.exe' 
	pushd $vsdir 
	Write-Log "current dir:" + $pwd
	Write-Log "Installing VS"
	Start-Process powershell -verb runas -ArgumentList '.\vs_enterprise.exe /noweb /NoRefresh /silent /norestart /CustomInstallPath c:\vs2015 /Log c:\temp\vs15-log.txt' -Wait
	# & $vsinstall /noweb /passive /norestart /NoRefresh /CustomInstallPath c:\vs2015 /Log c:\temp\vs15-log.txt
	Write-Log "Finished installing VS"
	popd
	$dorestart = $true;
}
else
{
	Write-Log "VS was already installed"
}

write-Log "unmounting share on X:"
net use x: /delete

Write-Log "Finish statup"


# Now, we run azure copy to copy 
#cmd.exe /C RunAzureCopy.cmd
# Write-Log "Blob copy to local folder"
 
if($dorestart -eq $true)
{
	Restart-Computer
}
#
# SetupExternalTask.ps1
#
 param (
#    [string]$tasksUrl = $(throw "-taskUrl is required."),
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

$msbuild64 = "$env:SystemDrive\Program Files\MSBuild\14.0"
$msbuild32 =  "$env:SystemDrive\Program Files (x86)\MSBuild\14.0"

$isms64 = Test-Path $msbuild64 
$isms32 = Test-Path $msbuild32

Write-Log "$msbuild64" 
Write-Log "$msbuild32" 

Write-Log "isms64 $isms64 " 
Write-Log "isms32 $isms32" 

if( ($isms64 -eq $false) -and  ($isms32 -eq $false))
{
 # To install Build tools. 
 Write-Log "Installing Build tools"
 $file = "$localFolder\BuildTools_Full.exe"
 & $file /Q
 Write-Log "Finished installing Build tools"
}
else 
{
	Write-Log "Build tools already installed"
}

 cd .. 


Write-Log "Finish startup"


# Now, we run azure copy to copy 
#cmd.exe /C RunAzureCopy.cmd
# Write-Log "Blob copy to local folder"
 
if($dorestart -eq $true)
{
	Restart-Computer
}
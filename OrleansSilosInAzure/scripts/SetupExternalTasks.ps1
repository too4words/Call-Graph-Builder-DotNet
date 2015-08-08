#
# SetupExternalTask.ps1
#
 param (
    [string]$tasksUrl = $(throw "-taskUrl is required."),
    [string]$localFolder = ""
 )

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

$file = "$localFolder\ExternalTasks.cmd"

 if ($tasksUrl.ToLower().EndsWith(".zip")) {
    $file = "$localFolder\ExternalTasks.zip"
 }
 if ($tasksUrl.ToLower().EndsWith(".ps1")) {
    $file = "$localFolder\ExternalTasks.ps1"
 }

 if ($tasksUrl.ToLower().EndsWith(".exe")) {
    $file = "$localFolder\ExternalTasks.exe"
 }

 $file = "$localFolder\BuildTools_Full.exe"

# Download the tasks file
### Deleting this ####
## Write-Log "Downloading external file $file"
## $webclient = New-Object System.Net.WebClient
## $webclient.DownloadFile($tasksUrl,$file)

## Write-Log "Download completed"


#####################

# If the tasks are zipped, unzip them first
 if ($tasksUrl.ToLower().EndsWith(".zip")) {
    Write-Log "Unzipping $localFolder\ExternalTasks.zip"
    Expand-ZIPFile -file "$localFolder\ExternalTasks.zip" -destination $localFolder
    Write-Log "Unzip completed"

    # When a .zip file is specied, only files called "Task???.cmd" and "Task???.ps1" will be executed
    # This allows to include assemblies and other file dependencies in the zip file
    Get-ChildItem $localFolder | Where-Object {$_.Name.ToLower() -match "task[0-9][0-9][0-9].[cmd|ps1]"} | Sort-Object $_.Name | ForEach-Object {
        Write-Log "Executing $localfolder\$_"        
        if ($_.Name.ToLower().EndsWith(".ps1")) {
            powershell.exe "$localFolder\$_"
        }
        elseif ($_.Name.ToLower().EndsWith(".cmd")) {
            cmd.exe /C "$localFolder\$_"
        }
    }
 }
 elseif ($tasksUrl.ToLower().EndsWith(".ps1")) {
    powershell.exe $file
 }
 elseif ($tasksUrl.ToLower().EndsWith(".cmd")) {
    cmd.exe /C $file
 }
  elseif ($tasksUrl.ToLower().EndsWith(".exe")) {
	Add-Content C:\Temp\sal.txt "Executing install"
    powershell.exe $file
 }

 Add-Content C:\Temp\sal.txt "Executing install"
 Add-Content C:\Temp\sal.txt $file
 & $file /Q

 Write-Log "External tasks execution finished"
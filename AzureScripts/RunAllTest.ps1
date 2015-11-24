#
#
param (
	[string] $drive = "Y",
#	[string] $className = "C",
#	[string] $assemblyName = "ConsoleApplication",
#    [string] $methodPrefix = "N",
	[string] $rootKind = "Default",
	[int] $repetitions = 50
	)


function Check_Instances_Ready($service)
{
	$NonReadyInstances = (Get-AzureDeployment $service -Slot Production).RoleInstanceList | Where-Object { $_.InstanceStatus -ne "ReadyRole" } | ft -Property RoleName, InstanceName, InstanceStatus  
	return ($NonReadyIntances -eq $null)
}

function Check_Deplyoment_Status($service)
{
	$status = (Get-AzureDeployment $service -Slot Production).Status
	return $status
}



function Wait_Instances_Ready($timeoutMin)
{
	$timeout = new-timespan -Minutes $timeoutMin
	$sw = [diagnostics.stopwatch]::StartNew()
	while ($sw.elapsed -lt $timeout){
		if (Check_Instances_Ready("orleansservicedg"))
		{
			if(Check_Deplyoment_Status("orleansservicedg") -eq "Running") 
			{
				Write-host "Instances are Ready!"
				return $true
			}
		}
		Write-host "Instances not ready yet! Waiting..."
		start-sleep -seconds 2
	}
 
	write-host "Timed out"
	return $false
}

$ErrorActionPreference = "Stop"

# get the paths to use in the invocations
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
#Write-Host "InvocationName:" $MyInvocation.InvocationName
#Write-Host "Path:" $MyInvocation.MyCommand.Path
#Write-Host "ScripPath:" $ScriptPath 

#$machinesSet = 1
#$machinesSet = 32
#$machinesSet = 32,16,8,4,2,1
$machinesSet = 16

# this is the time in seconds for the wait before starting to run experiments
$waitTime = 3
# this is the maximum time (in minutes) we wait the instances to be ready after changes the number of instances
$instancesTime = 5
foreach($machines in $machinesSet)
{
#	Write-Host "Changing instances to :" $machines
	$result1 = & "$ScriptPath\ChangeNumberOfInstances.ps1" -InstanceCount $machines

	Write-Host "Waiting instances to be ready" 
	$result1 = Wait_Instances_Ready($instancesTime)

#	Write-Host "Stopping and starting cloud servce" 
#	$result1 = & "$ScriptPath\Stop-Start-CloudService.ps1" -InstanceCount $machines

	Write-Host "Waiting :" $waitTime "seconds... to run tests" 
	Start-Sleep -s $waitTime
	
	Write-Host "Analizing all test in:" $machines " instances"
    # $result2 = & "$ScriptPath\RunAllTestsForInstance.ps1" -drive $drive  -machines $machines -className $className -assemblyName $assemblyName -methodPrefix $methodPrefix -repetitions $repetitions  | Write-Output
    $result2 = & "$ScriptPath\RunAllTestsForInstance.ps1" -drive $drive -machines $machines -repetitions $repetitions -rootKind $rootKind | Write-Output
	Write-Host "Result:" $result2
}

Write-Host "Done" 
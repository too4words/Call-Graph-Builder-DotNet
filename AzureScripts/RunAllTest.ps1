#
#
param (
	[string] $drive = "Y",
	[string] $className = "C",
	[string] $assemblyName = "ConsoleApplication",
    [string] $methodPrefix = "N",
	[int] $repetitions = 50
	)

# get the paths to use in the invocations
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
#Write-Host "InvocationName:" $MyInvocation.InvocationName
#Write-Host "Path:" $MyInvocation.MyCommand.Path
#Write-Host "ScripPath:" $ScriptPath 

$machinesSet = 2,1 
#$machinesSet = 16,8,4,2,1 

$waitTime = 5
foreach($machines in $machinesSet)
{
	Write-Host "Changing instances to :" $machines 
	$result1 = & "$ScriptPath\ChangeNumberOfInstances.ps1" -InstanceCount $machines
	Write-Host "Waiting :" $waitTime "seconds..." 
	Start-Sleep -s $waitTime
	Write-Host "Analizing all test in:" $machines " instances"
	$result2 = & "$ScriptPath\RunAllTestsForInstance.ps1" -drive $drive  -machines $machines -className $className -assemblyName $assemblyName -methodPrefix $methodPrefix -repetitions $repetitions  | Write-Output

	Write-Host "Result:" $result2
}

Write-Host "Done" 
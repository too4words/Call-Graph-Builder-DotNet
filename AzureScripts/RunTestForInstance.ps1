#
#
param (
	[string] $drive = "Y",
    [string] $solutionPath = "LongTest2",
	[string] $solutionName = "LongTest2",
	[int] $machines = 2,
#	[int] $numberOfMethods = 100,
#	[string] $className = "C",
#	[string] $assemblyName = "ConsoleApplication",
#    [string] $methodPrefix = "N",
	[int] $repetitions = 50,
	[string] $rootKind = "Default",
    [string] $expID = ""
	)

# get the paths to use in the invocations
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
#Write-Host "InvocationName:" $MyInvocation.InvocationName
#Write-Host "Path:" $MyInvocation.MyCommand.Path
#Write-Host "ScripPath:" $ScriptPath 

Write-Host "Invoke test:"  $solutionName
#$job = Start-Job { 
	$result1 = & "$ScriptPath\InvokeSolutionExperiment.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -numberOfMethods $numberOfMethods -expID $expID -rootKind $rootKind| Write-Output
#}
#Wait-Job $job
#Receive-Job $job
Write-Host "Result:" $result1

echo "Invoke query"
#$job = Start-Job { 
	#$result2 = & "$ScriptPath\InvokeRandomQueries.ps1" -className $className -methodPrefix $methodPrefix -machines $machines -numberOfMethods $numberOfMethods -repetitions $repetitions -assemblyName $assemblyName -expID $solutionName | Write-Output
	$result2 = & "$ScriptPath\InvokeRandomQueries.ps1" -machines $machines -repetitions $repetitions -expID $expID | Write-Output
#}
#Wait-Job $job
#Receive-Job $job
Write-Host "Result:" $result2

echo "Clean grain"
$result3 = & "$ScriptPath\InvokeGrainDeactivation.ps1"
$result4 = & "$ScriptPath\InvokeCmd.ps1" -command RemoveGrainState

Write-Host "Result:" $result3 $result4
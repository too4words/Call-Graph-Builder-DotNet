#
#
param (
	[string] $drive = "Y",
    [string] $solutionPath = "LongTest2",
	[string] $solutionName = "LongTest2",
	[int] $machines = 2,
	[int] $numberOfMethods = 100,
	[string] $className = "C",
	[string] $namespaceName = "ConsoleApplication",
    [string] $methodPrefix = "N",
	[int] $repetitions = 50
	)

# get the paths to use in the invocations
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
Write-Host "InvocationName:" $MyInvocation.InvocationName
Write-Host "Path:" $MyInvocation.MyCommand.Path
Write-Host "ScripPath:" $ScriptPath 

echo "Invoke test"
#$job = Start-Job { 
	$result1 = & "$ScriptPath\InvokeSolutionExperiment.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -numberOfMethods $numberOfMethods | Write-Output
#}
#Wait-Job $job
#Receive-Job $job
Write-Host "Result:" $result1

echo "Invoke query"
#$job = Start-Job { 
	$result2 = & "$ScriptPath\InvokeRandomQueries.ps1" -className $className -methodPrefix $methodPrefix -machines $machines -numberOfMethods $numberOfMethods -repetitions $repetitions -namespaceName $namespaceName -expID $solutionName | Write-Output
#}
#Wait-Job $job
#Receive-Job $job
Write-Host "Result:" $result2

echo "Clean grain"
$result3 = & "$ScriptPath\InvokeGrainDeactivation.ps1"
Write-Host "Result:" $result3
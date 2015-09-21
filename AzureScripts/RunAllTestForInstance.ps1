#
#
param (
	[string] $drive = "Y",
	[int] $machines = 2,
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

echo "Invoke test iteration LongTest2"

$solutionPath = "LongTest2"
$solutionName = "LongTest2"
$numberOfMethods = 100
$result1 = & "$ScriptPath\RunTestForInstance.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -numberOfMethods $numberOfMethods | Write-Output
Write-Host "Result:" $result1

$solutionPath = "LongTest3"
$solutionName = "LongTest3"
$numberOfMethods = 1000
$result2 = & "$ScriptPath\RunTestForInstance.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -numberOfMethods $numberOfMethods | Write-Output
Write-Host "Result:" $result2

$solutionPath = "LongTest4"
$solutionName = "LongTest4"
$numberOfMethods = 10000
$result3 = & "$ScriptPath\RunTestForInstance.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -numberOfMethods $numberOfMethods | Write-Output
Write-Host "Result:" $result3
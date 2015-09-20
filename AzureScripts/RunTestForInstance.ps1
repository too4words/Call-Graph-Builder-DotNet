#
#
param (
	[string] $drive = "Y",
    [string] $solutionPath = "LongTest2",
	[string] $solutionName = "LongTest2",
	[int] $machines ="1",
	[int] $numberOfMethods ="100",
	[string] $className = "C",
    [string] $methodPrefix = "N",
	[int] $repetitions ="50"
	)

# get the paths to use in the invocations
$ScriptPath = Split-Path $MyInvocation.InvocationName

echo invoke test
& "$ScriptPath\InvokeSolutionExperiment.ps1 -drive $drive -solutionPath=$solutionPath -solutionName=$solutionName -machines=$machines -numberOfMethods=$numberOfMethods"
echo invoke query
& "$ScriptPath\InvokeRandomQueries.ps1 -className $className - methodPrefix $methodPrefix-machines $machines -numberOfMethods $numberOfMethods -repetitions repetitions -expID $solutionName"
echo clean grain
& "$ScriptPath\InvokeGrainDeactivation.ps1"

#
#
param (
	[string] $drive = "Y",
	[int] $machines = 2,
#	[string] $className = "C",
#	[string] $assemblyName = "ConsoleApplication",
#    [string] $methodPrefix = "N",
	[int] $repetitions = 50
	)

$solutionPaths = @("synthetic100","synthetic1000","synthetic10000")
$solutionNames = @("test","test","test")
$expIDs= @("test100","test1000","test10000")

#$solutionPaths = @("synthetic100","synthetic1000")
#$solutionNames = @("test","test")

# get the paths to use in the invocations
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
#Write-Host "InvocationName:" $MyInvocation.InvocationName
#Write-Host "Path:" $MyInvocation.MyCommand.Path
#Write-Host "ScripPath:" $ScriptPath 
Write-Host "Machines: " $machines

for ($i=0; $i -lt $solutionNames.length; $i++) {
	$solutionPath = $solutionPaths[$i]
    $solutionName = $solutionNames[$i]
    $expID = $solutionName

    echo "Invoke analysis on" + $expID

    #$solutionPath = "LongTest1"
    #$solutionName = "LongTest1"
    #$numberOfMethods = 10
    #$result1 = & "$ScriptPath\RunTestForInstance.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -numberOfMethods $numberOfMethods -assemblyName $assemblyName | Write-Output
    $result1 = & "$ScriptPath\RunTestForInstance.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -repetitions $repetitions -expID $expID| Write-Output

    Write-Host "Result:" $result1
}

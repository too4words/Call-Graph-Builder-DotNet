#
#
param (
	[string] $drive = "Y",
	[int] $machines = 2,
#	[string] $className = "C",
#	[string] $assemblyName = "ConsoleApplication",
#    [string] $methodPrefix = "N",
	[string] $rootKind = "Default",
	[int] $repetitions = 100

	)

# synthetic solutions

#$solutionPaths = @("LongTest1", "new\newer\synthetic-100","new\newer\synthetic-1000","new\newer\synthetic-10000") #,"new\synthetic-100000")
#$solutionNames = @("Longtest1", "test","test","test") #,"test")
#$expIDs= @("test1","s100","s1000","s10000") #,"s100000")

#$solutionPaths = @("LongTest1", "new\newer\synthetic-1000000-100p")
#$solutionNames = @("Longtest1", "test")
#$expIDs= @("test01", "test1000000")

#$solutionPaths = @("LongTest1", "new\newer\synthetic-10000000-100p")
#$solutionNames = @("Longtest1", "test")
#$expIDs= @("test01", "test10000000")

#$solutionPaths = @("new\newer\synthetic-10000")
#$solutionNames = @("test")
#$expIDs= @("test10000d")

#$solutionPaths = @("new\synthetic-100000")
#$solutionNames = @("test")
#$expIDs= @("test100000d")

# real solutions

#$solutionPaths = @("LongTest1", "azure-powershell\src")
#$solutionNames = @("LongTest1", "ResourceManager.ForRefactoringOnly")
#$expIDs= @("test-1", "azure-powershell")

$solutionPaths = @("LongTest1", "ShareX")
$solutionNames = @("LongTest1", "ShareX")
$expIDs= @("test-1", "ShareX")

#$solutionPaths = @("LongTest1", "ILSpy")
#$solutionNames = @("LongTest1", "ILSpy")
#$expIDs= @("test-1", "ILSpy")

#$solutionPaths = @("LongTest1", "EntityFramework")
#$solutionNames = @("LongTest1", "EntityFramework")
#$expIDs= @("test-1", "EntityFramework")


# get the paths to use in the invocations
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
#Write-Host "InvocationName:" $MyInvocation.InvocationName
#Write-Host "Path:" $MyInvocation.MyCommand.Path
#Write-Host "ScripPath:" $ScriptPath 
Write-Host "Machines: " $machines

for ($i=0; $i -lt $solutionNames.length; $i++) {
	$solutionPath = $solutionPaths[$i]
    $solutionName = $solutionNames[$i]
    $expID =$expIDs[$i]

    echo "Invoke analysis on" + $expID

    #$solutionPath = "LongTest1"
    #$solutionName = "LongTest1"
    #$numberOfMethods = 10
    #$result1 = & "$ScriptPath\RunTestForInstance.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -numberOfMethods $numberOfMethods -assemblyName $assemblyName | Write-Output
    $result1 = & "$ScriptPath\RunTestForInstance.ps1" -drive $drive -solutionPath $solutionPath -solutionName $solutionName -machines $machines -repetitions $repetitions -expID $expID -rootKind $rootKind | Write-Output

    Write-Host "Result:" $result1
}

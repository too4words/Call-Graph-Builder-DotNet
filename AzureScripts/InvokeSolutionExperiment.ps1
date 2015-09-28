#
#
param (
	[string] $drive = "Y",
    [string] $solutionPath = "LongTest2",
	[string] $solutionName = "LongTest2",
	[int] $machines = 1,
	[int] $numberOfMethods = 100
	)

# http://orleansservicedg.cloudapp.net:8080/api/Experiments?drive=Y&solutionPath=LongTest2&solutionName=LongTest2&machines=1

if($env:ISEMULATED -ne $true)  {
	$port = "45002"
	$resource = "http://orleansservicedg.cloudapp.net:"+$port+"/"
}
else
{
	$resource = "http://localhost:49176/"
	$drive = "C"
	$solutionPath = "\Users\diegog\Source\Repos\Call-Graph-Builder-DotNet\TestsSolutions\" + $solutionPath
}
$controler="api/Experiments"
$cmd = "?drive="+$drive+"&solutionPath="+$solutionPath+"&solutionName="+$solutionName+"&machines="+$machines <#+"&numberOfMethods="+$numberOfMethods#>
$uri = $resource+$controler+$cmd
Write-Host "Invoking"  $uri
Invoke-WebRequest -Uri $uri -TimeoutSec 5000 
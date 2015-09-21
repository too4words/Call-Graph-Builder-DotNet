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
	$resource = "http://orleansservicedg.cloudapp.net:8080/"
}
else
{
	$resource = "http://localhost:49176/"
	$drive = "C"
	$solutionPath = "\Users\diegog\Source\Repos\Call-Graph-Builder-DotNet\TestsSolutions\LongTest2"
}
$controler="api/Experiments"
$cmd = "?drive="+$drive+"&solutionPath="+$solutionPath+"&solutionName="+$solutionName+"&machines="+$machines <#+"&numberOfMethods="+$numberOfMethods#>
$uri = $resource+$controler+$cmd
echo Invoking $uri
Invoke-WebRequest -Uri $uri 
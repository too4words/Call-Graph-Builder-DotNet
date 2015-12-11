#
#
param (
    [string] $testName = "LongGeneratedTest1",
	[int] $numberOfMethods = 10,
	[int] $machines = 1,
	[string] $expID = "dummy"
	)
if($env:ISEMULATED -ne $true)  {
	$port = "45002"
	$resource = "http://orleansservicedg.cloudapp.net:"+$port+"/"
}
else
{
	$resource = "http://localhost:49176/"
}
$controler="api/Experiments"
$cmd = "?testName="+$testName+"&machines="+$machines+"&numberOfMethods="+$numberOfMethods+"&expID="+$expID
$uri = $resource+$controler+$cmd
Invoke-WebRequest -Uri $uri 
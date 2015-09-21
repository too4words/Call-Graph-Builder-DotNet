﻿#
#
param (
	[string] $className = "C",
    [string] $methodPrefix = "N",
	[string] $assemblyName = "ConsoleApplication",
	[int] $machines = 1,
	[int] $numberOfMethods = 100,
	[int] $repetitions = 50,
	[string] $expID = "dummy"
	)

# http://orleansservicedg.cloudapp.net:8080/api/Experiments?className=C&methodPrefix=N&&machines=1&numberOfMethods=10&repetitions=50
# ComputeQueries(string className, string methodPrefix, int machines, int numberOfMethods, int repetitions)

if($env:ISEMULATED -ne $true)  {
	$resource = "http://orleansservicedg.cloudapp.net:8080/"
}
else
{
	$resource = "http://localhost:49176/"
}
$controler="api/Experiments"
$cmd = "?className="+$className+"&methodPrefix="+$methodPrefix+"&machines="+$machines+ "&numberOfMethods="+$numberOfMethods+"&repetitions="+$repetitions+"&assemblyName="+$assemblyName+"&expID="+$expID
$uri = $resource+$controler+$cmd
echo Invoking $uri
Invoke-WebRequest -Uri $uri 
#
#
param (
    [string] $testName = "LongGeneratedTest1"
	)
$resource = "http://orleansservice.cloudapp.net:8080/"
$controler="api/Experiments"
$cmd = "?testName="+$testName+"&machines=2&numberOfMethods=10"
$uri = $resource+$controler+$cmd
Invoke-WebRequest -Uri $uri 
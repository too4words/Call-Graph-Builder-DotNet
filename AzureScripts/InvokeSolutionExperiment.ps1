#
#
param (
	[string] $drive = "Y",
    [string] $solutionPath = "LongTest2",
	[string] $solutionName = "LongTest2",
	[int] $machines ="1",
	[int] $numberOfMethods ="10"
	)

# http://orleansservicedg.cloudapp.net:8080/api/Experiments?drive=Y&solutionPath=LongTest2&solutionName=LongTest2&machines=1

$resource = "http://orleansservicedg.cloudapp.net:8080/"
$controler="api/Experiments"
$cmd = "?drive="+$drive+"&solutionPath="+$solutionPath+"&solutionName="+$solutionName+"&machines="+$machines <#+"&numberOfMethods="+$numberOfMethods#>
$uri = $resource+$controler+$cmd
echo Invoking $uri
Invoke-WebRequest -Uri $uri 
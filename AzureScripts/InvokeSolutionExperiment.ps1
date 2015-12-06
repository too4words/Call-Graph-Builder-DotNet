#
#
param (
	[string] $drive = "Y",
    [string] $solutionPath = "LongTest2",
	[string] $solutionName = "LongTest2",
	[int] $machines = 1,
	[string] $expID= "",
	[string] $rootKind = "Default",
	[int] $numberOfMethods = 100
	)

function Check_Status($service)
{
	if($env:ISEMULATED -ne $true)  {
		$port = "45002"
		$resource = "http://orleansservicedg.cloudapp.net:"+$port+"/"
	}
	else
	{
			$resource = "http://localhost:45002/"

#		$resource = "http://localhost:49176/"
	}
	$controler="api/Experiments"
	$cmd = "?command=Status"
	$uri = $resource+$controler+$cmd
	$result = Invoke-WebRequest -Uri $uri
	return $result
}


function Wait_For_Ready($timeoutMin)
{
	$timeout = new-timespan -Minutes $timeoutMin
	$sw = [diagnostics.stopwatch]::StartNew()
	while ($sw.elapsed -lt $timeout){
		$status = Check_Status("orleansservicedg")
		Write-Host "Status" $status
		if ($status -match "Ready")
		{
			Write-host "Ready!"
			return $true
		
		}
		if ($status -match "Failed")
		{
			Write-host "Excecution Failed"
			return $true		
		}

		if ($status -match "None")
		{
			Write-host "Excecution finished abnormaly"
			return $true		
		}
		Write-host "Not ready yet! Waiting..."
		start-sleep -seconds 5
	}
 
	write-host "Timed out"
	return $false
}

# http://orleansservicedg.cloudapp.net:8080/api/Experiments?drive=Y&solutionPath=LongTest2&solutionName=LongTest2&machines=1

if($env:ISEMULATED -ne $true)  {
	$port = "45002"
	$resource = "http://orleansservicedg.cloudapp.net:"+$port+"/"
}
else
{
	$port = "45002"
	$resource = "http://localhost:45002/" 
	#"http://localhost:49176/"
#	$drive = "C"
#	$solutionPath = "\Users\dgarber\Desktop\" +$solutionPath
	#"\Users\diegog\Source\Repos\Call-Graph-Builder-DotNet\TestsSolutions\" + $solutionPath
}
$controler="api/Experiments"
$cmd = "?drive="+$drive+"&solutionPath="+$solutionPath+"&solutionName="+$solutionName+"&machines="+$machines+"&expID="+$expID+"&rootKind="+$rootKind <#+"&numberOfMethods="+$numberOfMethods#>
$uri = $resource+$controler+$cmd
Write-Host "Invoking"  $uri
Invoke-WebRequest -Uri $uri -TimeoutSec 5000
start-sleep -seconds 5
Wait_For_Ready(60*24)
 
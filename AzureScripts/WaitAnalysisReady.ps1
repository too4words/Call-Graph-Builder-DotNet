#
#
param (
	[int] $minutes = 60*24
	)

function Check_Status($service)
{
	if($env:ISEMULATED -ne $true)  {
		$port = "45002"
		$resource = "http://orleansservicedg.cloudapp.net:"+$port+"/"
	}
	else
	{
		$resource = "http://localhost:49176/"
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
		Write-host "Not ready yet! Waiting..."
		start-sleep -seconds 5
	}
 
	write-host "Timed out"
	return $false
}

Wait_For_Ready($minutes)
 
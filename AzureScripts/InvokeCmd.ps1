#
#
param (
	[string] $command = "Stats"
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
$cmd = "?command="+$command
$uri = $resource+$controler+$cmd
$result = Invoke-WebRequest -Uri $uri
Write-Host ($result.Content  -replace "\\n","`n")
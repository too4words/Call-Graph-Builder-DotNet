#
#
if($env:ISEMULATED -ne $true)  {
	$resource = "http://orleansservicedg.cloudapp.net:8080/"
}
else
{
	$resource = "http://localhost:49176/"
}
$controler="api/Experiments"
$cmd = "?command=Deactivate"
$uri = $resource+$controler+$cmd
Invoke-WebRequest -Uri $uri 
﻿#
#
if($env:ISEMULATED -ne $true)  {
	$port = "45002"
	$resource = "http://orleansservicedg.cloudapp.net:"+$port+"/"
}
else
{
	$resource = "http://localhost:49176/"
}
$controler="api/Experiments"
$cmd = "?command=Deactivate"
$uri = $resource+$controler+$cmd
Invoke-WebRequest -Uri $uri -TimeoutSec 5000
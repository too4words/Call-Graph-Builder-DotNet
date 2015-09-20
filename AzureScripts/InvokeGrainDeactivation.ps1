#
#

$resource = "http://orleansservicedg.cloudapp.net:8080/"
$controler="api/Experiments"
$cmd = "?command=Deactivate"
$uri = $resource+$controler+$cmd
Invoke-WebRequest -Uri $uri 
#
#

$resource = "http://orleansservice.cloudapp.net:8080/"
$controler="api/Experiments"
$cmd = ""
$uri = $resource+$controler+$cmd
Invoke-WebRequest -Uri $uri 
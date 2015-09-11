#
#
$resource = "http://orleansservice.cloudapp.net/"
$controler="api/Orleans"
$cmd = "?testName=LongTest1&machines=2&numberOfMethods=10"
$uri = $resource+$controler+$cmd
Invoke-WebRequest -Uri $uri 
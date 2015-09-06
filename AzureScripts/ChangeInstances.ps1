 import-module .\Update-CloudServiceScale.psm1

$SubscriptionId = "fea3a3c9-c4a3-4743-9835-1502a54705e9"

$SubscriptionName = "Internal Consumption"

$PfxFilePath =  ".\azure-internal-consumption.pfx"

$PfxPassword= "Diego2015Ben$"

$ServiceName = "orleansservice"

$Slot= "Production"

$InstanceCount = $1


Update-CloudServiceScale -SubscriptionID $SubscriptionId -SubscriptionName $SubscriptionName
-PfxFilePath $PfxFilePath -PfxPassword $PfxPassword -ServiceName $ServiceName -Slot $SLot
-InstanceCount $InstanceCount

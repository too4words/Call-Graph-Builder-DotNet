import-module .\Update-CloudServiceScale.psm1

$SubscriptionId = "fea3a3c9-c4a3-4743-9835-1502a54705e9"
$SubscriptionName = "Internal Consumption"

## MSR Subscription 
##$SubscriptionId = "6412f21b-b221-49c4-8728-96719adc2306"
##$SubscriptionName = "Ben Livshits"


# This is the file with a certificaque of the subscription 
# Maybe there is a way to avoid this by logging in azure before
$PfxFilePath =  ".\azure-internal-consumption.pfx"
$PfxPassword= "Diego2015Ben$"

# We Need to create a pfx for Ben's subscripiton

$ServiceName = "orleansservice"

$Slot= "Production"

# The number of Workers(Silos) 
$InstanceCount = $1

Update-CloudServiceScale.psm1 -SubscriptionID $SubscriptionId -SubscriptionName $SubscriptionName -PfxFilePath $PfxFilePath -PfxPassword $PfxPassword -ServiceName $ServiceName -Slot $SLot -InstanceCount $InstanceCount


##
## This script is used to change the number of Worker Roles
## You need to be logged to azure in order to run this script
## Al alternative option, that allows automatic connection, is to provide a certificate 
##


$SubscriptionId = "fea3a3c9-c4a3-4743-9835-1502a54705e9"
$SubscriptionName = "Internal Consumption"

## MSR Subscription 
##$SubscriptionId = "6412f21b-b221-49c4-8728-96719adc2306"
##$SubscriptionName = "Ben Livshits"


Add-AzureAccount

## Get-AzureSubscription

Select-AzureSubscription  $SubscriptionName 

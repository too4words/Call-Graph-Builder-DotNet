#
# This is used to for the service to start from scracth
# Requires to be loggued in azure
#

# This is the subscription when orleans service is deployed
Select-AzureSubscription "Microsoft Azure Sponsorship"

##Select-AzureSubscription  "Internal Consumption"
##Select-AzureSubscription  "Ben Livshits"

Stop-AzureService "orleansservicedg"
Start-AzureService "orleansservicedg"

##Stop-AzureService "orleansservice"
##Start-AzureService "orleansservice"

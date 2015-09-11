<#
.SYNOPSIS
    Scale Up/down an Azure cloud service in a given subscription.

.DESCRIPTION
    First check to see if the given slot of given cloud service is deployed already. If it is deployed, then change the instance count 
    of the slot to the given number. This applies to scaling down when the resource utilization is low; and also to scaling up when 
    the resource utlization is high. Slot could be "Staging" or "Production"

.EXAMPLE
    import-module .\Update-CloudServiceScale.psm1
    Update-CloudServiceScale -SubscriptionId "11111111-aaaa-bbbb-cccc-222222222222" `
        -SubscriptionName "My Test Subscription" -PfxFilePath C:\MyAbsolute.pfx -PfxPassword MyPassword -ServiceName MyServiceName
        -Slot "Staging"
#>

workflow Update-CloudServiceScale
{
    param(
       
                [parameter(Mandatory=$true)]
                [String]$SubscriptionId,

                [parameter(Mandatory=$true)]
                [String]$SubscriptionName,
	
				[parameter(Mandatory=$true)]
				[String]$PfxFilePath, 

                [parameter(Mandatory=$true)]
                [String]$PfxPassword,    
            
                # cloud service name for scale up/down
                [Parameter(Mandatory = $true)] 
                [String]$ServiceName,

                [Parameter(Mandatory = $true)]
                [String]$Slot,

                [Parameter(Mandatory = $true)]
                [String]$InstanceCount
    )

    # Check if Windows Azure Powershell is avaiable
    if ((Get-Module -ListAvailable Azure) -eq $null)
    {
        throw "Windows Azure Powershell not found! Please install from http://www.windowsazure.com/en-us/downloads/#cmd-line-tools"
    }

    $Start = [System.DateTime]::Now
    "Starting: " + $Start.ToString("HH:mm:ss.ffffzzz")

	<# Add this if you have a certificate#>
    $SecurePwd = ConvertTo-SecureString -String "$PfxPassword" -Force -AsPlainText
    $importedCert = Import-PfxCertificate -FilePath $PfxFilePath  -CertStoreLocation Cert:\CurrentUser\My  -Exportable  -Password $SecurePwd 
    $MyCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList($PfxFilePath, $SecurePwd, "Exportable")
	<##>

    inlinescript
    {
       Set-AzureSubscription -SubscriptionName "$using:SubscriptionName" -SubscriptionId $using:SubscriptionId -Certificate $using:MyCert 
       Select-Azuresubscription -SubscriptionName "$using:SubscriptionName" 

       $Deployment = Get-AzureDeployment -Slot $using:Slot -ServiceName $using:ServiceName
       if ($Deployment -ne $null -AND $Deployment.DeploymentId  -ne $null)
       {
           $Roles = Get-AzureRole -ServiceName $using:ServiceName -Slot $using:Slot

           foreach ($Role in $Roles)
           {
			   if($Role.RoleName -eq "OrleansSilosInAzure")
			   {
					$RoleDetails = Get-AzureRole -ServiceName $using:ServiceName -Slot $using:Slot -RoleName $Role.RoleName
					Write-Output (" {0} current " -f $Role.RoleName)
					$RoleDetails
					if ($RoleDetails.InstanceCount -eq $using:InstanceCount)
					{
						Write-Output ("Role {0} already has instance count {1}." -f $Role.RoleName, $using:InstanceCount)
					}else
					{
						Write-Output ("Role {0} changing instance count from {1} to {2}." -f $Role.RoleName, $RoleDetails.InstanceCount, $using:InstanceCount)
						Set-AzureRole -ServiceName $using:ServiceName -Slot $using:Slot -RoleName $Role.RoleName -Count $using:InstanceCount 
						Write-Output ("Role {0} changed instance count from {1} to {2}." -f $Role.RoleName, $RoleDetails.InstanceCount, $using:InstanceCount)
					}
			  }
           }
        }
    }
    
    $Finish = [System.DateTime]::Now
    $TotalUsed = $Finish.Subtract($Start).TotalSeconds
   
    Write-Output ("Updated cloud service {0} slot {1} to instance count {2} in subscription {3} in {4} seconds." -f $ServiceName, $Slot, $InstanceCount, $SubscriptionName, $TotalUsed)
    "Finished " + $Finish.ToString("HH:mm:ss.ffffzzz")
} 


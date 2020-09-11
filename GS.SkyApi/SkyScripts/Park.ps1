# Clear the console pane
Clear-Host

#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

Write-Host "Park Name:" $GS.ParkPosition
$GS.ParkPosition = "Home"
Write-Host "Park Name:" $GS.ParkPosition

$p = $GS.IsParked
Write-Host "IsParked:" $p
if($p -eq "True") {$GS.UnPark()}
$GS.Park()


# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
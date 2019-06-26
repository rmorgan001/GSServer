# Clear the console pane
Clear-Host

#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

Write-Host "PPEC On:" $GS.IsPpecOn
Write-Host "PPEC Training On:" $GS.IsPpecInTrainingOn
Write-Host "Support for Az/Eq:" $GS.CanAzEq
Write-Host "Support for Dual Encoders:" $GS.CanDualEncoders
Write-Host "Support for track at half speed:" $GS.CanHalfTrack
Write-Host "Support for polar LED:" $GS.CanPolarLed
Write-Host "Support for PPEC:" $GS.CanPpec
Write-Host "Support for CanWifi:" $GS.CanWifi



# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
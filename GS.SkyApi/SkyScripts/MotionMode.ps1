# Clear the console pane
Clear-Host

# GSS must be already running and connected to the mount
#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

$GS.AxisStop(1)

$GS.SetMotionMode(1,1,1) #G
$GS.SetGotoTargetIncrement(1,3) #H
$GS.SetStepSpeed(1,4)
$GS.StartMotion(1)

DO{
    $axis1SlewingTo = $GS.IsSlewing(1)
    Write-Host "Axis1 Position:" $GS.GetAxisPosition(1)

} while ($axis1SlewingTo -eq $true)

$GS.AxisStop(1)

# Write out position
Write-Host "Axis1 Position:" $GS.GetAxisPosition(1)

# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
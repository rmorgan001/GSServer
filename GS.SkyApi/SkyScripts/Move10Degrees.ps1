# Clear the console pane
Clear-Host

# GSS must be already running and connected to the mount
#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

# Necessary - Turn off GSS from processing ascom movements
# from other sources
$GS.AscomOn = $false;

# Stop both axis
$GS.AxisStop(1)
$GS.AxisStop(2)

# Write to console if bothe axis have stopped
Write-Host "Axis1 Stopped:" $GS.IsFullStop(1)
Write-Host "Axis2 Stopped:" $GS.IsFullStop(2)

# Write current positions of both axis
Write-Host "Axis1 Position:" $GS.GetAxisPosition(1)
Write-Host "Axis2 Position:" $GS.GetAxisPosition(2)

# Move axis1 to 10 degrees in positive direction
$GS.AxisGoToTarget(1,10)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)

} while ($axis1Stoppped -eq $false)

# Write out position
Write-Host "Axis1 Position:" $GS.GetAxisPosition(1)

# Move axis1 to -10 degrees in negative direction
$GS.AxisGoToTarget(1,-10)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)

} while ($axis1Stoppped -eq $false)

# Write out position
Write-Host "Axis1 Position:" $GS.GetAxisPosition(1)

# Move axis1 back to 0
$GS.AxisGoToTarget(1,0)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)

} while ($axis1Stoppped -eq $false)

# Write out position
Write-Host "Axis1 Position:" $GS.GetAxisPosition(1)

# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
# Clear the console pane
Clear-Host

# GSS must be already running and connected to the mount
#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

# Necessary - Turn off GSS from processing ascom movements
# from other sources
$GS.AscomOn = $false;

# Stop Axis 1
$GS.AxisStop(1)

# Move axis1 at a rate of 1 degree for 10 seconds
$GS.AxisSlew(1,1)
$timeout = new-timespan -Seconds 10
$sw = [diagnostics.stopwatch]::StartNew()
while ($sw.elapsed -lt $timeout){

    Write-Host "Axis1 Moving Position:" $GS.GetAxisPosition(1)
 
    start-sleep -seconds 1
}

# Stop axis
$GS.AxisStop(1)

# Wait a second to stop.  Must be stopped before issuing another move
start-sleep -seconds 1

# Write out position
Write-Host "Axis1 Finished Position:" $GS.GetAxisPosition(1)

# Move axis1 back to 0
$GS.AxisGoToTarget(1,0)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)

} while ($axis1Stoppped -eq $false)

# Write out position
Write-Host "Axis1 Home Position:" $GS.GetAxisPosition(1)

# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
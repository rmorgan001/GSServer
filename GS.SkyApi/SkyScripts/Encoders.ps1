# Clear the console pane
Clear-Host

# GSS must be already running and connected to the mount
#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

# Necessary - Turn off GSS from processing ascom movements
# from other sources
$GS.AscomOn = $false;

# Write out position
Write-Host "Axis 1 Position:" $GS.GetAxisPosition(1)

Write-Host "Encoder Count Axis 1:" $GS.GetEncoderCount(1)
Write-Host "Encoder Count Axis 2:" $GS.GetEncoderCount(2)

# Turn off encoders
$GS.SetEncoder(1,$false)
$GS.SetEncoder(2,$false)

# Move axis1 to 10 degrees in positive direction
$GS.AxisGoToTarget(1,10)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)

} while ($axis1Stoppped -eq $false)

# Write out position
Write-Host "Axis 1 Position:" $GS.GetAxisPosition(1)

# set new position as 0
$GS.SetAxisPosition(1,0)

# Write out position
Write-Host "Axis 1 Position:" $GS.GetAxisPosition(1)

# Move axis1 to 10 degrees in positive direction
$GS.AxisGoToTarget(1,10)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)

} while ($axis1Stoppped -eq $false)

Write-Host "Encoder Count Axis 1:" $GS.GetEncoderCount(1)
Write-Host "Encoder Count Axis 2:" $GS.GetEncoderCount(2)

# Write out position
Write-Host "Axis 1 Position:" $GS.GetAxisPosition(1)

# Move axis1 back to 0
$GS.AxisGoToTarget(1,0)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)

} while ($axis1Stoppped -eq $false)

Write-Host "Encoder Count Axis 1:" $GS.GetEncoderCount(1)
Write-Host "Encoder Count Axis 2:" $GS.GetEncoderCount(2)

# Move axis1 back to 0
$GS.AxisGoToTarget(1,-10)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)

} while ($axis1Stoppped -eq $false)

# set new position as 0
$GS.SetAxisPosition(1,0)

Write-Host "Encoder Count Axis 1:" $GS.GetEncoderCount(1)
Write-Host "Encoder Count Axis 2:" $GS.GetEncoderCount(2)

# Turn on encoders
$GS.SetEncoder(1,$true)
$GS.SetEncoder(2,$true)

# Write out position
Write-Host "Axis 1 Position:" $GS.GetAxisPosition(1)

# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
#Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass -Force;
#Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted -Force;

#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

# Necessary - Turn off GSS from processing ascom movements
# from other sources
$GS.AscomOn = $false;

For ($i=0; $i -le 10; $i++) {
    $GS.AxisStop(1)
    $GS.AxisStop(2)
}

# Is Axis stopped
Write-Host "Stopped:" $GS.IsFullStop(1)
#is ASCOM on
Write-Host "ASCOM on:" $GS.AscomOn

#move 10 degrees in positive direction
$GS.AxisGoToTarget(1,10)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)
    Write-Host $GS.GetAxisPosition(1)

} while ($axis1Stoppped -eq $false)

#move to 0 degrees
$GS.AxisGoToTarget(1,0)
DO{
    $axis1Stoppped = $GS.IsFullStop(1)
    Write-Host $GS.GetAxisPosition(1)

} while ($axis1Stoppped -eq $false)


#Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
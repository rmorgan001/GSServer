# Clear the console pane
Clear-Host

# GSS must be already running and connected to the mount
#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

# Necessary - Turn off GSS from processing ascom movements
# from other sources
$GS.AscomOn = $false;

Write-Host "Axis factor radian rate to int speed:" 
$rate = $GS.GetFactorRadRateToInt()
Write-Host "Axis 1 rate:" $rate[0]
Write-Host "Axis 2 rate:" $rate[1]

write-host "`n"

Write-Host "Axis high speed ratio:" 
$ratio = $GS.GetHighSpeedRatio()
Write-Host "Axis 1 rate:" $ratio[0]
Write-Host "Axis 2 rate:" $ratio[1]

write-host "`n"

Write-Host "Axis low speed margin:" 
$margin = $GS.GetLowSpeedGotoMargin()
Write-Host "Axis 1 rate:" $margin[0]
Write-Host "Axis 2 rate:" $margin[1]

write-host "`n"

Write-Host "Axis Ramp Down Range:" 
Write-Host "Axis 1 range:" $GS.GetRampDownRange(1)
Write-Host "Axis 2 range:" $GS.GetRampDownRange(2)

write-host "`n"

Write-Host "Axis Sidereal Speed Rate:" 
Write-Host "Axis 1 rate:" $GS.GetSiderealRate(1)
Write-Host "Axis 2 rate:" $GS.GetSiderealRate(2)

write-host "`n"

Write-Host "Axis Steps Per Revolution:"
$steps = $GS.GetStepsPerRevolution() 
Write-Host "Axis 1 steps:" $steps[0]
Write-Host "Axis 2 steps:" $steps[1]

write-host "`n"

Write-Host "Axis Step Time Frequency:"
$freq = $GS.GetStepTimeFreq() 
Write-Host "Axis 1 steps:" $freq[0]
Write-Host "Axis 2 steps:" $freq[1]

write-host "`n"

Write-Host "ASCOM On:" $GS.AscomOn
Write-Host "Mount Connected:" $GS.IsConnected
Write-Host "Mount Type SkyWatcher:" $GS.IsServerSkyWatcher

write-host "`n"

Write-Host "Axis 1 Full Stop:" $GS.IsFullStop(1)
Write-Host "Axis 1 High Speed:" $GS.IsHighSpeed(1)
Write-Host "Axis 1 Is Slewing Low Speed:" $GS.IsSlewing(1)
Write-Host "Axis 1 Is Slewing High Speed:" $GS.IsSlewingTo(1)
Write-Host "Axis 1 Is Moving Positive Direction:" $GS.IsSlewingFoward(1)

# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
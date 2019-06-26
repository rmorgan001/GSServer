# Clear the console pane
Clear-Host

# GSS must be already running and connected to the mount
#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

# Necessary - Turn off GSS from processing ascom movements
# from other sources
$GS.AscomOn = $false;

# Show the memeber list, which is a list of commands 
# available to execute
$GS | Get-Member

# String array versions
Write-Host "Axis String Versions:" 
$versions = $GS.GetAxisStringVersions()
Write-Host "Axis 1 Version:" $versions[0]
Write-Host "Axis 2 Version:" $versions[1]

#`0    Null
#`a    Alert
#`b    Backspace
#`f    Form feed
#`n    New line
#`r    Carriage return
#`t    Horizontal tab
#`v    Vertical tab
write-host "`n"
write-host "`r"

#Integer array versions
Write-Host "Axis Integer Versions:" 
$versions = $GS.GetAxisVersions()
Write-Host "Axis 1 Version:" $versions[0]
Write-Host "Axis 2 Version:" $versions[1]

# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
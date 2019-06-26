# Clear the console pane
Clear-Host

# GSS must be already running and connected to the mount
#Necessary - Create a new instance of the GS.Sky.Api.dll
$GS = New-Object -COMObject "GS.SkyApi"

# Show the memeber list, which is a list of commands 
# available to execute
$GS | Get-Member

# Necessary - Relese the Com Object
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($GS)
Remove-Variable GS
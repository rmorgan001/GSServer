# this will not help getting rid of the unknown publisher when installing on other computers.   The cert would need to be verified by a CA and that costs $$$$
# http://www.softwarepublishercertificate.com/

# test information do not use
#New-SelfSignedCertificate -Type Custom -Subject "CN=GreenSwamp Software, O=GreenSwamp, C=US" -KeyUsage DigitalSignature -FriendlyName "GreenSwamp Software" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
#PSParentPath: Microsoft.PowerShell.Security\Certificate::CurrentUser\My
#15C071A5A538305A59099A6702AB9921A66C721D  CN=GreenSwamp Software, O=GreenSwamp, C=US

# run this line first
#New-SelfSignedCertificate -Type CodeSigningCert -CertStoreLocation "Cert:\LocalMachine\My" -Subject "CN=GreenSwamp Software" -TextExtension @("2.5.29.19={text}false") -KeyUsage DigitalSignature -KeyLength 2048 -NotAfter (Get-Date).AddMonths(33) -FriendlyName "GreenSwamp Software"
# produces the following output....
# PSParentPath: Microsoft.PowerShell.Security\Certificate::LocalMachine\MY
#
# Thumbprint                                Subject                                                                                                                            
# ----------                                -------                                                                                                                            
# DE67300F5009BFC86BB038C8F162BA2992FEEC8C  CN="GreenSwamp Software, 0=GreenSwamp, C=US" 

# run this line second, change the thumbprint to match
#$pwd = ConvertTo-SecureString -String rem -Force -AsPlainText 
#Export-PfxCertificate -cert "Cert:\LocalMachine\My\DE67300F5009BFC86BB038C8F162BA2992FEEC8C" -FilePath "C:\Users\Rob\source\repos\GSSolution\Resources\Installer\GreenSwamp.pfx" -Password $pwd
# produces the following output....
# Mode                LastWriteTime         Length Name                                                                                                                        
# ----                -------------         ------ ----                                                                                                                        
# -a----         2/6/2020   7:04 PM            2685 GreenSwamp.pfx




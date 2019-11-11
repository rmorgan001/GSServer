# GSServer - ASCOM Synta/SkyWatcher Mount Driver
GS Server is SkyWatcher ASCOM telescope driver for use with astronomy software and SkyWatcher mounts.  It is built using C#, WPF, and a variation of MVVM.

You can download the installable version at https://groups.io/g/GSS/files . located in the files section.

## Features

* Gamepad support
* GPS NMEA support for lat/long/elevation information
* CdC Observatory lat/long/elevation push/pull
* Local DarkSky weather conditions
* PHD2 plotting along with mount steps or pulse information for tracking
* Log Viewer for viewing Charting logs
* Keep observing notes or logs
* Session, Error, and Charting Logs
* Built in simulator for testing
* Synthesized speach commands
* No Sleep mode to keep screensaver off
* Monitor driver, server, and mount data live
* Autohome process for axes with home sensors

![Alt text](Docs/gsserver2.jpg?raw=true "GSServer")

Installable version at https://groups.io/g/GSS/files .  located in the files section.

## Solution Projects

* ASCOM.GS.Sky.Telescope - COM/.Net Class Library implementing the ASCOM device interface for V3 telescope driver.
* ColorPicker - from another source
* GS.LogView - Log viewer for the charting data
* Principles - Class Library that contains a number of fundamental methods including Coordinates, Conversions, Hi Resoulution dates,               Julian dates, Timers, Time, and unit functions.
* GS.SerialTester - WPF application that connects then runs in a loop getting the axes poistions.  Good for testing cables.
* GS.Server - ASCOM local server and organizes the view models 
* GS.Shared - Common code
* GS.Simulator - Complete simulator that mimic a synta mount
* GS.SkyApi - API for the server
* GS.SkyWatcher - Synta codes and mount controls

![Alt text](Docs/GSScreens.jpg?raw=true "GSScreens")

## Built With

* Visual Studio 2017 Community edition
* .Net Framework 4.6.1
* DarkSkyApi for weather information https://darksky.net/dev
* Live Charts and Live Charts Geared - for plotting https://lvcharts.net/
* Material Design In XAML - https://github.com/MaterialDesignInXAML
* SharpDX - gamepad support http://sharpdx.org/
* ASCOM platform 6.4 and developer tools
* Inno Setup Compiler version 5.5.8 http://www.innosetup.com
* Microsoft.Xaml.Behaviors.Wpf
* Newtonsoft.Json
* Reference strong named Dlls and other information can be downloaded [here](https://drive.google.com/open?id=13nAFTjvD_HTZVNBRV0BwxsHk0EmJ1ayi)

## Contributing

Please read [CODE_OF_CONDUCT.md](https://github.com/rmorgan001/GSServer/blob/master/Docs/CODE_OF_CONDUCT.md) for details on our code of conduct, and the process for submitting pull requests to us.

If you are contributing to this repository, please first discuss the change you wish to make via issue,
email, or any other method with the owners of this repository before making a change. 

Please note we have a code of conduct, please follow it in all your interactions with the project.

Please read [CONTRIBUTING.md](https://github.com/rmorgan001/GSServer/blob/master/Docs/CONTRIBUTING.md)

## Authors

* **Robert Morgan** - *Initial work* - https://github.com/rmorgan001

See also the list of [contributors](https://github.com/your/project/contributors) who participated in this project.

## Support

Can be found at https://groups.io/g/GSS/topics

## License

/* 
    Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

## Acknowledgments

* Hat tip to anyone whose code was used
* ASCOM development team
* Andrew Johansen & Colm Brazel
* SkywatcherEQ8 members at https://groups.yahoo.com/neo/groups/SkywatcherEQ8/info

## Updates

1.0.0.8 released 12 July 2019
* Added Full Current Option
* Corrected EQ6 :q command returning !0 Abnormal response

1.0.0.10 released 28 July 2019
* UI bug fixes and enhancements 
   
1.0.0.11 released 23 Aug 2019
* User can resize application window
* Ascom conformance changes
* Goto strategy changed when within meridian limits setting
* Manual Flip SOP button added
* Corrected sync issue after a flip
* Added color themes

1.0.0.16 released 28 Sept 2019
* New Hand Controler Modes
* New Reset Settings
* Park Correction
* Abort Slew Correction for tracking
* IsSlewing correction for CCD Commander

1.0.0.17 released 9 Oct 2019
* Added park poistions
* Added 3d model

1.0.0.18 released 1 Nov 2019
* Added AutoHome process
* Notes enhancements

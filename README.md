# GSServer - ASCOM Synta/SkyWatcher Mount Driver
GS Server is SkyWatcher ASCOM telescope driver for use with astronomy software and SkyWatcher and Orion mounts.  It is built using C#, WPF, and a variation of MVVM.

Download the GSServer installer program at 
## http://www.greenswamp.org/

![Alt text](Docs/gsserver2.jpg?raw=true "GSServer")

## ScreenShots here http://www.greenswamp.org/screenshots

## Features

* Autohome process for mount with home sensors
* New Dec guiding alternative
* PEC support for mounts that support PPEC
* GPS NMEA support for lat/long/elevation information
* CdC push/pull for Observatory locations 
* Local DarkSky weather conditions
* PHD2 plotting along with pulse information
* ChartViewer for viewing plots after sessions
* Take and save observing notes
* Session, Error, and Monitor logs for troubleshooting
* Built in simulator for testing
* Synthesized voice commands
* No Sleep Mode to keep screensaver off
* Monitor raw mount commands live
* Gamepad support
* 3d model representation of mount position
* Multiple park positions
* Full and half current tracking for battery power source
* Theme support with primary and secondary colors

## Solution Projects

* ASCOM.GS.Sky.Telescope - COM/.Net Class Library implementing the ASCOM device interface for V3 telescope driver.
* ColorPicker - from another source
* GS.ChartViewer - Viewer for the charting data
* Principles - Class Library that contains a number of fundamental methods including Coordinates, Conversions, Hi Resoulution dates,               Julian dates, Timers, Time, and unit functions.
* GS.Utilities - WPF application for troubleshooting GS Server.
* GS.Server - ASCOM local server and organizes the view models 
* GS.Shared - Common code
* GS.Simulator - Complete simulator that mimic a synta mount
* GS.SkyApi - API for the server
* GS.SkyWatcher - Synta codes and mount controls

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

Can be found at [Groups.io](https://groups.io/g/GSS)

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
* SkywatcherEQ8 members at [Groups.io](https://groups.io/g/SkywatcherEQ8)

## Release Dates

* 1.0.1.1 release 13 May 2020
* 1.0.1.1 release 12 Apr 2020
* 1.0.1.0 release 21 Mar 2020
* 1.0.0.27 release 15 Mar 2020


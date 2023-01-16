using GS.Server.SkyTelescope;

namespace ASCOM.GS.Sky.Telescope
{
    internal class CommandStrings
    {
        public static string ProcessCommand(string command, bool raw)
        {
            command = command.Trim();
            CheckIsMountRunning("NotConnectedException in CommandStrings/ProcessCommand");
            //if (raw) { throw new DriverException("Raw param error"); }
            if (command.Length < 2) { throw new DriverException("Command length error"); }
            if (!command.Contains(":")) { throw new DriverException("Command colon error"); }

            // case statement for the mount commands
            switch (command.Substring(0, 2))
            {
                case ":O":// Trigger Snap Port
                    if (command.Length != 4) { throw new DriverException("Param Cmd error"); }
                    switch (command.Substring(2, 1))
                    {
                        case "1": //Port 1
                            switch (command.Substring(3, 1))
                            {
                                case "0": // Off
                                    SkyServer.SnapPort1 = false;
                                    break;
                                case "1": // On
                                    SkyServer.SnapPort1 = true;
                                    break;
                                default:
                                    throw new DriverException("Param error");
                            }
                            switch (SkySettings.Mount)
                            {
                                case MountType.Simulator:
                                    SkyServer.SimTasks(MountTaskName.SetSnapPort1);
                                    break;
                                case MountType.SkyWatcher:
                                    SkyServer.SkyTasks(MountTaskName.SetSnapPort1);
                                    break;
                                default:
                                    throw new DriverException("Mount type error");
                            }
                            return SkyServer.SnapPort1Result ? "1" : "0";
                        case "2"://Port 2
                            switch (command.Substring(3, 1))
                            {
                                case "0": // Off
                                    SkyServer.SnapPort2 = false;
                                    break;
                                case "1": // On
                                    SkyServer.SnapPort2 = true;
                                    break;
                                default:
                                    throw new DriverException("Param 2 error");
                            }
                            switch (SkySettings.Mount)
                            {
                                case MountType.Simulator:
                                    SkyServer.SimTasks(MountTaskName.SetSnapPort2);
                                    break;
                                case MountType.SkyWatcher:
                                    SkyServer.SkyTasks(MountTaskName.SetSnapPort2);
                                    break;
                                default:
                                    throw new DriverException("Mount type error");
                            }
                            return SkyServer.SnapPort2Result ? "1" : "0";
                        default:
                            throw new DriverException("Param Port error");
                    }
                default:
                    throw new DriverException("Command not found");
            }
        }

        private static void CheckIsMountRunning(string msg)
        {
            if (!SkyServer.IsMountRunning)
            {
                throw new NotConnectedException(msg);
            }
        }
    }
}

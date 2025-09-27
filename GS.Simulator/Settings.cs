namespace GS.Simulator
{
    /// <summary>
    /// Provides configuration settings for the application's auto-home functionality.
    /// </summary>
    /// <remarks>This class contains static properties that define the default positions for the X and Y axes 
    /// during the auto-home operation. These settings can be used to configure or retrieve the  axis values as
    /// needed.</remarks>
    public static class Settings
    {
        public static int AutoHomeAxisX { get; set; }
        public static int AutoHomeAxisY { get; set; }
    }
}

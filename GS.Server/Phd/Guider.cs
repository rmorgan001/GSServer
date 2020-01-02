using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;

namespace GS.Server.Phd
{
    // settling progress information returned by Guider::CheckSettling()
    public class SettleProgress
    {
        public bool Done;
        public double Distance;
        public double SettlePx;
        public double Time;
        public double SettleTime;
        public int Status;
        public string Error;
    }

    public class GuideStats
    {
        public double rms_tot;
        public double rms_ra;
        public double rms_dec;
        public double peak_ra;
        public double peak_dec;

        public GuideStats Clone() { return (GuideStats)MemberwiseClone(); }
    }

    public class GuideStep
    {
        //public double dx { get; set; }
        public double RADistanceRaw { get; set; }
        //public double RADistanceGuide { get; set; }
        public double RADuration { get; set; }
        public string RADirection { get; set; }
        //public bool RALimited { get; set; }

        //public double dy { get; set; }
        public double DecDistanceRaw { get; set; }
        //public double DecDistanceGuide { get; set; }
        public double DECDuration { get; set; }
        public string DECDirection { get; set; }
        //public bool DecLimited { get; set; }

        public double ErrorCode { get; set; }
        public double Time { get; set; }
        public double TimeStamp { get; set; }
        public DateTime LocalTimeStamp { get; set; }
        //public string Mount { get; set; }
        //public double SNR { get; set; }
        //public double StarMass { get; set; }
        //public double AvgDist { get; set; }
        //public double Frame { get; set; }
    }

    public class GuiderException : ApplicationException
    {
        public ErrorCode ErrorCode { get; }

        public GuiderException()
        {
        }

        public GuiderException(ErrorCode err) : base($"{err}")
        {
            ErrorCode = err;
        }

        public GuiderException(ErrorCode err, string message) : base($"{err}, {message}")
        {
            ErrorCode = err;
        }

        public GuiderException(ErrorCode err, string message, Exception inner) : base($"{err}, {message}", inner)
        {
            ErrorCode = err;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        // Constructor should be protected for unsealed classes, private for sealed classes.
        // (The Serializer invokes this constructor through reflection, so it can be private)
        protected GuiderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Enum.TryParse("err", out ErrorCode err);
            ErrorCode = err;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            info.AddValue("err", ErrorCode.ToString());
            // MUST call through to the base class to let it save its own state
            base.GetObjectData(info, context);
        }


    }

    public enum ErrorCode
    {
        NewConnection = 1,
        LostConnection = 2,
        Disconnected = 3,
        GuidingError = 4,
        NoResponse = 5,
    }

    public abstract class Guider : IDisposable
    {
        public static Guider Factory(string hostname, CancellationTokenSource token, uint phd2_instance = 1) { return new GuiderImpl(hostname, phd2_instance, token); }

        public abstract void Dispose();

        // connect to PHD2 -- you'll need to call Connect before calling any of the server API methods below
        public abstract void Connect();

        // disconnect from PHD2
        public abstract void Close();

        // these two member functions are for raw JSONRPC method invocation. Generally you won't need to
        // use these functions as it is much more convenient to use the higher-level methods below
        public abstract JObject Call(string method);
        public abstract JObject Call(string method, JToken param);

        // Start guiding with the given settling parameters. PHD2 takes care of looping exposures,
        // guide star selection, and settling. Call CheckSettling() periodically to see when settling
        // is complete.
        public abstract void Guide(double settlePixels, double settleTime, double settleTimeout);

        // Dither guiding with the given dither amount and settling parameters. Call CheckSettling()
        // periodically to see when settling is complete.
        public abstract void Dither(double ditherPixels, double settlePixels, double settleTime, double settleTimeout);

        // Check if phd2 is currently in the process of settling after a Guide or Dither
        public abstract bool IsSettling();

        // Get the progress of settling
        public abstract SettleProgress CheckSettling();

        // Get the guider statistics since guiding started. Frames captured while settling is in progress
        // are excluded from the stats.
        public abstract GuideStats GetStats();

        // stop looping and guiding
        public abstract void StopCapture(uint timeoutSeconds = 10);

        // start looping exposures
        public abstract void Loop(uint timeoutSeconds = 10);

        // get the guider pixel scale in arc-seconds per pixel
        public abstract double PixelScale();

        // get a list of the Equipment Profile names
        public abstract List<string> GetEquipmentProfiles();

        // connect the equipment in an equipment profile
        public abstract void ConnectEquipment(string profileName);

        // disconnect equipment
        public abstract void DisconnectEquipment();

        // get the AppState (https://github.com/OpenPHDGuiding/phd2/wiki/EventMonitoring#appstate)
        // and current guide error
        public abstract void GetStatus(out string appState, out double avgDist);

        // check if currently guiding
        public abstract bool IsGuiding();

        // pause guiding (looping exposures continues)
        public abstract void Pause();

        // un-pause guiding
        public abstract void Unpause();

        // save the current guide camera frame (FITS format), returning the name of the file.
        // The caller will need to remove the file when done.
        public abstract string SaveImage();
    }
}

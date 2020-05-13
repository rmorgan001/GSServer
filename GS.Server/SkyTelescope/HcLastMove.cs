using System;

namespace GS.Server.SkyTelescope
{
    class HcPrevMove
    {
        public DateTime StartDate { get; set; }
        public SlewDirection Direction { get; set; }
        public double Delta { get; set; }
        public double? StepStart { get; set; }
        public double? StepEnd { get; set; }
        public double StepDiff { get; set; }

    }
}

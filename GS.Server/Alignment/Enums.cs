using GS.Server.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Alignment
{
    [TypeConverter(typeof(EnumLocalizationTypeConverter))]
    public enum AlignmentAlgorithm
    {
        [Description("enumAlignmentAlgorithm_0")]
        Nearest,
        [Description("enumAlignmentAlgorithm_1")]
        NStar,
        [Description("enumAlignmentAlgorithm_2")]
        NStarPlusNearest
    }

    [TypeConverter(typeof(EnumLocalizationTypeConverter))]
    public enum PointFilterMode
    {
        [Description("enumPointFilterMode_0")]
        AllPoints,
        [Description("enumPointFilterMode_1")]
        Meridian,
        [Description("enumPointFilterMode_2")]
        LocalQuadrant
    }

    [TypeConverter(typeof(EnumLocalizationTypeConverter))]
    public enum ThreePointMode
    {
        [Description("enumThreePointMode_0")]
        NearestTriangle,
        [Description("enumThreePointMode_1")]
        TriangleWithNearestCentre,
    }

}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using GS.Server.SkyTelescope;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlignmentModelTests
{
    // These tests change process-wide settings. Never run alongside other tests.
    [TestClass]
    [DoNotParallelize]
    public class HomeParkCharacterizationTests
    {
        public TestContext TestContext { get; set; }
        private static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly Type Settings = typeof(SkySettings);
        private static readonly Type AxesType = typeof(Axes);
        private static readonly Type Server = typeof(SkyServer);
        private static Func<double, double, double[]> ConvertPark;
        private static Func<double, double, bool> ThroughPole;
        private static Func<double[], bool> InLimits;
        private static readonly string[] Fields = {
            "_alignmentMode", "_mount", "_polarMode", "_latitude",
            "_axisLimitX", "_axisLowerLimitY", "_axisUpperLimitY"
        };

        private static T Bind<T>(Type type, string name) where T : class
        {
            var method = type.GetMethod(name, Static);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            return (T)(object)Delegate.CreateDelegate(typeof(T), method);
        }

        private static object DefaultSettings(string name)
        {
            var type = Server.Assembly.GetType("GS.Server.Properties." + name, true);
            return type.GetProperty("Default", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
        }

        private static object Property(object target, string name)
        {
            return target.GetType().GetProperty(name).GetValue(target, null);
        }

        // The server constructor can migrate and write configuration. Refuse to start it
        // unless its two upgrade paths are already known not to save anything.
        private static void InitializeSafely()
        {
            var profile = DefaultSettings("Profile");
            var telescope = DefaultSettings("SkyTelescope");
            var key = telescope.GetType().GetProperty("SettingsKey");
            var original = key.GetValue(telescope, null);
            try
            {
                key.SetValue(telescope, Property(profile, "Current"), null);
                Assert.AreNotEqual("0", (string)Property(telescope, "Version"),
                    "SkyServer would initialize or migrate and save telescope settings; refusing to run.");
            }
            finally { key.SetValue(telescope, original, null); }
            var alignment = DefaultSettings("Alignment");
            Assert.AreEqual(Server.Assembly.GetName().Version.ToString(), (string)Property(alignment, "Version"),
                "SkyServer would upgrade and save alignment settings; refusing to run.");
            RuntimeHelpers.RunClassConstructor(Server.TypeHandle);
            ConvertPark = Bind<Func<double, double, double[]>>(AxesType, "AzAltToPolarPark");
            ThroughPole = Bind<Func<double, double, bool>>(AxesType, "DetermineIfThroughPole");
            InLimits = Bind<Func<double[], bool>>(Server, "IsTargetWithinLimits");
        }

        private sealed class State : IDisposable
        {
            private readonly Dictionary<FieldInfo, object> values = new Dictionary<FieldInfo, object>();
            public State()
            {
                InitializeSafely();
                foreach (string name in Fields)
                {
                    var field = Settings.GetField(name, Static);
                    if (field == null) throw new MissingFieldException(Settings.FullName, name);
                    values.Add(field, field.GetValue(null));
                }
            }
            public void Set(string name, object value)
            {
                var field = Settings.GetField(name, Static);
                if (field == null || !values.ContainsKey(field)) throw new MissingFieldException(Settings.FullName, name);
                field.SetValue(null, field.FieldType.IsEnum ? Enum.Parse(field.FieldType, (string)value) : value);
            }
            public void Configure(string mount, string side, double latitude, double x, double lower, double upper)
            {
                Set("_alignmentMode", "algPolar"); Set("_mount", mount); Set("_polarMode", side);
                Set("_latitude", latitude); Set("_axisLimitX", x);
                Set("_axisLowerLimitY", lower); Set("_axisUpperLimitY", upper);
            }
            public void Dispose() { foreach (var pair in values) pair.Key.SetValue(null, pair.Value); }
        }

        private static double[] Physical(double[] axes) { return SkyServer.MapSlewTargetToAxes(axes, SlewType.SlewPark); }
        private static string Decision(double[] physical)
        {
            if (!InLimits(physical)) return "outside-limits";
            return ThroughPole(physical[0], physical[1]) ? "through-pole" : "accepted";
        }
        private static string F(double value) { return value.ToString("R", CultureInfo.InvariantCulture); }
        private static void Finite(double[] axes)
        {
            Assert.AreEqual(2, axes.Length);
            Assert.IsFalse(double.IsNaN(axes[0]) || double.IsInfinity(axes[0]));
            Assert.IsFalse(double.IsNaN(axes[1]) || double.IsInfinity(axes[1]));
        }

        [TestMethod]
        public void IntegerGrid_RecordsProductionChainAndIndependentSkyVectors()
        {
            var file = Path.Combine(TestContext.TestResultsDirectory, "home-park-grid.csv.gz");
            var summary = Path.Combine(TestContext.TestResultsDirectory, "home-park-summary.csv");
            var latitudes = new[] { 1.0, 30.0, 52.0, 80.0, 52.25 };
            // Stored upper Y excludes the site's latitude; profiles are selectable in the UI.
            var profiles = new[] { new[] { 105.0, 20.0, 50.0 }, new[] { 150.0, -30.0, 70.0 }, new[] { 210.0, -90.0, 90.0 } };
            long all = 0;
            using (var state = new State())
            using (var stream = new GZipStream(File.Create(file), CompressionMode.Compress))
            using (var csv = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var sums = new StreamWriter(summary, false, new UTF8Encoding(false)))
            {
                csv.WriteLine("mount,requestedSide,effectiveSide,latitude,xLimit,lowerY,storedUpperY,az,alt,appX,appY,physicalX,physicalY,inLimits,throughPole,decision,suspiciousNormalThroughPole");
                sums.WriteLine("mount,requestedSide,effectiveSide,latitude,xLimit,lowerY,storedUpperY,points,accepted,outsideLimits,throughPole,suspiciousNormalThroughPole,firstWitnessAz,firstWitnessAlt");
                foreach (var mount in new[] { "SkyWatcher", "Simulator" })
                foreach (var side in new[] { "Left", "Right" })
                foreach (var lat in latitudes)
                foreach (var profile in profiles)
                {
                    state.Configure(mount, side, lat, profile[0], profile[1], profile[2]);
                    string effective = mount == "Simulator" ? "Right" : side;
                    long count = 0, accepted = 0, outside = 0, through = 0, suspicious = 0;
                    string witnessAz = "", witnessAlt = "";
                    for (int az = 0; az <= 360; az++)
                    for (int alt = -90; alt <= 90; alt++)
                    {
                        var app = ConvertPark(az, alt);
                        var physical = Physical(app);
                        Finite(app); Finite(physical);
                        // Compare Cartesian sky directions; unlike hour angle, this is meaningful at poles.
                        double d = Math.PI / 180.0, h = app[0] * d, dec = app[1] * d, a = az * d, el = alt * d, phi = lat * d;
                        double east = -Math.Cos(dec) * Math.Sin(h);
                        double north = Math.Sin(dec) * Math.Cos(phi) - Math.Cos(dec) * Math.Cos(h) * Math.Sin(phi);
                        double up = Math.Sin(dec) * Math.Sin(phi) + Math.Cos(dec) * Math.Cos(h) * Math.Cos(phi);
                        Assert.AreEqual(Math.Cos(el) * Math.Sin(a), east, 0.000001, "east vector");
                        Assert.AreEqual(Math.Cos(el) * Math.Cos(a), north, 0.000001, "north vector");
                        Assert.AreEqual(Math.Sin(el), up, 0.000001, "up vector");
                        bool within = InLimits(physical), tp = ThroughPole(physical[0], physical[1]);
                        string decision = !within ? "outside-limits" : tp ? "through-pole" : "accepted";
                        bool suspect = within && tp && Math.Abs(app[1]) < 90 - 1e-9;
                        if (decision == "accepted") accepted++; else if (decision == "outside-limits") outside++; else through++;
                        if (suspect) { suspicious++; if (witnessAz == "") { witnessAz = az.ToString(CultureInfo.InvariantCulture); witnessAlt = alt.ToString(CultureInfo.InvariantCulture); } }
                        csv.WriteLine(string.Join(",", mount, side, effective, F(lat), F(profile[0]), F(profile[1]), F(profile[2]), az.ToString(CultureInfo.InvariantCulture), alt.ToString(CultureInfo.InvariantCulture), F(app[0]), F(app[1]), F(physical[0]), F(physical[1]), within ? "1" : "0", tp ? "1" : "0", decision, suspect ? "1" : "0"));
                        count++;
                    }
                    Assert.AreEqual(65341L, count); Assert.AreEqual(count, accepted + outside + through);
                    sums.WriteLine(string.Join(",", mount, side, effective, F(lat), F(profile[0]), F(profile[1]), F(profile[2]), count, accepted, outside, through, suspicious, witnessAz, witnessAlt));
                    all += count;
                }
            }
            Assert.AreEqual(3920460L, all);
            TestContext.AddResultFile(file); TestContext.AddResultFile(summary);
            TestContext.WriteLine("Grid: " + file);
            TestContext.WriteLine("Summary: " + summary);
        }

        [TestMethod]
        public void PolarLimits_OneArcsecondXAndExactLatitudeAdjustedY()
        {
            using (var state = new State())
            {
                foreach (var side in new[] { "Left", "Right" })
                {
                    state.Configure("SkyWatcher", side, 52, 45, -20, 40);
                    double lower = side == "Left" ? 88 : -20;
                    double upper = side == "Left" ? 200 : 92;
                    Assert.IsTrue(InLimits(new[] { 45 + 1.0 / 3600, lower }));
                    Assert.IsTrue(InLimits(new[] { -45 - 1.0 / 3600, upper }));
                    Assert.IsFalse(InLimits(new[] { 45 + 1.0 / 3600 + 0.000001, lower }));
                    Assert.IsFalse(InLimits(new[] { -45 - 1.0 / 3600 - 0.000001, upper }));
                    Assert.IsFalse(InLimits(new[] { 0.0, lower - 0.000001 }));
                    Assert.IsFalse(InLimits(new[] { 0.0, upper + 0.000001 }));
                }
            }
        }

        [TestMethod]
        public void MappingAndRejection_CharacterizeLeftAsymmetryAndPrecedence()
        {
            using (var state = new State())
            {
                state.Configure("SkyWatcher", "Left", 52, 180, -180, 180);
                var app = new[] { 0.0, 10.0 };
                var physical = Physical(app);
                Assert.AreEqual(0, physical[0], 1e-10); Assert.AreEqual(170, physical[1], 1e-10);
                Assert.IsTrue(ThroughPole(physical[0], physical[1]));
                Assert.AreEqual("through-pole", Decision(physical), "Normal app orientation can be flagged through-pole after mapping.");
                state.Set("_axisLimitX", 0.0);
                Assert.AreEqual("outside-limits", Decision(new[] { 1.0, 170.0 }), "Limits take precedence over through-pole.");
                state.Configure("Simulator", "Left", 52, 180, -180, 180);
                physical = Physical(app);
                Assert.AreEqual(10, physical[1], 1e-10); Assert.IsFalse(ThroughPole(physical[0], physical[1]));
                Assert.AreEqual("accepted", Decision(physical));
            }
        }

        [TestMethod]
        [DataRow("SkyWatcher", "Left", 148.0, "through-pole")]
        [DataRow("SkyWatcher", "Right", 32.0, "accepted")]
        [DataRow("Simulator", "Left", 32.0, "accepted")]
        [DataRow("Simulator", "Right", 32.0, "accepted")]
        public void CustomPark_ReproducibleNormalPosition(string mount, string side, double physicalY, string expectedDecision)
        {
            using (var state = new State())
            {
                state.Configure(mount, side, 52, 105, 20, 50);
                var app = ConvertPark(180, 70);
                Assert.AreEqual(0.0, app[0], 1e-9);
                Assert.AreEqual(32.0, app[1], 1e-9);
                var physical = Physical(app);
                Assert.AreEqual(0.0, physical[0], 1e-9);
                Assert.AreEqual(physicalY, physical[1], 1e-9);
                Assert.IsTrue(InLimits(physical));
                Assert.AreEqual(expectedDecision, Decision(physical));
            }
        }

        [TestMethod]
        public void PoleDetector_ExactBoundaries()
        {
            using (var state = new State())
            {
                state.Configure("Simulator", "Right", 52, 180, -180, 180);
                Assert.IsFalse(ThroughPole(90, 89.999999)); Assert.IsTrue(ThroughPole(90.000001, 0));
                Assert.IsTrue(ThroughPole(0, 90)); Assert.IsFalse(ThroughPole(0, 180));
                Assert.IsTrue(ThroughPole(0, -180)); Assert.IsFalse(ThroughPole(0, -90));
            }
        }

        [TestMethod]
        [DataRow(180.0, 0.0, 0.0, -38.0)]
        [DataRow(90.0, 0.0, -90.0, 0.0)]
        [DataRow(270.0, 0.0, 90.0, 0.0)]
        public void CardinalDirections_HaveKnownAxisCoordinates(double az, double alt, double x, double y)
        {
            using (var state = new State())
            {
                state.Configure("Simulator", "Right", 52, 180, -180, 180);
                var axes = ConvertPark(az, alt);
                Assert.AreEqual(x, axes[0], 1e-9);
                Assert.AreEqual(y, axes[1], 1e-9);
            }
        }

        [TestMethod]
        public void AzimuthWrap_AndDefaultOverrideAreDistinct()
        {
            using (var state = new State())
            {
                state.Configure("Simulator", "Right", 52, 180, -180, 180);
                for (int alt = -89; alt <= 89; alt++)
                {
                    if (alt == 52) continue; // hour angle is singular at the celestial pole
                    var zero = ConvertPark(0, alt); var full = ConvertPark(360, alt);
                    Assert.AreEqual(Math.Sin(zero[0] * Math.PI / 180), Math.Sin(full[0] * Math.PI / 180), 1e-9);
                    Assert.AreEqual(Math.Cos(zero[0] * Math.PI / 180), Math.Cos(full[0] * Math.PI / 180), 1e-9);
                    Assert.AreEqual(zero[1], full[1], 1e-9);
                }
                // The dialog overrides the computed pole to [90,90] and bypasses validation;
                // the test does not invoke UI code or claim this is the persisted result.
                var custom = Physical(ConvertPark(180, 0));
                Assert.IsTrue(Math.Abs(custom[0] - 90) > 1 || Math.Abs(custom[1] - 90) > 1);
                Assert.IsTrue(ThroughPole(90, 90));
            }
        }
    }
}

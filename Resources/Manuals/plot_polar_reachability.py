r"""Plot the SkyWatcher GSServer Polar-mode hardware envelope.

From the repository root:
    python -m pip install "matplotlib>=3.8"
    python Docs\plot_polar_reachability.py
    python Docs\plot_polar_reachability.py --help

For A4 portrait printing at actual size, use --output polar_reachability.pdf.
Enter limits as shown in Hardware Limits. The chart also shows the
latitude-adjusted Y range and, for Polar Left, the reflected raw Y range.
The plot assumes ideal polar alignment and a geometric, unobstructed horizon.
It describes valid endpoints, not slew paths or tracking duration.
"""

import argparse
import math
from pathlib import Path
import xml.etree.ElementTree as ET

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.lines import Line2D
from matplotlib.patches import Circle, Patch
import numpy as np


COLORS = ("#cbe1ed", "#f6c888", "#c7c1e3")
OUTLINE = "#172b42"
SKY = "#f1f3f5"


def bundled_settings(filename):
    settings = Path(__file__).resolve().parents[1] / "GS.Server" / "Properties" / filename
    namespace = {"vs": "http://schemas.microsoft.com/VisualStudio/2004/01/settings"}
    return {entry.attrib["Name"]: entry.findtext("vs:Value", namespaces=namespace)
            for entry in ET.parse(settings).findall("vs:Settings/vs:Setting", namespace)}


def arguments():
    profile = bundled_settings(Path("Profiles") / "Polar.settings")
    default_latitude = float(bundled_settings("SkyTelescope.settings")["Latitude"])
    parser = argparse.ArgumentParser(
        description="Plot SkyWatcher Polar-mode reachability in hour angle and declination.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument("--latitude", type=float, default=default_latitude,
                        help="site latitude in degrees; signed unless hemisphere is supplied")
    parser.add_argument("--hemisphere", choices=("north", "south"),
                        help="override the latitude sign, using its absolute magnitude")
    parser.add_argument("--polar-mode", choices=("left", "right"), type=str.lower,
                        default=profile["PolarMode"].lower(), help="SkyWatcher Polar orientation")
    parser.add_argument("--ra-limit", type=float, default=float(profile["AxisLimitX"]),
                        help="symmetric physical RA-axis limit in degrees")
    parser.add_argument("--dec-lower", type=float, default=float(profile["AxisLowerLimitY"]),
                        help="Dec-axis lower setting in degrees, as shown in Hardware Limits")
    parser.add_argument("--dec-upper", type=float, default=float(profile["AxisUpperLimitY"]),
                        help="Dec-axis upper setting in degrees, as shown in Hardware Limits")
    parser.add_argument("--ha-step", type=float, default=1.0, help="hour-angle grid interval in hours")
    parser.add_argument("--dec-step", type=float, default=10.0, help="declination grid interval in degrees")
    parser.add_argument("--output", type=Path, default=Path("polar_reachability.png"),
                        help="output file (.png, .svg, or .pdf)")
    args = parser.parse_args()

    if not -90 <= args.latitude <= 90:
        parser.error("--latitude must be between -90 and 90 degrees")
    if args.hemisphere == "north":
        args.latitude = abs(args.latitude)
    elif args.hemisphere == "south":
        args.latitude = -abs(args.latitude)
    args.southern = args.hemisphere == "south" or args.latitude < 0
    if not 0 < args.ra_limit <= 360:
        parser.error("--ra-limit must be greater than 0 and at most 360 degrees")
    if not -90 <= args.dec_lower <= args.dec_upper <= 90:
        parser.error("Dec settings must satisfy -90 <= --dec-lower <= --dec-upper <= 90")
    for value, maximum, option in ((args.ha_step, 24, "--ha-step"),
                                   (args.dec_step, 180, "--dec-step")):
        if not math.isfinite(value) or not 0 < value <= maximum or maximum / value > 720:
            parser.error(f"{option} must be positive, at most {maximum}, and yield at most 720 intervals")
    if args.output.suffix.lower() not in {".png", ".svg", ".pdf"}:
        parser.error("--output must end in .png, .svg, or .pdf")
    return args


def wrap_degrees(angle):
    return (angle + 180) % 360 - 180


def dec_axis_limits(latitude, polar_mode, lower, upper):
    """Return (latitude-adjusted limits, effective raw mount limits) in degrees."""
    adjusted = lower, upper + abs(latitude)
    raw = (180 - adjusted[1], 180 - adjusted[0]) if polar_mode == "left" else adjusted
    return adjusted, raw


def mount_margins(hour_angle, declination, latitude, southern, polar_mode,
                  ra_limit, dec_lower, dec_upper):
    """Signed distances to the closest violated/satisfied axis limit, in degrees."""
    _, (lower, upper) = dec_axis_limits(latitude, polar_mode, dec_lower, dec_upper)
    left = polar_mode == "left"

    # Reverse the pointing conversion in Axes.AxesMountToApp / AxesXyToRaDec.
    # The alternate orientation rotates RA by 180 degrees and reflects Dec at the pole.
    poleward_dec = -declination if southern else declination
    margins = []
    for app_x, app_y in ((hour_angle, poleward_dec),
                         (hour_angle + 180, 180 - poleward_dec)):
        raw_x = -app_x if southern else app_x
        raw_y = 180 - app_y if left else app_y
        ra_margin = ra_limit + 1 / 3600 - np.abs(wrap_degrees(raw_x))
        margins.append(np.minimum(ra_margin, np.minimum(raw_y - lower, upper - raw_y)))
    return tuple(margins)


def project(hour_angle, declination, latitude):
    """Project equatorial angles (degrees); north is up, west is right."""
    ha = np.deg2rad(hour_angle)
    dec = np.deg2rad(declination)
    lat = math.radians(latitude)
    west = np.cos(dec) * np.sin(ha)
    north = np.sin(dec) * math.cos(lat) - np.cos(dec) * np.cos(ha) * math.sin(lat)
    up = np.sin(dec) * math.sin(lat) + np.cos(dec) * np.cos(ha) * math.cos(lat)
    scale = np.where(up >= -0.02, 1 + up, np.nan)
    return west / scale, north / scale, up


def unproject(x, y, latitude):
    lat = math.radians(latitude)
    radius_sq = x*x + y*y
    west = 2*x / (1 + radius_sq)
    north = 2*y / (1 + radius_sq)
    up = (1 - radius_sq) / (1 + radius_sq)
    cos_dec_cos_ha = up * math.cos(lat) - north * math.sin(lat)
    sin_dec = up * math.sin(lat) + north * math.cos(lat)
    return (np.rad2deg(np.arctan2(west, cos_dec_cos_ha)),
            np.rad2deg(np.arcsin(np.clip(sin_dec, -1, 1))))


def ticks(step, half_range):
    return [i * step for i in range(-math.ceil(half_range / step), math.ceil(half_range / step) + 1)
            if -half_range <= i * step < half_range]


def draw_grid(ax, disk, latitude, ha_step, dec_step):
    dec_samples = np.linspace(-90, 90, 1801)
    ha_samples = np.linspace(-180, 180, 3601)
    grid_color = "#465a71"
    labels = []
    for hour in ticks(ha_step, 12):
        x, y, up = project(15 * hour, dec_samples, latitude)
        line, = ax.plot(x, y, color=grid_color, lw=0.55, alpha=0.55, zorder=4)
        line.set_clip_path(disk)
        if np.max(up) <= 1e-10:
            continue
        label = f"{hour:+g}h" if hour not in (0, -12) else f"{abs(hour):g}h"
        # Near the equator, horizon intersections crowd around the celestial poles.
        anchors = []
        for i in np.flatnonzero(np.diff(up > 0)):
            fraction = up[i] / (up[i] - up[i + 1])
            horizon_dec = dec_samples[i] + fraction * (dec_samples[i + 1] - dec_samples[i])
            hx, hy, _ = project(15 * hour, horizon_dec, latitude)
            if abs(latitude) >= 1e-8:
                anchors.append((1.055 * hx, 1.055 * hy))
        for dec in (15, -15, 35, -35, 55, -55):
            hx, hy, altitude_sine = project(15 * hour, dec, latitude)
            if altitude_sine > 0.025 and abs(hx) > 0.045:
                anchors.append((hx, hy))
        for hx, hy in anchors:
            if all(math.hypot(hx - lx, hy - ly) >= 0.066 for lx, ly in labels):
                ax.text(hx, hy, label, ha="center", va="center", fontsize=7.5,
                        color=grid_color, zorder=8,
                        bbox=dict(facecolor="white", edgecolor="none", alpha=0.75, pad=0.4))
                labels.append((hx, hy))
                break

    for dec in ticks(dec_step, 90):
        if abs(dec) == 90:
            continue
        x, y, _ = project(ha_samples, dec, latitude)
        line, = ax.plot(x, y, color=grid_color, lw=0.55, alpha=0.55, zorder=4)
        line.set_clip_path(disk)
        tx, ty, up = project(0, dec, latitude)
        if up > 0.025:
            annotation = ax.annotate(f"{dec:+g}\N{DEGREE SIGN}" if dec else "0\N{DEGREE SIGN}",
                                     (tx, ty), xytext=(3, 2), textcoords="offset points",
                                     fontsize=7, color=OUTLINE, zorder=7,
                                     bbox=dict(facecolor="white", edgecolor="none", alpha=0.75, pad=0.4))
            annotation.set_clip_path(disk)


def draw(args):
    fig, ax = plt.subplots(figsize=(210 / 25.4, 297 / 25.4), facecolor="white")
    fig.subplots_adjust(left=0.06, right=0.94, top=0.89, bottom=0.19)
    ax.set(aspect="equal", xlim=(-1.17, 1.17), ylim=(-1.17, 1.17))
    ax.axis("off")
    disk = Circle((0, 0), 1, facecolor=SKY, edgecolor="none", zorder=0)
    ax.add_patch(disk)

    grid = np.linspace(-1.025, 1.025, 1025)
    x, y = np.meshgrid(grid, grid)
    ha, dec = unproject(x, y, args.latitude)
    normal, through = mount_margins(ha, dec, args.latitude, args.southern, args.polar_mode,
                                    args.ra_limit, args.dec_lower, args.dec_upper)
    for margin, color in zip((np.minimum(normal, -through), np.minimum(through, -normal),
                              np.minimum(normal, through)), COLORS):
        if np.max(margin) > 0:
            fill = ax.contourf(grid, grid, margin, levels=(0, np.inf), colors=(color,), zorder=2)
            fill.set_clip_path(disk)

    draw_grid(ax, disk, args.latitude, args.ha_step, args.dec_step)
    ax.add_patch(Circle((0, 0), 1, fill=False, edgecolor="#8c98a7", lw=1, zorder=5))
    edge = np.minimum(np.maximum(normal, through), 1 - np.hypot(x, y))
    if np.max(edge) > 0:
        ax.contour(grid, grid, edge, levels=(0,), colors=(OUTLINE,), linewidths=1.6, zorder=6)

    for label, tx, ty in (("N", 0, 1.125), ("S", 0, -1.125),
                          ("E", -1.125, 0), ("W", 1.125, 0)):
        ax.text(tx, ty, label, ha="center", va="center", color=OUTLINE, fontweight="bold")
    ax.plot(0, 0, marker="+", color=OUTLINE, ms=7, zorder=7)

    degree = "\N{DEGREE SIGN}"
    hemisphere = "S" if args.southern else "N"
    adjusted, raw = dec_axis_limits(args.latitude, args.polar_mode, args.dec_lower, args.dec_upper)
    fig.suptitle("SkyWatcher Polar Reachability", fontsize=21, fontweight="bold", color=OUTLINE, y=0.97)
    fig.text(0.5, 0.929,
             f"{abs(args.latitude):g}{degree} {hemisphere}  |  Polar {args.polar_mode.title()}  |  "
             f"X (RA) limit: \N{PLUS-MINUS SIGN}{args.ra_limit:g}{degree}  |  "
             f"Y (Dec) settings: {args.dec_lower:g}{degree} to {args.dec_upper:g}{degree}",
             ha="center", fontsize=10, color=OUTLINE)
    if args.polar_mode == "left":
        effective_text = (f"Latitude-adjusted Y: {adjusted[0]:g}{degree} to {adjusted[1]:g}{degree}  |  "
                          f"Effective raw Y (Dec): {raw[0]:g}{degree} to {raw[1]:g}{degree}")
    else:
        effective_text = (f"Effective raw Y (Dec), latitude-adjusted: "
                          f"{raw[0]:g}{degree} to {raw[1]:g}{degree}")
    fig.text(0.5, 0.904, effective_text, ha="center", fontsize=10, color=OUTLINE)
    fig.legend(handles=[Line2D([], [], color=OUTLINE, lw=1.6, label="Reachable boundary")],
               loc="lower center", bbox_to_anchor=(0.5, 0.18), frameon=False, fontsize=9)
    fig.legend(handles=[Patch(facecolor=COLORS[0], label="Normal only"),
                        Patch(facecolor=COLORS[1], label="Through-pole required"),
                        Patch(facecolor=COLORS[2], label="Both orientations"),
                        Patch(facecolor=SKY, label="Unreachable")],
               loc="lower center", bbox_to_anchor=(0.5, 0.15), ncols=4, frameon=False, fontsize=9)
    fig.text(0.5, 0.125, "Hour angle: east (negative) / west (positive)  |  "
             "Center: zenith  |  Rim: geometric horizon", ha="center", fontsize=9, color=OUTLINE)
    fig.text(0.5, 0.103, "Ideal polar alignment; endpoint limits only", ha="center", fontsize=8,
             color="#465a71")
    fig.savefig(args.output, dpi=300)
    plt.close(fig)


if __name__ == "__main__":
    options = arguments()
    draw(options)
    print(f"Wrote {options.output.resolve()}")

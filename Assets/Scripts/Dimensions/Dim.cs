using System;
using UnityEngine;

namespace Skp.Dimensions
{
    /// <summary>
    /// The three dimensions as a bitmask, as in the prototype (ORD = 1, SPI = 2, INV = 4).
    /// The current dimension is always exactly one bit; an object's mask may combine several.
    /// </summary>
    [Flags]
    public enum Dim
    {
        None = 0,
        Ordinary = 1,
        Spirit = 2,
        Inverted = 4,
        All = Ordinary | Spirit | Inverted,
    }

    public static class DimensionRules
    {
        public static readonly Dim[] Each = { Dim.Ordinary, Dim.Spirit, Dim.Inverted };

        /// <summary>An object is visible and solid iff its mask contains the current dimension.</summary>
        public static bool Active(Dim mask, Dim current) => (mask & current) != 0;

        /// <summary>1 = gravity pulls down, -1 = gravity pulls up (Inverted).</summary>
        public static int GravitySign(Dim current) => current == Dim.Inverted ? -1 : 1;

        public static string Label(Dim d) => d switch
        {
            Dim.Ordinary => "WAKING WORLD",
            Dim.Spirit => "SPIRIT",
            Dim.Inverted => "INVERTED / GRAVITY UP",
            _ => "",
        };

        /// <summary>Palette: gold = safe/ordinary, teal = spirit, violet = inverted.</summary>
        public static Color Color(Dim d) => d switch
        {
            Dim.Spirit => new Color32(0x41, 0xe0, 0xd0, 0xff),
            Dim.Inverted => new Color32(0xc0, 0x7d, 0xff, 0xff),
            _ => new Color32(0xe8, 0xb8, 0x5a, 0xff),
        };

        /// <summary>Full-screen tint behind the world (prototype: spirit rgba(20,190,180,.26), inverted rgba(120,40,160,.32)).</summary>
        public static Color ScreenTint(Dim d) => d switch
        {
            Dim.Spirit => new Color(20 / 255f, 190 / 255f, 180 / 255f, 0.26f),
            Dim.Inverted => new Color(120 / 255f, 40 / 255f, 160 / 255f, 0.32f),
            _ => new Color(0, 0, 0, 0),
        };

        /// <summary>Colour of an object's faint outline when it belongs to another dimension.</summary>
        public static Color GhostColor(Dim mask) =>
            (mask & Dim.Spirit) != 0 ? Color(Dim.Spirit) : (mask & Dim.Inverted) != 0 ? Color(Dim.Inverted) : new Color32(0xe2, 0xa4, 0x5a, 0xff);
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace QTTabBarLib {
    internal static class TagColorPalette {
        private static readonly Color[] DefaultPalette = new[] {
            ColorTranslator.FromHtml("#F44336"),
            ColorTranslator.FromHtml("#E91E63"),
            ColorTranslator.FromHtml("#9C27B0"),
            ColorTranslator.FromHtml("#673AB7"),
            ColorTranslator.FromHtml("#3F51B5"),
            ColorTranslator.FromHtml("#03A9F4"),
            ColorTranslator.FromHtml("#009688"),
            ColorTranslator.FromHtml("#4CAF50"),
            ColorTranslator.FromHtml("#8BC34A"),
            ColorTranslator.FromHtml("#CDDC39"),
            ColorTranslator.FromHtml("#FFC107"),
            ColorTranslator.FromHtml("#FF9800"),
            ColorTranslator.FromHtml("#FF5722")
        };

        private static readonly Color[] HighContrastPalette = new[] {
            Color.Black,
            Color.White,
            ColorTranslator.FromHtml("#0D47A1"),
            ColorTranslator.FromHtml("#1B5E20"),
            ColorTranslator.FromHtml("#B71C1C"),
            ColorTranslator.FromHtml("#004D40"),
            ColorTranslator.FromHtml("#F57F17"),
            ColorTranslator.FromHtml("#4A148C")
        };

        internal static IEnumerable<Color> GetDefaultPalette() {
            return DefaultPalette;
        }

        internal static IEnumerable<Color> GetHighContrastPalette() {
            return HighContrastPalette;
        }

        internal static Color? SuggestColor(IEnumerable<Color?> existingColors) {
            HashSet<int> used = new HashSet<int>();
            if(existingColors != null) {
                foreach(Color? color in existingColors) {
                    if(color.HasValue) {
                        used.Add(color.Value.ToArgb());
                    }
                }
            }

            foreach(Color color in DefaultPalette) {
                if(!used.Contains(color.ToArgb())) {
                    return color;
                }
            }

            foreach(Color color in HighContrastPalette) {
                if(!used.Contains(color.ToArgb())) {
                    return color;
                }
            }

            if(DefaultPalette.Length > 0) {
                return DefaultPalette[0];
            }
            if(HighContrastPalette.Length > 0) {
                return HighContrastPalette[0];
            }
            return null;
        }

        internal static IEnumerable<Color> EnumerateAll() {
            return DefaultPalette.Concat(HighContrastPalette).Distinct(new ColorEqualityComparer());
        }

        private sealed class ColorEqualityComparer : IEqualityComparer<Color> {
            public bool Equals(Color x, Color y) {
                return x.ToArgb() == y.ToArgb();
            }

            public int GetHashCode(Color obj) {
                return obj.ToArgb();
            }
        }
    }
}


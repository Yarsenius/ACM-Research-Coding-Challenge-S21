using System;
using System.Collections.Generic;
using System.Text;
using VectSharp;

namespace GenMapper
{
    // This is a large struct, so it should be passed by reference.
    public struct MapSettings
    {
        public Colour LabelColour;
        public FontFamily LabelFontFamily;

        public uint FeatureRingRadius;
        public uint FeatureWidth;
        public int FeatureLabelFontSize;
        public uint FeatureLabelOffset;
        public Colour FeatureStrokeColour;
        public Colour FeatureFillColour;

        public uint MarkerRingRadius;
        public uint MarkCount;
        public uint MarkLength;
        public int MarkLabelFontSize;
        public uint MarkLabelOffset;

        public int OrganismLabelFontSize;

        public static MapSettings Default = new MapSettings()
        {
            LabelColour = Colours.Black,
            LabelFontFamily = new FontFamily(FontFamily.StandardFontFamilies.Helvetica),

            FeatureRingRadius = 325,
            FeatureWidth = 25,
            FeatureLabelFontSize = 15,
            FeatureLabelOffset = 10,
            FeatureStrokeColour = Colour.FromRgb(70, 25, 25),
            FeatureFillColour = Colour.FromRgba(70, 25, 25, 100),

            MarkerRingRadius = 150,
            MarkCount = 8,
            MarkLength = 10,
            MarkLabelFontSize = 12,
            MarkLabelOffset = 10,

            OrganismLabelFontSize = 17
        };
    }
}

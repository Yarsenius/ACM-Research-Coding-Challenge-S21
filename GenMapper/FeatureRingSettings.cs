using System;
using System.Collections.Generic;
using System.Text;
using VectSharp;

namespace GenMapper
{
    public struct FeatureRingSettings
    {
        public uint Radius;
        public uint FeatureWidth;

        public Font LabelFont;
        public Colour LabelColour;
        public uint LabelOffset;

        public Colour FeatureStrokeColour;
        public Colour FeatureFillColour;
        public Colour RingStrokeColour;
    }
}

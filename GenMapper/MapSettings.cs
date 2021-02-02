using VectSharp;

namespace GenMapper
{
    public struct MapSettings
    {
        public Point Center;
        public uint GenomeRingRadius;
        public uint MarkerRingRadius;
        public uint MarkCount;

        public uint FeatureWidth;
        public Colour FeatureFillColour;
        public Colour FeatureStrokeColour;

        public Font LabelFont;
        public Colour LabelColour;
        public uint LabelOffset;
    }
}
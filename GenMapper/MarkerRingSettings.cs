using System;
using System.Collections.Generic;
using System.Text;
using VectSharp;

namespace GenMapper
{
    public struct MarkerRingSettings
    {
        public uint Radius;
        public uint MarkCount;
        public uint MarkLength;

        public Font LabelFont;
        public Colour LabelColour;
        public uint LabelOffset;

        public Colour RingStrokeColor;
    }
}

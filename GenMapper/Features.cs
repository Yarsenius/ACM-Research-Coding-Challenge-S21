using System.Collections.Generic;

namespace GenMapper
{
    public readonly struct Features
    {
        public readonly string Organism;
        public readonly uint BasePositions;
        public readonly Dictionary<string, FeatureLocation> Locations;

        public Features(string organism, uint basePositions)
        {
            Organism = organism;
            BasePositions = basePositions;
            Locations = new Dictionary<string, FeatureLocation>();
        }
    }
}
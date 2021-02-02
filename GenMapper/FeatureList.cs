using System.Collections.Generic;

namespace GenMapper
{
    public readonly struct FeatureList
    {
        public readonly string Organism;
        public readonly uint BasePositions;
        public readonly Dictionary<string, FeatureLocation> Locations;

        public FeatureList(string organism, uint basePositions)
        {
            Organism = organism;
            BasePositions = basePositions;
            Locations = new Dictionary<string, FeatureLocation>();
        }
    }
}

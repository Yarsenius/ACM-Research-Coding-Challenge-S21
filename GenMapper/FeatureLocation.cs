namespace GenMapper
{
    public readonly struct FeatureLocation
    {
        public readonly uint Start;
        public readonly uint End;
        public readonly bool Complement;

        public FeatureLocation(uint start, uint end, bool complement)
        {
            Start = start;
            End = end;
            Complement = complement;
        }

        public override string ToString()
        {
            return $"FeatureLocation({Start}, {End}, {Complement})";
        }
    }
}
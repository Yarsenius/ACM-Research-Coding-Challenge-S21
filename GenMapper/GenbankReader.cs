using System;
using System.Collections.Generic;
using System.Globalization;

namespace GenMapper
{
    public static class GenbankReader
    {
        private const int LINE_LENGTH = 80;

        public static FeatureList? Read(ASCIILineReader lineReader)
        {
            if (lineReader == null)
                throw new ArgumentNullException("lineReader");

            char[] buffer = new char[LINE_LENGTH - 6];
            var featuresSpan = new Span<char>(buffer, 0, 8);

            // Read until we encounter the FEATURES keyword.
            while (!lineReader.ReadCharsExact(featuresSpan) || !featuresSpan.StartsWith("FEATURES"))
            {
                // Early termination in case we reach EOF.
                if (!lineReader.NextLine())
                    return null;
            }

            FeatureList? featureList = null;
            FeatureLocation? location = null;
            bool source = false; // flag indicating whether the current scope is part of the source key

            while (lineReader.NextLine())
            {
                int indentLength = lineReader.SkipConsecutive(' ');
                if (indentLength == 0)
                    return featureList;

                // Indent of five characters - potentially a feature key.
                if (indentLength == 5)
                {
                    int charsRead = lineReader.ReadChars(buffer);

                    // Since the feature location entry starts at position 21, we must have read
                    // at least 21 - 5 = 16 chars.
                    if (charsRead < 16)
                        continue;

                    location = ParseLocation(buffer, 16, charsRead - 16);
                    source = location.HasValue && MemoryExtensions.StartsWith<char>(buffer, "source");
                }
                else if (indentLength == 21 && location.HasValue) // Indent of 21 characters - potentially a feature qualifier.
                {
                    int charsRead = lineReader.ReadChars(buffer);
                    if (source)
                    {
                        if (!featureList.HasValue && charsRead > 10 && MemoryExtensions.StartsWith<char>(buffer, "/organism="))
                            featureList = new FeatureList(ParseQuotation(buffer, 10, charsRead - 10), location.Value.End);
                    }
                    else if (featureList.HasValue && charsRead > 6 && MemoryExtensions.StartsWith<char>(buffer, "/gene="))
                    {
                        string gene = ParseQuotation(buffer, 6, charsRead - 6);
                        if (gene != null)
                        {
                            Dictionary<string, FeatureLocation> locations = featureList.Value.Locations;
                            if (!locations.ContainsKey(gene))
                                locations.Add(gene, location.Value);
                        }
                    }
                }
            }
            return featureList;
        }
        
        // Helper method to parse feature locations.
        private static FeatureLocation? ParseLocation(char[] array, int offset, int count)
        {
            if (count < 4)
                return null;

            int startIndex = offset;
            bool complement = false;

            // Note that the ReadOnlySpan will check if the arguments are valid, 
            // so we do not need to perform our own checks.
            if (MemoryExtensions.StartsWith(new ReadOnlySpan<char>(array, offset, count), "complement("))
            {
                // "complement(x..x" is 15 characters (closing parentheses is optional).
                if (count < 15)
                    return null;
                startIndex += 11;
                complement = true;
            }

            if (array[startIndex] == '<')
                startIndex++;

            int index = startIndex;
            int end = offset + count;

            // First base position.
            while (index < end - 3 && char.IsDigit(array[index]))
                index++;

            // Attempt to parse the first set of digits as a positive int.
            var bpSpan = new ReadOnlySpan<char>(array, startIndex, index - startIndex);
            if (!uint.TryParse(bpSpan, NumberStyles.None, null, out uint rangeStart)) // could result in problems with different cultures?
                return null;

            // Ensure the separator between base positions is valid.
            if (array[index++] != '.' || array[index++] != '.')
                return null;

            if (array[index] == '>')
                index++;

            startIndex = index;

            // Second base position.
            while (index < end && char.IsDigit(array[index]))
                index++;

            // Attempt to parse the second set of digits as a positive int.
            bpSpan = new ReadOnlySpan<char>(array, startIndex, index - startIndex);
            if (!uint.TryParse(bpSpan, NumberStyles.None, null, out uint rangeEnd))
                return null;

            // Verify that the range is valid.
            if (rangeStart < 1 || rangeEnd < 1 || rangeStart > rangeEnd)
                return null;

            return new FeatureLocation(rangeStart, rangeEnd, complement);
        }

        // Finds the first and last quotation marks in a segment, and returns a string composed
        // of the characters between them, ignoring all characters outside the quotations marks.
        private static string ParseQuotation(char[] array, int offset, int count)
        {
            // Need at least two characters - leading and trailing quotation marks.
            if (count < 2)
                return null;

            if (array == null)
                throw new ArgumentNullException("array");

            if (offset >= array.Length || offset < 0)
                throw new ArgumentOutOfRangeException("offset");

            if (offset + count > array.Length)
                throw new ArgumentOutOfRangeException("count");

            int index = offset;
            int endIndex = offset + count - 1;

            // Find the leading quotation mark.
            while (array[index++] != '"')
            {
                // Early return if reached the last char in the segment.
                if (index >= endIndex)
                    return null;
            }

            int quotationStart = index;
            index = endIndex;

            // Find the trailing quotation mark.
            while (array[index] != '"')
            {
                if (--index < quotationStart)
                    return null;
            }

            return new string(array, quotationStart, index - quotationStart);
        }
    }
}
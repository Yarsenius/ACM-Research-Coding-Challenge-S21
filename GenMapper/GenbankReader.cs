using System;
using System.Collections.Generic;
using System.Globalization;

namespace GenMapper
{
    public static class GenbankReader
    {
        // *Most* Genbank files have a line length not exceeding 80 chars.
        private const int LINE_LENGTH = 80;

        // Feature keys are indented by five spaces.
        private const int INDENT_KEY = 5;

        // Feature qualifiers are indented by 21 spaces.
        private const int INDENT_QUALIFIER = 21;

        // Regardless of whether a particular line contains a key or a qualifier,
        // the actual content for that line will begin at position 21. Thus,
        // the maximum possible length of a feature key is 15 chars.
        private const int KEY_LENGTH = INDENT_QUALIFIER - INDENT_KEY;

        // Reads the feature table of a Genbank file, returning null if either no feature table
        // was found, or a feature table was found, but contained no source key.
        public static Features? ReadFeatures(ASCIILineReader lineReader)
        {
            if (lineReader == null)
                throw new ArgumentNullException("lineReader");

            char[] buffer = new char[LINE_LENGTH - INDENT_KEY];

            // The feature table begins with a line starting with the keyword FEATURES. 
            // Read the first eight characters of each line until EOF or the keyword is found.
            var temp = new Span<char>(buffer, 0, 8);
            while (lineReader.ReadChars(temp) < 8 || !temp.StartsWith("FEATURES"))
            {
                if (!lineReader.NextLine())
                    return null;
            }

            // Begin parsing the actual feature table.

            Features? features = null;
            FeatureLocation? location = null;

            // Flag indicating whether the current line is an entry corresponding to the source key.
            bool source = false;

            while (lineReader.NextLine())
            {
                int indentLength = lineReader.SkipConsecutive(' ');

                // Indent of zero - we've reached the end of the feature table.
                if (indentLength == 0)
                    return features;

                // Indent of INDENT_KEY characters - potentially a feature key.
                if (indentLength == INDENT_KEY)
                {
                    int charsRead = lineReader.ReadChars(buffer);
                    if (charsRead < KEY_LENGTH)
                        continue;

                    location = ParseLocation(buffer, KEY_LENGTH, charsRead - KEY_LENGTH);
                    source = location.HasValue && MemoryExtensions.StartsWith<char>(buffer, "source");
                }
                // Indent of INDENT_QUALIFIER characters - potentially a feature qualifier.
                else if (indentLength == INDENT_QUALIFIER && location.HasValue)
                {
                    int charsRead = lineReader.ReadChars(buffer);
                    if (source)
                    {
                        if (!features.HasValue && charsRead > 10 && MemoryExtensions.StartsWith<char>(buffer, "/organism="))
                            features = new Features(ParseQuotation(buffer, 10, charsRead - 10), location.Value.End);
                    }
                    else if (features.HasValue && charsRead > 6 && MemoryExtensions.StartsWith<char>(buffer, "/gene="))
                    {
                        string gene = ParseQuotation(buffer, 6, charsRead - 6);
                        if (gene != null)
                        {
                            Dictionary<string, FeatureLocation> locations = features.Value.Locations;
                            if (!locations.ContainsKey(gene))
                                locations.Add(gene, location.Value);
                        }
                    }
                }
            }
            return features;
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

            // Attempt to parse the first set of digits as a uint.
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

            // Attempt to parse the second set of digits as a uint.
            bpSpan = new ReadOnlySpan<char>(array, startIndex, index - startIndex);
            if (!uint.TryParse(bpSpan, NumberStyles.None, null, out uint rangeEnd))
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
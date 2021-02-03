using System;
using System.IO;
using VectSharp;
using VectSharp.Raster;

namespace GenMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Must provide at least two arguments!");
                return;
            }

            string importPath = args[0];
            string exportPath = args[1];

            Features features;

            try
            {
                using var fileStream = new FileStream(importPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var lineReader = new ASCIILineReader(fileStream);
                Features? featuresOptional = GenbankReader.ReadFeatures(lineReader);

                if (!featuresOptional.HasValue)
                {
                    Console.WriteLine("Failed to extract valid feature table data.");
                    return;
                }

                features = featuresOptional.Value;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing file '{importPath}'");
                Console.WriteLine(e.Message);
                return;
            }

            // Currently settings for the output are hardcoded, but it shouldn't be too difficult
            // to add some configurability.

            int size = 768;
            var page = new Page(size, size) { Background = Colours.White };
            CircleMapper.DrawMap(page.Graphics, features, new Point(size / 2, size / 2), in MapSettings.Default);

            try
            {
                Raster.SaveAsPNG(page, exportPath);
                Console.WriteLine($"Wrote file to '{exportPath}'");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error saving file to '{exportPath}'");
                Console.WriteLine(e.Message);
            }
        }
    }
}
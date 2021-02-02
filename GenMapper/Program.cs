using System;
using System.Collections.Generic;
using System.IO;
using VectSharp;
using VectSharp.Raster;

namespace GenMapper
{
    class Program
    {
        private const double TAU = 2 * Math.PI;
        private const double HALF_PI = 0.5 * Math.PI;

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Must provide at least two arguments!");
                return;
            }

            string importPath = args[0];
            string exportPath = args[1];

            FeatureList? featureList;

            try
            {
                using var fileStream = new FileStream(importPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var lineReader = new ASCIILineReader(fileStream);
                featureList = GenbankReader.Read(lineReader);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to parse file {importPath}");
                Console.WriteLine(e.Message);
                return;
            }

            if (!featureList.HasValue)
            {
                Console.WriteLine("Failed to ");
                return;
            }

            int size = 1024;

            Page page = new Page(size, size);
            page.Background = Colours.White;
            Graphics graphics = page.Graphics;

            var settings = new MapSettings
            {
                Center = new Point(size / 2, size / 2),
                GenomeRingRadius = 325,
                MarkerRingRadius = 150,
                MarkCount = 8,
                FeatureWidth = 25,
                FeatureFillColour = Colour.FromRgba(70, 25, 25, 100),
                FeatureStrokeColour = Colour.FromRgb(70, 25, 25),
                LabelFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), 17),
                LabelColour = Colours.Black,
                LabelOffset = 10
            };

            DrawMap(graphics, featureList.Value, settings);
            Raster.SaveAsPNG(page, exportPath);
        }

        private static void DrawMap(Graphics graphics, FeatureList featureList, MapSettings settings)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            Point center = settings.Center;
            double basePositionsFloat = featureList.BasePositions;

            foreach (KeyValuePair<string, FeatureLocation> entry in featureList.Locations)
            {
                FeatureLocation location = entry.Value;

                if (location.End <= basePositionsFloat)
                {
                    double startAngle = (double)location.Start / basePositionsFloat * TAU - HALF_PI;
                    double endAngle = (double)location.End / basePositionsFloat * TAU - HALF_PI;

                    double cosStart = Math.Cos(startAngle);
                    double sinStart = Math.Sin(startAngle);

                    GraphicsPath textGuide = new GraphicsPath();
                    GraphicsPath arc = new GraphicsPath();
                    arc.Arc(center, settings.GenomeRingRadius, endAngle, startAngle);

                    if (location.Complement)
                    {
                        double genomeInnerRadius = settings.GenomeRingRadius - settings.FeatureWidth;
                        double innerX = genomeInnerRadius * cosStart + center.X;
                        double innerY = genomeInnerRadius * sinStart + center.Y;

                        arc.LineTo(innerX, innerY);
                        arc.Arc(center, genomeInnerRadius, startAngle, endAngle);

                        double labelRadius = genomeInnerRadius - settings.LabelOffset;

                        textGuide.MoveTo(innerX, innerY);
                        textGuide.LineTo(labelRadius * cosStart + center.X, labelRadius * sinStart + center.Y);
                        graphics.FillTextOnPath(textGuide, entry.Key, settings.LabelFont, settings.LabelColour, 1, 
                            TextAnchors.Left, TextBaselines.Bottom);
                    }
                    else
                    {
                        double genomeOuterRadius = settings.GenomeRingRadius + settings.FeatureWidth;
                        double outerX = genomeOuterRadius * cosStart + center.X;
                        double outerY = genomeOuterRadius * sinStart + center.Y;

                        arc.LineTo(outerX, outerY);
                        arc.Arc(center, genomeOuterRadius, startAngle, endAngle);

                        double labelRadius = genomeOuterRadius + settings.LabelOffset;

                        textGuide.MoveTo(outerX, outerY);
                        textGuide.LineTo(labelRadius * cosStart + center.X, labelRadius * sinStart + center.Y);
                        graphics.FillTextOnPath(textGuide, entry.Key, settings.LabelFont, settings.LabelColour, 1);
                    }

                    arc.Close();
                    graphics.FillPath(arc, settings.FeatureFillColour);
                    graphics.StrokePath(arc, settings.FeatureStrokeColour);
                }
            }

            Size organismTextSize = graphics.MeasureText(featureList.Organism, settings.LabelFont);

            graphics.FillText(center.X - organismTextSize.Width / 2, center.Y, 
                featureList.Organism, settings.LabelFont, settings.LabelColour, TextBaselines.Middle);

            string basePositionText = $"{featureList.BasePositions} bp";

            graphics.FillText(center.X - graphics.MeasureText(basePositionText, settings.LabelFont).Width / 2, 
                center.Y + organismTextSize.Height, basePositionText, settings.LabelFont, settings.LabelColour, TextBaselines.Top);

            var genomeArc = new GraphicsPath();
            genomeArc.Arc(center, settings.GenomeRingRadius, 0, TAU);
            graphics.StrokePath(genomeArc, Colours.Black, 2);

            uint markerRingRadius = settings.MarkerRingRadius;
            var markerArc = new GraphicsPath();
            markerArc.Arc(center, settings.MarkerRingRadius, 0, TAU);
            graphics.StrokePath(markerArc, Colours.Black, 2);

            if (settings.MarkCount > 0)
            {
                uint markInterval = featureList.BasePositions / settings.MarkCount;

                if (markInterval == 0)
                    return;

                // Adjust markInterval to a more readable value.
                if (markInterval < 5)
                    markInterval = 1;
                else if (markInterval < 10)
                    markInterval = 5;
                else
                {
                    // Find greatest multiple of 5 * (the largest power of 10 less than subdivision).
                    uint current = 10;
                    uint next = 100;

                    while (next < markInterval)
                    {
                        current = next;
                        next *= 10;
                    }

                    current /= 4;
                    markInterval = (markInterval / current) * current;
                }

                Console.WriteLine(markInterval);

                // Draw the marks.
                uint markOuterRadius = (uint)markerRingRadius + (uint)settings.LabelOffset * 2;
                uint currentInterval = 0;

                for (uint i = 0; i < settings.MarkCount; ++i)
                {
                    double angle = (double)currentInterval / (double)featureList.BasePositions * TAU - HALF_PI;
                    double cosAngle = Math.Cos(angle);
                    double sinAngle = Math.Sin(angle);
                    Point endPoint = new Point(markOuterRadius * cosAngle + center.X, markOuterRadius * sinAngle + center.Y);

                    GraphicsPath marks = new GraphicsPath();
                    marks.MoveTo(markerRingRadius * cosAngle + center.X, markerRingRadius * sinAngle + center.Y);
                    marks.LineTo(endPoint);

                    string markLabelText = $"{currentInterval} bp";
                    //Size markLabelSize = graphics.MeasureText(markLabelText, settings.LabelFont);
                    graphics.FillTextOnPath(marks, markLabelText, settings.LabelFont, settings.LabelColour, 1, default, TextBaselines.Middle);
                    graphics.StrokePath(marks, Colours.Black, 2);

                    currentInterval += markInterval;
                }
            }
        }
    }
}
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
                Console.WriteLine($"Error parsing file {importPath}");
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

            var featureRingSettings = new FeatureRingSettings
            {
                Radius = 325,
                FeatureWidth = 25,
                LabelFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), 15),
                LabelColour = Colours.Black,
                LabelOffset = 10,
                FeatureStrokeColour = Colour.FromRgb(70, 25, 25),
                FeatureFillColour = Colour.FromRgba(70, 25, 25, 100),
                RingStrokeColour = Colours.Black
            };

            var markerRingSettings = new MarkerRingSettings
            {
                Radius = 150,
                MarkCount = 8,
                MarkLength = 15,
                LabelFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), 12),
                LabelColour = Colours.Black,
                LabelOffset = 10,
                RingStrokeColor = Colours.Black
            };

            var innerLabelFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), 17);
            var innerLabelColour = Colours.Black;

            var center = new Point(size / 2, size / 2);

            DrawFeatureRing(graphics, featureList.Value, center, in featureRingSettings);
            DrawMarkerRing(graphics, featureList.Value.BasePositions, center, in markerRingSettings);

            Size organismTextSize = graphics.MeasureText(featureList.Value.Organism, innerLabelFont);

            graphics.FillText(center.X - organismTextSize.Width / 2, center.Y,
                featureList.Value.Organism, innerLabelFont, innerLabelColour, TextBaselines.Middle);

            string basePositionText = $"{featureList.Value.BasePositions} bp";

            graphics.FillText(center.X - graphics.MeasureText(basePositionText, innerLabelFont).Width / 2,
                center.Y + organismTextSize.Height, basePositionText, innerLabelFont, innerLabelColour, TextBaselines.Top);

            Raster.SaveAsPNG(page, exportPath);
        }

        private static void DrawFeatureRing(Graphics graphics, FeatureList featureList, Point center, in FeatureRingSettings settings)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            double basePositionsFloat = featureList.BasePositions;

            foreach (KeyValuePair<string, FeatureLocation> entry in featureList.Locations)
            {
                FeatureLocation location = entry.Value;

                if (location.End <= basePositionsFloat)
                {
                    double startAngle = location.Start / basePositionsFloat * TAU;
                    double endAngle = location.End / basePositionsFloat * TAU;

                    double cosStart = Math.Cos(startAngle);
                    double sinStart = Math.Sin(startAngle);

                    GraphicsPath textGuide = new GraphicsPath();
                    GraphicsPath feature = new GraphicsPath();
                    feature.Arc(center, settings.Radius, endAngle, startAngle);

                    if (location.Complement)
                    {
                        double featureInnerRadius = settings.Radius - settings.FeatureWidth;
                        double innerX = featureInnerRadius * cosStart + center.X;
                        double innerY = featureInnerRadius * sinStart + center.Y;

                        feature.LineTo(innerX, innerY);
                        feature.Arc(center, featureInnerRadius, startAngle, endAngle);

                        double labelRadius = featureInnerRadius - settings.LabelOffset;

                        textGuide.MoveTo(innerX, innerY);
                        textGuide.LineTo(labelRadius * cosStart + center.X, labelRadius * sinStart + center.Y);
                        graphics.FillTextOnPath(textGuide, entry.Key, settings.LabelFont, settings.LabelColour, 1,
                            TextAnchors.Left, TextBaselines.Bottom);
                    }
                    else
                    {
                        double featureOuterRadius = settings.Radius + settings.FeatureWidth;
                        double outerX = featureOuterRadius * cosStart + center.X;
                        double outerY = featureOuterRadius * sinStart + center.Y;

                        feature.LineTo(outerX, outerY);
                        feature.Arc(center, featureOuterRadius, startAngle, endAngle);

                        double labelRadius = featureOuterRadius + settings.LabelOffset;

                        textGuide.MoveTo(outerX, outerY);
                        textGuide.LineTo(labelRadius * cosStart + center.X, labelRadius * sinStart + center.Y);
                        graphics.FillTextOnPath(textGuide, entry.Key, settings.LabelFont, settings.LabelColour, 1);
                    }

                    feature.Close();
                    graphics.FillPath(feature, settings.FeatureFillColour);
                    graphics.StrokePath(feature, settings.FeatureStrokeColour);
                }
            }

            var featureRing = new GraphicsPath();
            featureRing.Arc(center, settings.Radius, 0, TAU);
            graphics.StrokePath(featureRing, settings.RingStrokeColour, 2);
        }

        private static void DrawMarkerRing(Graphics graphics, uint basePositions, Point center, in MarkerRingSettings settings)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            var markerArc = new GraphicsPath();
            markerArc.Arc(center, settings.Radius, 0, TAU);
            graphics.StrokePath(markerArc, settings.RingStrokeColor, 2);

            if (settings.MarkCount < 1)
                return;

            uint markInterval = basePositions / settings.MarkCount;

            if (markInterval == 0)
                return;

            // Adjust markInterval to a more readable value.
            if (markInterval < 5)
                markInterval = 1;
            else if (markInterval < 10)
                markInterval = 5;
            else
            {
                // Find the largest power of 10 less than subdivision.
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

            uint outerRadius = settings.Radius + settings.MarkLength;
            uint labelRadius = outerRadius + settings.LabelOffset;
            uint currentInterval = 0;
            var marks = new GraphicsPath();

            for (uint i = 0; i < settings.MarkCount; ++i)
            {
                double angle = (double)currentInterval / basePositions * TAU - HALF_PI;
                double cosAngle = Math.Cos(angle);
                double sinAngle = Math.Sin(angle);

                marks.MoveTo(settings.Radius * cosAngle + center.X, settings.Radius * sinAngle + center.Y);
                marks.LineTo(outerRadius * cosAngle + center.X, outerRadius * sinAngle + center.Y);

                string labelText = $"{currentInterval} bp";
                Size labelSize = graphics.MeasureText(labelText, settings.LabelFont);

                Point labelCenter;
                if (cosAngle < 0)
                    labelCenter = new Point((labelRadius + labelSize.Width) * cosAngle + center.X, labelRadius * sinAngle + center.Y);
                else
                    labelCenter = new Point(labelRadius * cosAngle + center.X, labelRadius * sinAngle + center.Y);

                graphics.FillText(labelCenter, labelText, settings.LabelFont, settings.LabelColour, TextBaselines.Middle);
                currentInterval += markInterval;
            }
            graphics.StrokePath(marks, settings.RingStrokeColor, 2);
        }
    }
}
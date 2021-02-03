using System;
using System.Collections.Generic;
using VectSharp;

namespace GenMapper
{
    public static class CircleMapper
    {
        private const double TAU = Math.PI * 2;

        public static void DrawMap(Graphics graphics, Features features, Point center, in MapSettings settings)
        {
            DrawFeatureRing(graphics, features, center, settings);
            DrawMarkerRing(graphics, features.BasePositions, center, settings);
            DrawOrganismDescription(graphics, features, center, 
                new Font(settings.LabelFontFamily, settings.OrganismLabelFontSize), settings.LabelColour);
        }

        private static void DrawFeatureRing(Graphics graphics, Features features, Point center, in MapSettings settings)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            double basePositionsFloat = features.BasePositions;
            double featureInnerRadius = settings.FeatureRingRadius - settings.FeatureWidth;
            double featureOuterRadius = settings.FeatureRingRadius + settings.FeatureWidth;

            Font font = new Font(settings.LabelFontFamily, settings.FeatureLabelFontSize);

            foreach (KeyValuePair<string, FeatureLocation> entry in features.Locations)
            {
                FeatureLocation location = entry.Value;

                if (location.Start > location.End)
                    continue;

                if (location.End > basePositionsFloat)
                    continue;

                double startAngle = location.Start / basePositionsFloat * TAU;
                double endAngle = location.End / basePositionsFloat * TAU;

                double cosStart = Math.Cos(startAngle);
                double sinStart = Math.Sin(startAngle);

                GraphicsPath textGuide = new GraphicsPath();
                GraphicsPath feature = new GraphicsPath();
                feature.Arc(center, settings.FeatureRingRadius, endAngle, startAngle);

                if (location.Complement)
                {
                    double innerX = featureInnerRadius * cosStart + center.X;
                    double innerY = featureInnerRadius * sinStart + center.Y;

                    feature.LineTo(innerX, innerY);
                    feature.Arc(center, featureInnerRadius, startAngle, endAngle);

                    double labelRadius = featureInnerRadius - settings.FeatureLabelOffset;

                    textGuide.MoveTo(innerX, innerY);
                    textGuide.LineTo(labelRadius * cosStart + center.X, labelRadius * sinStart + center.Y);
                    graphics.FillTextOnPath(textGuide, entry.Key, font, settings.LabelColour, 1, TextAnchors.Left, TextBaselines.Bottom);
                }
                else
                {
                    double outerX = featureOuterRadius * cosStart + center.X;
                    double outerY = featureOuterRadius * sinStart + center.Y;

                    feature.LineTo(outerX, outerY);
                    feature.Arc(center, featureOuterRadius, startAngle, endAngle);

                    double labelRadius = featureOuterRadius + settings.FeatureLabelOffset;

                    textGuide.MoveTo(outerX, outerY);
                    textGuide.LineTo(labelRadius * cosStart + center.X, labelRadius * sinStart + center.Y);
                    graphics.FillTextOnPath(textGuide, entry.Key, font, settings.LabelColour, 1);
                }

                feature.Close();
                graphics.FillPath(feature, settings.FeatureFillColour);
                graphics.StrokePath(feature, settings.FeatureStrokeColour);
            }

            var guideRing = new GraphicsPath();
            guideRing.Arc(center, settings.FeatureRingRadius, 0, TAU);
            graphics.StrokePath(guideRing, settings.LabelColour, 2);
        }

        private static void DrawMarkerRing(Graphics graphics, uint basePositions, Point center, in MapSettings settings)
        {
            var markerRing = new GraphicsPath();
            markerRing.Arc(center, settings.MarkerRingRadius, 0, TAU);
            graphics.StrokePath(markerRing, settings.LabelColour, 2);

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

            uint outerRadius = settings.MarkerRingRadius + settings.MarkLength;
            uint labelRadius = outerRadius + settings.MarkLabelOffset;
            Font font = new Font(settings.LabelFontFamily, settings.MarkLabelFontSize);
            var marks = new GraphicsPath();

            uint currentInterval = 0;
            for (uint i = 0; i < settings.MarkCount; ++i)
            {
                double angle = (double)currentInterval / basePositions * TAU;
                double cosAngle = Math.Cos(angle);
                double sinAngle = Math.Sin(angle);

                marks.MoveTo(settings.MarkerRingRadius * cosAngle + center.X, settings.MarkerRingRadius * sinAngle + center.Y);
                marks.LineTo(outerRadius * cosAngle + center.X, outerRadius * sinAngle + center.Y);

                string labelText = $"{currentInterval} bp";
                Size labelSize = graphics.MeasureText(labelText, font);

                Point labelPosition;
                if (cosAngle < 0)
                    labelPosition = new Point((labelRadius + labelSize.Width) * cosAngle + center.X, labelRadius * sinAngle + center.Y);
                else
                    labelPosition = new Point(labelRadius * cosAngle + center.X, labelRadius * sinAngle + center.Y);

                graphics.FillText(labelPosition, labelText, font, settings.LabelColour, TextBaselines.Middle);
                currentInterval += markInterval;
            }
            graphics.StrokePath(marks, settings.LabelColour, 2);
        }

        private static void DrawOrganismDescription(Graphics graphics, Features features, Point center, Font font, Colour labelColour)
        {
            Size organismTextSize = graphics.MeasureText(features.Organism, font);

            graphics.FillText(center.X - organismTextSize.Width / 2, center.Y, features.Organism, font, labelColour, TextBaselines.Middle);

            string basePositionText = $"{features.BasePositions} bp";

            graphics.FillText(center.X - graphics.MeasureText(basePositionText, font).Width / 2,
                center.Y + organismTextSize.Height, basePositionText, font, labelColour, TextBaselines.Top);
        }
    }
}
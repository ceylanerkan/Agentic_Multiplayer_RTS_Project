using System;
using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>
    /// Simple group formation: a roughly square grid centred on the target point, so a group of units
    /// does not pile up on one spot. Returns (x, z) offsets, one per unit, nearest the centre first.
    /// </summary>
    public static class FormationLayout
    {
        public static List<float[]> Offsets(int count, float spacing)
        {
            var result = new List<float[]>();
            if (count <= 0) return result;
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling(count / (double)cols);
            for (int i = 0; i < count; i++)
            {
                int c = i % cols, r = i / cols;
                float x = (c - (cols - 1) / 2f) * spacing;
                float z = (r - (rows - 1) / 2f) * spacing;
                result.Add(new[] { x, z });
            }
            // Sort so that unit 0 gets the spot nearest to the clicked point.
            result.Sort((a, b) => (a[0] * a[0] + a[1] * a[1]).CompareTo(b[0] * b[0] + b[1] * b[1]));
            return result;
        }
    }
}

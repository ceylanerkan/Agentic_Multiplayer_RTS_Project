using System.Collections.Generic;

namespace RTS.Core.Rules
{
    /// <summary>An object on the map as the visibility rules see it (no Unity types).</summary>
    public class MapObject
    {
        public int Id;
        public int Owner;          // player slot 1 or 2
        public string Type;        // unit or building key
        public bool IsBuilding;
        public float X, Z;
        public float Los;          // line of sight radius (already includes tech bonuses)
    }

    /// <summary>
    /// The single visibility query (design brief, fog of war). A player sees:
    ///  - all of their own objects, and
    ///  - enemy objects whose centre is inside the line-of-sight circle of at least one own object.
    /// Rendering and the AI snapshot both call FilterFor, so the AI can never know more than the player.
    /// </summary>
    public static class VisibilityService
    {
        public static bool IsVisibleTo(int player, MapObject target, IList<MapObject> all)
        {
            if (target.Owner == player) return true;
            foreach (var o in all)
            {
                if (o.Owner != player) continue;
                float dx = o.X - target.X, dz = o.Z - target.Z;
                if (dx * dx + dz * dz <= o.Los * o.Los) return true;
            }
            return false;
        }

        public static List<MapObject> FilterFor(int player, IList<MapObject> all)
        {
            var result = new List<MapObject>();
            foreach (var t in all) if (IsVisibleTo(player, t, all)) result.Add(t);
            return result;
        }

        /// <summary>True when the point (x, z) is inside the sight of any of the player's objects (used for the fog texture).</summary>
        public static bool PointVisible(int player, float x, float z, IList<MapObject> all)
        {
            foreach (var o in all)
            {
                if (o.Owner != player) continue;
                float dx = o.X - x, dz = o.Z - z;
                if (dx * dx + dz * dz <= o.Los * o.Los) return true;
            }
            return false;
        }
    }
}

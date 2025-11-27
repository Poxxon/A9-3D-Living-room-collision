using OpenTK.Mathematics;

namespace LivingRoom3D
{
    public interface ICollider
    {
        string Name { get; }
        bool IsTrigger { get; }
        bool IntersectsSphere(Vector3 center, float radius);
    }

    public sealed class AabbCollider : ICollider
    {
        public string Name { get; }
        public bool IsTrigger { get; }
        public Vector3 Min { get; }
        public Vector3 Max { get; }

        public AabbCollider(Vector3 min, Vector3 max, string name, bool isTrigger = false)
        {
            Min = min;
            Max = max;
            Name = name;
            IsTrigger = isTrigger;
        }

        public static AabbCollider FromCenterSize(Vector3 center, Vector3 size, string name, bool isTrigger = false)
        {
            var half = size * 0.5f;
            return new AabbCollider(center - half, center + half, name, isTrigger);
        }

        public bool IntersectsSphere(Vector3 center, float radius)
        {
            float closestX = MathHelper.Clamp(center.X, Min.X, Max.X);
            float closestY = MathHelper.Clamp(center.Y, Min.Y, Max.Y);
            float closestZ = MathHelper.Clamp(center.Z, Min.Z, Max.Z);

            float distSq =
                (center.X - closestX) * (center.X - closestX) +
                (center.Y - closestY) * (center.Y - closestY) +
                (center.Z - closestZ) * (center.Z - closestZ);

            return distSq < radius * radius;
        }
    }
}

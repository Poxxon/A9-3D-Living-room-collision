using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace LivingRoom3D
{
    public sealed class PlayerController
    {
        public Camera Camera { get; }
        public float Speed { get; set; } = 5.0f;
        public float Radius { get; set; } = 0.4f;
        public float EyeHeight { get; set; } = 1.7f;

        public PlayerController(Camera camera)
        {
            Camera = camera;
        }

        public void Update(KeyboardState input, float deltaTime, IReadOnlyList<ICollider> solidColliders)
        {
            Vector3 moveDirection = Vector3.Zero;
            Vector3 forwardXZ = Vector3.Normalize(new Vector3(Camera.Front.X, 0, Camera.Front.Z));
            Vector3 rightXZ = Vector3.Normalize(new Vector3(Camera.Right.X, 0, Camera.Right.Z));

            if (input.IsKeyDown(Keys.W)) moveDirection += forwardXZ;
            if (input.IsKeyDown(Keys.S)) moveDirection -= forwardXZ;
            if (input.IsKeyDown(Keys.A)) moveDirection -= rightXZ;
            if (input.IsKeyDown(Keys.D)) moveDirection += rightXZ;

            if (moveDirection.LengthSquared > 0f)
            {
                moveDirection = Vector3.Normalize(moveDirection);
            }

            Vector3 current = Camera.Position;
            current.Y = EyeHeight;

            Vector3 desired = current + moveDirection * Speed * deltaTime;
            desired.Y = EyeHeight;

            if (!WouldCollide(desired, solidColliders))
            {
                Camera.Position = desired;
                return;
            }

            // Try sliding on X and Z separately for smoother blocking.
            Vector3 slideX = new Vector3(desired.X, EyeHeight, current.Z);
            if (!WouldCollide(slideX, solidColliders))
            {
                Camera.Position = slideX;
                return;
            }

            Vector3 slideZ = new Vector3(current.X, EyeHeight, desired.Z);
            if (!WouldCollide(slideZ, solidColliders))
            {
                Camera.Position = slideZ;
                return;
            }

            // Fully blocked: stay put.
            Camera.Position = current;
        }

        public Vector3 Position => Camera.Position;

        private bool WouldCollide(Vector3 newPos, IReadOnlyList<ICollider> colliders)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                // Treat collisions in XZ plane (ignore height so low objects still block movement)
                if (colliders[i].IsTrigger)
                    continue;

                if (IntersectsXZ(colliders[i], newPos, Radius))
                    return true;
            }
            return false;
        }

        private bool IntersectsXZ(ICollider collider, Vector3 center, float radius)
        {
            // We assume collider is an AABB; use XZ projection for simpler blocking.
            if (collider is not AabbCollider aabb)
                return collider.IntersectsSphere(center, radius);

            float closestX = MathHelper.Clamp(center.X, aabb.Min.X, aabb.Max.X);
            float closestZ = MathHelper.Clamp(center.Z, aabb.Min.Z, aabb.Max.Z);

            float dx = center.X - closestX;
            float dz = center.Z - closestZ;

            return dx * dx + dz * dz < radius * radius;
        }
    }
}

using OpenTK.Mathematics;

namespace LivingRoom3D
{
    public sealed class GameObject
    {
        public string Name { get; init; } = string.Empty;
        public Vector3 Position { get; init; }
        public Vector3 Scale { get; init; } = Vector3.One;
        public Vector3 Rotation { get; init; } = Vector3.Zero; // radians (pitch, yaw, roll)
        public int TextureId { get; init; }
        public Model? Model { get; init; }
        public ICollider? Collider { get; init; }

        public Matrix4 ModelMatrix
        {
            get
            {
                var rot = Matrix4.CreateRotationX(Rotation.X) *
                          Matrix4.CreateRotationY(Rotation.Y) *
                          Matrix4.CreateRotationZ(Rotation.Z);

                return Matrix4.CreateScale(Scale) *
                       rot *
                       Matrix4.CreateTranslation(Position);
            }
        }
    }
}

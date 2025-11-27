using OpenTK.Mathematics;

namespace LivingRoom3D
{
    public class Camera
    {
        public Vector3 Position;
        public Vector3 Front = -Vector3.UnitZ;
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Right => Vector3.Normalize(Vector3.Cross(Front, Up));

        private float _yaw = -90.0f;
        private float _pitch = 0.0f;

        public float Fov = 60.0f;

        public Camera(Vector3 position)
        {
            Position = position;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        public void ProcessMouse(float deltaX, float deltaY, float sensitivity = 0.1f)
        {
            _yaw += deltaX * sensitivity;
            _pitch -= deltaY * sensitivity;

            if (_pitch > 89.0f) _pitch = 89.0f;
            if (_pitch < -89.0f) _pitch = -89.0f;

            var front = new Vector3
            {
                X = MathF.Cos(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch)),
                Y = MathF.Sin(MathHelper.DegreesToRadians(_pitch)),
                Z = MathF.Sin(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch))
            };

            Front = Vector3.Normalize(front);
        }
    }
}

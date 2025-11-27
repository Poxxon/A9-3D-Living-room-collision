using System;
using OpenTK.Windowing.Desktop;

namespace LivingRoom3D
{
    internal static class Program
    {
        static void Main()
        {
            var nativeSettings = new NativeWindowSettings()
            {
                Size = new OpenTK.Mathematics.Vector2i(1600, 900),
                Title = "OpenTK Living Room - FPS Camera & Collision"
            };

            using (var game = new Game(GameWindowSettings.Default, nativeSettings))
            {
                game.Run();
            }
        }
    }
}

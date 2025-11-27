using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;

namespace LivingRoom3D
{
    public class Game : GameWindow
    {
        // GL objects
        private int _cubeVao;
        private int _cubeVbo;
        private int _cubeEbo;

        private int _shaderProgram;

        // Textures
        private int _floorTexture;
        private int _wallTexture;
        private int _sofaTexture;
        private int _tableTexture;
        private int _tvStandTexture;
        private int _defaultModelTexture;
        private int _doorTexture;
        private int _ottomanTexture;
        private int _plantTexture;
        private int _megaphoneTexture;
        private int _televisionTexture;
        private int _crosshairDefaultTex;
        private int _crosshairInteractTex;
        private int _crosshairVao;
        private int _crosshairVbo;
        private int _crosshairVertCount;
        private int _messageTex;
        private float _messageTexWidth;
        private float _messageTexHeight;
        private Vector3 _messageWorldPos;
        private double _messageExpires;

        // Camera
        private Camera _camera = null!;
        private PlayerController _player = null!;
        private bool _firstMouse = true;
        private Vector2 _lastMousePos;

        // Timing
        private double _lastTime;

        // Scene data
        private readonly List<GameObject> _objects = new();
        private readonly List<ICollider> _solidColliders = new();
        private readonly List<ICollider> _triggerColliders = new();
        private string? _prompt;
        private readonly string _baseTitle = "OpenTK Living Room - FPS Camera & Collision";
        private Model? _npcModel;
        private Model? _sofaModel;
        private Model? _tableModel;
        private Model? _ottomanModel;
        private Model? _megaphoneModel;
        private Model? _televisionModel;
        private AudioService _audio = null!;
        private bool _canInteract;
        private string? _lastInteractMessage;

        public Game(GameWindowSettings gws, NativeWindowSettings nws)
            : base(gws, nws)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.1f, 0.12f, 0.15f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            CursorState = CursorState.Grabbed;

            _camera = new Camera(new Vector3(0f, 1.7f, 6f));
            _player = new PlayerController(_camera)
            {
                EyeHeight = 1.7f,
                Radius = 0.4f,
                Speed = 5.0f
            };

            _audio = new AudioService();

            CreateShader();
            CreateCube();
            CreateQuad();
            CreateTextures();
            LoadModels();
            LoadAudio();
            CreateScene();

            _lastTime = GLFW.GetTime();
        }

        #region GL Setup

        private void CreateShader()
        {
            const string vertexShaderSource = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 vTexCoord;

void main()
{
    vTexCoord = aTexCoord;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}
";

            const string fragmentShaderSource = @"
#version 330 core
in vec2 vTexCoord;
out vec4 FragColor;

uniform sampler2D uTexture;

void main()
{
    FragColor = texture(uTexture, vTexCoord);
}
";

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vertexShaderSource);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int vStatus);
            if (vStatus != (int)All.True)
            {
                Console.WriteLine(GL.GetShaderInfoLog(vs));
                throw new Exception("Vertex shader compile error");
            }

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fragmentShaderSource);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out int fStatus);
            if (fStatus != (int)All.True)
            {
                Console.WriteLine(GL.GetShaderInfoLog(fs));
                throw new Exception("Fragment shader compile error");
            }

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vs);
            GL.AttachShader(_shaderProgram, fs);
            GL.LinkProgram(_shaderProgram);

            GL.GetProgram(_shaderProgram, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus != (int)All.True)
            {
                Console.WriteLine(GL.GetProgramInfoLog(_shaderProgram));
                throw new Exception("Program link error");
            }

            GL.DetachShader(_shaderProgram, vs);
            GL.DetachShader(_shaderProgram, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
        }

        private void CreateCube()
        {
            // 24 vertices (4 per face) with position + texcoord
            float[] vertices =
            {
                // Face: +Z
                -0.5f, -0.5f,  0.5f, 0f, 0f,
                 0.5f, -0.5f,  0.5f, 1f, 0f,
                 0.5f,  0.5f,  0.5f, 1f, 1f,
                -0.5f,  0.5f,  0.5f, 0f, 1f,

                // Face: -Z
                 0.5f, -0.5f, -0.5f, 0f, 0f,
                -0.5f, -0.5f, -0.5f, 1f, 0f,
                -0.5f,  0.5f, -0.5f, 1f, 1f,
                 0.5f,  0.5f, -0.5f, 0f, 1f,

                // Face: +X
                 0.5f, -0.5f,  0.5f, 0f, 0f,
                 0.5f, -0.5f, -0.5f, 1f, 0f,
                 0.5f,  0.5f, -0.5f, 1f, 1f,
                 0.5f,  0.5f,  0.5f, 0f, 1f,

                // Face: -X
                -0.5f, -0.5f, -0.5f, 0f, 0f,
                -0.5f, -0.5f,  0.5f, 1f, 0f,
                -0.5f,  0.5f,  0.5f, 1f, 1f,
                -0.5f,  0.5f, -0.5f, 0f, 1f,

                // Face: +Y
                -0.5f,  0.5f,  0.5f, 0f, 0f,
                 0.5f,  0.5f,  0.5f, 1f, 0f,
                 0.5f,  0.5f, -0.5f, 1f, 1f,
                -0.5f,  0.5f, -0.5f, 0f, 1f,

                // Face: -Y
                -0.5f, -0.5f, -0.5f, 0f, 0f,
                 0.5f, -0.5f, -0.5f, 1f, 0f,
                 0.5f, -0.5f,  0.5f, 1f, 1f,
                -0.5f, -0.5f,  0.5f, 0f, 1f,
            };

            uint[] indices =
            {
                0, 1, 2, 2, 3, 0,       // +Z
                4, 5, 6, 6, 7, 4,       // -Z
                8, 9,10,10,11, 8,       // +X
               12,13,14,14,15,12,       // -X
               16,17,18,18,19,16,       // +Y
               20,21,22,22,23,20        // -Y
            };

            _cubeVao = GL.GenVertexArray();
            _cubeVbo = GL.GenBuffer();
            _cubeEbo = GL.GenBuffer();

            GL.BindVertexArray(_cubeVao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            int stride = 5 * sizeof(float);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

            GL.BindVertexArray(0);
        }

        private int CreateCheckerTexture(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
        {
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            const int size = 64;
            byte[] data = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool even = ((x / 8) + (y / 8)) % 2 == 0;
                    int index = (y * size + x) * 4;
                    if (even)
                    {
                        data[index + 0] = r1;
                        data[index + 1] = g1;
                        data[index + 2] = b1;
                        data[index + 3] = 255;
                    }
                    else
                    {
                        data[index + 0] = r2;
                        data[index + 1] = g2;
                        data[index + 2] = b2;
                        data[index + 3] = 255;
                    }
                }
            }

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size, size, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return tex;
        }

        private void CreateTextures()
        {
            // Load floor and wall from texture files; fall back to checkers if missing.
            _floorTexture = TryLoadTextureFromFile("Models/textures/wood_floor_diff_2k.jpg")
                            ?? CreateCheckerTexture(180, 180, 180, 150, 150, 150);
            _wallTexture = TryLoadTextureFromFile("Models/textures/plastered_wall_diff_2k.jpg")
                           ?? CreateCheckerTexture(230, 230, 240, 210, 210, 220);
            _doorTexture = TryLoadTextureFromFile("Models/textures/rough_pine_door_diff_1k.jpg", TextureWrapMode.ClampToEdge)
                           ?? TryLoadTextureFromFile("Models/textures/oak_veneer_01_diff_2k_1.jpeg", TextureWrapMode.ClampToEdge)
                           ?? TryLoadTextureFromFile("Models/textures/door.png", TextureWrapMode.ClampToEdge)
                           ?? _wallTexture;
            _ottomanTexture = TryLoadTextureFromFile("Models/textures/Ottoman_01_diff_2k.jpg")
                              ?? _tableTexture;
            _plantTexture = TryLoadTextureFromFile("Models/textures/potted_plant_01_pot_diff_2k.jpg")
                             ?? _defaultModelTexture;
            _megaphoneTexture = TryLoadTextureFromFile("Models/textures/Megaphone_01_diff_2k.jpg")
                                ?? _defaultModelTexture;
            _televisionTexture = TryLoadTextureFromFile("Models/textures/Television_01_diff_2k.jpg")
                                 ?? _tvStandTexture;
            _sofaTexture = CreateCheckerTexture(40, 60, 120, 25, 40, 90);       // dark blue sofa
            _tableTexture = CreateCheckerTexture(120, 80, 40, 100, 60, 30);     // wood
            _tvStandTexture = CreateCheckerTexture(30, 30, 30, 60, 60, 60);     // dark
            _defaultModelTexture = CreateCheckerTexture(200, 200, 220, 180, 180, 200);
            _crosshairDefaultTex = TryLoadTextureFromFile("Models/Crosshair/default.png", TextureWrapMode.ClampToEdge)
                                   ?? CreateCrosshairTexture(24, 2, 8, 255, 255, 255);
            _crosshairInteractTex = TryLoadTextureFromFile("Models/Crosshair/interactable.png", TextureWrapMode.ClampToEdge)
                                    ?? CreateCrosshairTexture(32, 3, 10, 255, 220, 80);
        }

        private void CreateQuad()
        {
            // Simple quad centered at origin in NDC, drawn as triangle strip
            float[] vertices =
            {
                // pos.xy   tex.xy
                -0.5f, -0.5f, 0f, 0f,
                 0.5f, -0.5f, 1f, 0f,
                -0.5f,  0.5f, 0f, 1f,
                 0.5f,  0.5f, 1f, 1f
            };

            _crosshairVao = GL.GenVertexArray();
            _crosshairVbo = GL.GenBuffer();
            _crosshairVertCount = 4;

            GL.BindVertexArray(_crosshairVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _crosshairVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            int stride = 4 * sizeof(float);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));

            GL.BindVertexArray(0);
        }

        private int? TryLoadTextureFromFile(string path, TextureWrapMode? wrapOverride = null)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                StbImageSharp.StbImage.stbi_set_flip_vertically_on_load(1);
                using var stream = File.OpenRead(path);
                var image = StbImageSharp.ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                int tex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, tex);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                var wrap = wrapOverride ?? TextureWrapMode.Repeat;
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                return tex;
            }
            catch
            {
                return null;
            }
        }

        private int CreateCrosshairTexture(int size, int thickness, int gap, byte r, byte g, byte b)
        {
            byte[] data = new byte[size * size * 4];
            int center = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onVertical = Math.Abs(x - center) < thickness && Math.Abs(y - center) > gap;
                    bool onHorizontal = Math.Abs(y - center) < thickness && Math.Abs(x - center) > gap;
                    bool draw = onVertical || onHorizontal;
                    int idx = (y * size + x) * 4;
                    if (draw)
                    {
                        data[idx + 0] = r;
                        data[idx + 1] = g;
                        data[idx + 2] = b;
                        data[idx + 3] = 255;
                    }
                    else
                    {
                        data[idx + 0] = 0;
                        data[idx + 1] = 0;
                        data[idx + 2] = 0;
                        data[idx + 3] = 0;
                    }
                }
            }

            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size, size, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            return tex;
        }

        private void ShowInteractMessage(string text, ICollider trigger)
        {
            if (_lastInteractMessage == text && GLFW.GetTime() < _messageExpires)
                return;

            _lastInteractMessage = text;
            _messageWorldPos = GetColliderCenter(trigger);
            _messageWorldPos.Y += 1.2f; // lift above object
            _messageExpires = GLFW.GetTime() + 1.8;

            if (_messageTex != 0)
            {
                GL.DeleteTexture(_messageTex);
                _messageTex = 0;
            }

            CreateTextTexture(text, out _messageTex, out _messageTexWidth, out _messageTexHeight);
        }

        private Vector3 GetColliderCenter(ICollider collider)
        {
            if (collider is AabbCollider aabb)
            {
                return (aabb.Min + aabb.Max) * 0.5f;
            }
            return Vector3.Zero;
        }

#pragma warning disable CA1416 // Drawing APIs only on Windows
        private void CreateTextTexture(string text, out int tex, out float width, out float height)
        {
            using var bmp = new SD.Bitmap(256, 64, SDI.PixelFormat.Format32bppArgb);
            using var g = SD.Graphics.FromImage(bmp);
            g.Clear(SD.Color.Transparent);
            using var font = new SD.Font(SD.FontFamily.GenericSansSerif, 18, SD.FontStyle.Bold, SD.GraphicsUnit.Pixel);
            var size = g.MeasureString(text, font);
            g.TextRenderingHint = SD.Text.TextRenderingHint.AntiAlias;
            g.DrawString(text, font, SD.Brushes.White, new SD.PointF((bmp.Width - size.Width) / 2f, (bmp.Height - size.Height) / 2f));

            width = (float)size.Width / bmp.Width;
            height = (float)size.Height / bmp.Height;

            var data = bmp.LockBits(new SD.Rectangle(0, 0, bmp.Width, bmp.Height), SDI.ImageLockMode.ReadOnly, bmp.PixelFormat);
            tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }
#pragma warning restore CA1416

        private void LoadModels()
        {
            _npcModel?.Dispose();
            _sofaModel?.Dispose();
            _tableModel?.Dispose();
            _ottomanModel?.Dispose();
            _megaphoneModel?.Dispose();
            _televisionModel?.Dispose();

            _npcModel = Model.Load("Models/potted_plant_01_2k.gltf", _plantTexture);
            _sofaModel = Model.Load("Models/sofa_03_2k.gltf", _defaultModelTexture);
            _tableModel = Model.Load("Models/WoodenTable_01_2k.gltf", _defaultModelTexture);
            _ottomanModel = Model.Load("Models/Ottoman_01_2k.gltf", _ottomanTexture);
            _megaphoneModel = Model.Load("Models/Megaphone_01_2k.gltf", _megaphoneTexture);
            _televisionModel = Model.Load("Models/Television_01_2k.gltf", _televisionTexture);
        }

        private void LoadAudio()
        {
            _audio.Load("door", "Models/Audios/door-knock_e3LiVYz.mp3");
            _audio.Load("megaphone", "Models/Audios/loop-megaphone-voice-in-beijing-99892.mp3");
            _audio.Load("television", "Models/Audios/phone-static.mp3");
        }

        private void CreateScene()
        {
            _objects.Clear();
            _solidColliders.Clear();
            _triggerColliders.Clear();

            float wallHeight = 3f;
            float halfRoom = 8f;
            float wallThickness = 0.4f;

            // Floor (render only; no collision so movement isn't blocked in XZ)
            AddBox("Floor", new Vector3(16f, 0.1f, 16f), new Vector3(0f, -0.05f, 0f), _floorTexture, solid: false);

            // Walls
            AddBox("Back Wall", new Vector3(16f, wallHeight, wallThickness), new Vector3(0f, wallHeight / 2f, -halfRoom - wallThickness / 2f), _wallTexture);
            AddBox("Front Wall", new Vector3(16f, wallHeight, wallThickness), new Vector3(0f, wallHeight / 2f, halfRoom + wallThickness / 2f), _wallTexture);
            AddBox("Left Wall", new Vector3(wallThickness, wallHeight, 16f), new Vector3(-halfRoom - wallThickness / 2f, wallHeight / 2f, 0f), _wallTexture);
            AddBox("Right Wall", new Vector3(wallThickness, wallHeight, 16f), new Vector3(halfRoom + wallThickness / 2f, wallHeight / 2f, 0f), _wallTexture);

            // Furniture (solid)
            // Table remains at the front area (collider matches visual size)
            // Slightly oversized collider to cover full tabletop footprint
            AddObject("Wood Table", new Vector3(2.2f, 1.0f, 1.4f), new Vector3(0f, 0f, -3f), _tableTexture, _tableModel, solid: true, colliderScale: new Vector3(3.6f, 0.8f, 1.2f));
            // TV now sits on top of the table
            AddObject("Television", new Vector3(1.6f, 1.0f, 1f), new Vector3(0f, 0.55f, -3f), _televisionTexture, _televisionModel, solid: true, trigger: true, rotation: new Vector3(0f, MathF.PI, 0f));
            // Sofa collider enlarged to cover arms and prevent side clipping
            AddObject("Sofa", new Vector3(2.5f, 1.1f, 1.2f), new Vector3(0f, 0f, -7f), _sofaTexture, _sofaModel, solid: true, trigger: true, colliderScale: new Vector3(6f, 1.2f, 1.2f));
            AddObject("Megaphone", new Vector3(1.1f, 1.1f, 1.1f), new Vector3(-4f, 0.9f, -2f), _megaphoneTexture, _megaphoneModel, solid: true, trigger: true);
            AddObject("Ottoman", new Vector3(1.5f, 1.2f, 1.0f), new Vector3(-4f, 0f, -2f), _ottomanTexture, _ottomanModel, solid: true);

            // Interaction zones (triggers only)
            // Door: very thin and slightly pulled off the wall so the texture covers the full face
            AddObject("Door",
                new Vector3(1.5f, 3.0f, 0.05f),
                new Vector3(-6f, 1.5f, halfRoom - 0.05f),
                _doorTexture,
                model: null,
                solid: true,
                trigger: true,
                colliderScale: new Vector3(1.5f, 3.0f, 0.1f));
            AddObject("Plant", new Vector3(1.0f, 1.6f, 1.0f), new Vector3(5f, 0f, -5.5f), _plantTexture, _npcModel, solid: true, trigger: true);
        }

        private void AddBox(string name, Vector3 scale, Vector3 position, int textureId, bool solid = true, bool trigger = false, Vector3? rotation = null)
        {
            AddObject(name, scale, position, textureId, null, solid, trigger, rotation);
        }

        private void AddObject(string name, Vector3 scale, Vector3 position, int textureId, Model? model, bool solid = true, bool trigger = false, Vector3? rotation = null, Vector3? colliderScale = null, Vector3? colliderOffset = null)
        {
            var solidSize = colliderScale ?? scale;
            var collOffset = colliderOffset ?? Vector3.Zero;
            Vector3 collPos = position + collOffset;

            ICollider? solidCollider = solid ? AabbCollider.FromCenterSize(collPos, solidSize, name, false) : null;
            ICollider? triggerCollider = trigger ? AabbCollider.FromCenterSize(collPos, solidSize, name, true) : null;

            var obj = new GameObject
            {
                Name = name,
                Position = position,
                Scale = scale,
                Rotation = rotation ?? Vector3.Zero,
                TextureId = textureId,
                Model = model,
                Collider = solidCollider ?? triggerCollider
            };

            _objects.Add(obj);

            if (solidCollider != null)
                _solidColliders.Add(solidCollider);
            if (triggerCollider != null)
                _triggerColliders.Add(triggerCollider);
        }

        #endregion

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (!IsFocused)
                return;

            double currentTime = GLFW.GetTime();
            float deltaTime = (float)(currentTime - _lastTime);
            _lastTime = currentTime;

            var input = KeyboardState;
            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            _player.Update(input, deltaTime, _solidColliders);

            _prompt = null;
            _canInteract = false;
            foreach (var trigger in _triggerColliders)
            {
                float interactRadius = _player.Radius + 0.8f;
                if (!trigger.IntersectsSphere(_player.Position, interactRadius))
                    continue;

                _prompt = $"Press E to interact with {trigger.Name}";
                _canInteract = true;

                if (input.IsKeyPressed(Keys.E))
                {
                    switch (trigger.Name.ToLowerInvariant())
                    {
                        case "door":
                            ShowInteractMessage("Knocking...", trigger);
                            Console.WriteLine("Knocking...");
                            _audio.Play("door");
                            break;
                        case "megaphone":
                            ShowInteractMessage("*GIBBERISH*", trigger);
                            Console.WriteLine("Hello..Can you hear me?");
                            _audio.Play("megaphone");
                            break;
                        case "sofa":
                            ShowInteractMessage("Don't wanna sit", trigger);
                            Console.WriteLine("Don't wanna sit");
                            break;
                        case "television":
                            ShowInteractMessage("*STATIC*", trigger);
                            Console.WriteLine("*STATIC*");
                            _audio.Play("television");
                            break;
                        case "plant":
                        case "npc":
                            ShowInteractMessage("Water me", trigger);
                            Console.WriteLine("Water me");
                            break;
                        default:
                            ShowInteractMessage($"Interacted with {trigger.Name}", trigger);
                            Console.WriteLine($"Interacted with {trigger.Name}");
                            break;
                    }
                }
                break;
            }

            Title = _prompt != null ? $"{_baseTitle} - {_prompt}" : _baseTitle;
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (!IsFocused) return;

            if (_firstMouse)
            {
                _lastMousePos = e.Position;
                _firstMouse = false;
                return;
            }

            Vector2 delta = e.Position - _lastMousePos;
            _lastMousePos = e.Position;

            _camera.ProcessMouse(delta.X, delta.Y);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(_shaderProgram);

            var projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(_camera.Fov),
                Size.X / (float)Size.Y,
                0.1f,
                100f);

            var view = _camera.GetViewMatrix();

            int locModel = GL.GetUniformLocation(_shaderProgram, "uModel");
            int locView = GL.GetUniformLocation(_shaderProgram, "uView");
            int locProj = GL.GetUniformLocation(_shaderProgram, "uProjection");

            GL.UniformMatrix4(locView, false, ref view);
            GL.UniformMatrix4(locProj, false, ref projection);

            GL.ActiveTexture(TextureUnit.Texture0);
            int locTex = GL.GetUniformLocation(_shaderProgram, "uTexture");
            GL.Uniform1(locTex, 0);

            foreach (var obj in _objects)
            {
                GL.BindTexture(TextureTarget.Texture2D, obj.TextureId);
                Matrix4 model = obj.ModelMatrix;
                GL.UniformMatrix4(locModel, false, ref model);
                if (obj.Model != null)
                {
                    obj.Model.Draw();
                }
                else
                {
                    GL.BindVertexArray(_cubeVao);
                    GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
                }
            }

            GL.BindVertexArray(0);

            // Draw crosshair in screen space
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Matrix4 identity = Matrix4.Identity;
            GL.UniformMatrix4(locView, false, ref identity);
            GL.UniformMatrix4(locProj, false, ref identity);

            float crossScale = _canInteract ? 0.10f : 0.07f;
            Matrix4 crossModel = Matrix4.CreateScale(crossScale) * Matrix4.CreateTranslation(0f, 0f, 0f);
            GL.UniformMatrix4(locModel, false, ref crossModel);
            GL.BindTexture(TextureTarget.Texture2D, _canInteract ? _crosshairInteractTex : _crosshairDefaultTex);
            GL.BindVertexArray(_crosshairVao);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _crosshairVertCount);
            // Draw interaction message if active
            double now = GLFW.GetTime();
            if (_messageTex != 0 && now < _messageExpires)
            {
                Vector4 world = new Vector4(_messageWorldPos, 1f);
                Vector4 viewPos = Vector4.TransformRow(world, view);
                Vector4 clip = Vector4.TransformRow(viewPos, projection);
                if (clip.W > 0.0001f)
                {
                    Vector3 ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
                    float msgScale = 1f;
                    float sx = msgScale * _messageTexWidth;
                    float sy = msgScale * _messageTexHeight;
                    // Flip Y so text renders upright in screen space
                    Matrix4 msgModel = Matrix4.CreateScale(sx, -sy, 1f) * Matrix4.CreateTranslation(ndc.X, ndc.Y, 0f);
                    GL.UniformMatrix4(locModel, false, ref msgModel);
                    GL.BindTexture(TextureTarget.Texture2D, _messageTex);
                    GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _crosshairVertCount);
                }
            }
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(_cubeVao);
            GL.DeleteBuffer(_cubeVbo);
            GL.DeleteBuffer(_cubeEbo);
            GL.DeleteProgram(_shaderProgram);

            var textures = new HashSet<int>
            {
                _floorTexture,
                _wallTexture,
                _sofaTexture,
                _tableTexture,
                _tvStandTexture,
                _defaultModelTexture,
                _doorTexture,
                _ottomanTexture,
                _plantTexture,
                _megaphoneTexture,
                _televisionTexture,
                _crosshairDefaultTex,
                _crosshairInteractTex
            };
            foreach (var tex in textures)
            {
                GL.DeleteTexture(tex);
            }
            if (_messageTex != 0) GL.DeleteTexture(_messageTex);

            _npcModel?.Dispose();
            _sofaModel?.Dispose();
            _tableModel?.Dispose();
            _ottomanModel?.Dispose();
            _megaphoneModel?.Dispose();
            _televisionModel?.Dispose();
            _audio?.Dispose();

            if (_crosshairVao != 0) GL.DeleteVertexArray(_crosshairVao);
            if (_crosshairVbo != 0) GL.DeleteBuffer(_crosshairVbo);
        }
    }
}

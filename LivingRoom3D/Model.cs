using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using GL = OpenTK.Graphics.OpenGL4.GL;
using BufferTarget = OpenTK.Graphics.OpenGL4.BufferTarget;
using BufferUsageHint = OpenTK.Graphics.OpenGL4.BufferUsageHint;
using VertexAttribPointerType = OpenTK.Graphics.OpenGL4.VertexAttribPointerType;
using GLEnum = OpenTK.Graphics.OpenGL4.PrimitiveType;
using GLElementType = OpenTK.Graphics.OpenGL4.DrawElementsType;
using TextureTarget = OpenTK.Graphics.OpenGL4.TextureTarget;
using PixelInternalFormat = OpenTK.Graphics.OpenGL4.PixelInternalFormat;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using PixelType = OpenTK.Graphics.OpenGL4.PixelType;
using TextureParameterName = OpenTK.Graphics.OpenGL4.TextureParameterName;
using TextureMinFilter = OpenTK.Graphics.OpenGL4.TextureMinFilter;
using TextureMagFilter = OpenTK.Graphics.OpenGL4.TextureMagFilter;
using TextureWrapMode = OpenTK.Graphics.OpenGL4.TextureWrapMode;
using GenerateMipmapTarget = OpenTK.Graphics.OpenGL4.GenerateMipmapTarget;
using OpenTK.Mathematics;
using StbImageSharp;

namespace LivingRoom3D
{
    public sealed class Model : IDisposable
    {
        private sealed class MeshData
        {
            public int Vao;
            public int Vbo;
            public int Ebo;
            public int IndexCount;
            public int TextureId;
        }

        private readonly List<MeshData> _meshes = new();
        private readonly Dictionary<string, int> _textureCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<int> _ownedTextures = new();
        private readonly int _fallbackTexture;
        private readonly string _baseDirectory;

        private Model(int fallbackTexture, string baseDirectory)
        {
            _fallbackTexture = fallbackTexture;
            _baseDirectory = baseDirectory;
        }

        public static Model Load(string path, int fallbackTexture)
        {
            var context = new AssimpContext();
            var scene = context.ImportFile(path, PostProcessPreset.TargetRealTimeMaximumQuality);
            if (scene == null || scene.Meshes.Count == 0)
                throw new InvalidOperationException($"Model at '{path}' contains no meshes.");

            string baseDir = Path.GetDirectoryName(path) ?? string.Empty;
            var model = new Model(fallbackTexture, baseDir);
            StbImageSharp.StbImage.stbi_set_flip_vertically_on_load(1);

            foreach (var mesh in scene.Meshes)
            {
                int textureId = fallbackTexture;
                if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < scene.Materials.Count)
                {
                    var material = scene.Materials[mesh.MaterialIndex];
                    if (material.GetMaterialTexture(TextureType.Diffuse, 0, out TextureSlot slot))
                    {
                        textureId = model.LoadTextureForSlot(slot);
                    }
                }

                var vertices = new List<float>(mesh.VertexCount * 5);
                var hasTex = mesh.TextureCoordinateChannelCount > 0 && mesh.TextureCoordinateChannels[0].Count == mesh.VertexCount;
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    var pos = mesh.Vertices[i];
                    Vector3D tex = hasTex ? mesh.TextureCoordinateChannels[0][i] : new Vector3D(0, 0, 0);
                    vertices.Add(pos.X);
                    vertices.Add(pos.Y);
                    vertices.Add(pos.Z);
                    vertices.Add(tex.X);
                    vertices.Add(tex.Y);
                }

                var indices = mesh.Faces.SelectMany(f => f.Indices).Select(idx => (uint)idx).ToArray();
                var vao = GL.GenVertexArray();
                var vbo = GL.GenBuffer();
                var ebo = GL.GenBuffer();

                GL.BindVertexArray(vao);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

                int stride = 5 * sizeof(float);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

                GL.BindVertexArray(0);

                model._meshes.Add(new MeshData
                {
                    Vao = vao,
                    Vbo = vbo,
                    Ebo = ebo,
                    IndexCount = indices.Length,
                    TextureId = textureId
                });
            }

            return model;
        }

        private int LoadTextureForSlot(TextureSlot slot)
        {
            string? resolved = ResolveTexturePath(slot.FilePath);
            if (resolved == null)
                return _fallbackTexture;

            if (_textureCache.TryGetValue(resolved, out int cached))
                return cached;

            try
            {
                using var stream = File.OpenRead(resolved);
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                int tex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, tex);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                _textureCache[resolved] = tex;
                _ownedTextures.Add(tex);
                return tex;
            }
            catch
            {
                return _fallbackTexture;
            }
        }

        private string? ResolveTexturePath(string pathFromModel)
        {
            if (string.IsNullOrWhiteSpace(pathFromModel))
                return null;

            // Already absolute and exists
            if (File.Exists(pathFromModel))
                return Path.GetFullPath(pathFromModel);

            // Try relative to model directory
            string candidate = Path.GetFullPath(Path.Combine(_baseDirectory, pathFromModel));
            if (File.Exists(candidate))
                return candidate;

            // Try relative using only filename
            string fileOnly = Path.GetFileName(pathFromModel);
            if (!string.IsNullOrEmpty(fileOnly))
            {
                string fileCandidate = Path.GetFullPath(Path.Combine(_baseDirectory, fileOnly));
                if (File.Exists(fileCandidate))
                    return fileCandidate;
            }

            return null;
        }

        public void Draw()
        {
            foreach (var mesh in _meshes)
            {
                GL.BindTexture(TextureTarget.Texture2D, mesh.TextureId);
                GL.BindVertexArray(mesh.Vao);
                GL.DrawElements(GLEnum.Triangles, mesh.IndexCount, GLElementType.UnsignedInt, 0);
            }
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            foreach (var mesh in _meshes)
            {
                GL.DeleteVertexArray(mesh.Vao);
                GL.DeleteBuffer(mesh.Vbo);
                GL.DeleteBuffer(mesh.Ebo);
            }
            _meshes.Clear();

            foreach (var tex in _ownedTextures)
            {
                GL.DeleteTexture(tex);
            }
            _ownedTextures.Clear();
        }
    }
}

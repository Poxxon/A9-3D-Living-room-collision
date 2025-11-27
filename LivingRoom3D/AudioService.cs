using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LivingRoom3D
{
    public sealed class AudioService : IDisposable
    {
        private sealed class Sound
        {
            public AudioFileReader Reader = null!;
            public WaveOutEvent Output = null!;
        }

        private readonly Dictionary<string, Sound> _sounds = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public void Load(string key, string path, TimeSpan? maxDuration = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AudioService));

            if (_sounds.ContainsKey(key))
                return;

            var reader = new AudioFileReader(path);
            ISampleProvider provider = reader;
            if (maxDuration.HasValue)
            {
                provider = new OffsetSampleProvider(reader) { Take = maxDuration.Value };
            }
            var output = new WaveOutEvent();
            output.Init(provider);
            _sounds[key] = new Sound { Reader = reader, Output = output };
        }

        public void Play(string key)
        {
            if (_disposed) return;
            if (!_sounds.TryGetValue(key, out var sound))
                return;

            lock (sound)
            {
                sound.Reader.Position = 0;
                sound.Output.Stop();
                sound.Output.Play();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (var s in _sounds.Values)
            {
                s.Output.Dispose();
                s.Reader.Dispose();
            }
            _sounds.Clear();
            _disposed = true;
        }
    }
}

using Godot;
using System;

namespace Herbivore.Autoloads;

public static class SoundGenerator
{
    public static AudioStreamWav CreateTone(float frequency, float duration, float volume = 0.5f)
    {
        int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * duration);

        var samples = new byte[sampleCount * 2]; // 16-bit samples

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Simple sine wave with fade out
            float envelope = 1.0f - (float)i / sampleCount;
            float sample = (float)Math.Sin(2 * Math.PI * frequency * t) * volume * envelope;

            // Convert to 16-bit signed
            short sampleInt = (short)(sample * 32767);
            samples[i * 2] = (byte)(sampleInt & 0xFF);
            samples[i * 2 + 1] = (byte)((sampleInt >> 8) & 0xFF);
        }

        var stream = new AudioStreamWav();
        stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        stream.MixRate = sampleRate;
        stream.Stereo = false;
        stream.Data = samples;

        return stream;
    }

    public static AudioStreamWav CreateSuccessSound()
    {
        // Rising two-tone success sound
        int sampleRate = 22050;
        float duration = 0.3f;
        int sampleCount = (int)(sampleRate * duration);

        var samples = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / sampleCount;

            // Two rising notes
            float freq = progress < 0.5f ? 440f : 660f;
            float envelope = 1.0f - Math.Abs(progress - (progress < 0.5f ? 0.25f : 0.75f)) * 4;
            envelope = Math.Max(0, Math.Min(1, envelope));

            float sample = (float)Math.Sin(2 * Math.PI * freq * t) * 0.4f * envelope;

            short sampleInt = (short)(sample * 32767);
            samples[i * 2] = (byte)(sampleInt & 0xFF);
            samples[i * 2 + 1] = (byte)((sampleInt >> 8) & 0xFF);
        }

        var stream = new AudioStreamWav();
        stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        stream.MixRate = sampleRate;
        stream.Stereo = false;
        stream.Data = samples;

        return stream;
    }

    public static AudioStreamWav CreateFailSound()
    {
        // Descending buzz
        int sampleRate = 22050;
        float duration = 0.4f;
        int sampleCount = (int)(sampleRate * duration);

        var samples = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / sampleCount;

            // Descending frequency
            float freq = 300f - progress * 150f;
            float envelope = 1.0f - progress;

            // Add some buzz with square-ish wave
            float sample = (float)Math.Sin(2 * Math.PI * freq * t);
            sample = sample > 0 ? 0.3f : -0.3f; // Square wave
            sample *= envelope;

            short sampleInt = (short)(sample * 32767);
            samples[i * 2] = (byte)(sampleInt & 0xFF);
            samples[i * 2 + 1] = (byte)((sampleInt >> 8) & 0xFF);
        }

        var stream = new AudioStreamWav();
        stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        stream.MixRate = sampleRate;
        stream.Stereo = false;
        stream.Data = samples;

        return stream;
    }

    public static AudioStreamWav CreateIdentifyFoeSound()
    {
        // Short percussive sound
        int sampleRate = 22050;
        float duration = 0.15f;
        int sampleCount = (int)(sampleRate * duration);

        var samples = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / sampleCount;

            float freq = 550f;
            float envelope = (float)Math.Exp(-progress * 10); // Fast decay

            float sample = (float)Math.Sin(2 * Math.PI * freq * t) * 0.5f * envelope;

            short sampleInt = (short)(sample * 32767);
            samples[i * 2] = (byte)(sampleInt & 0xFF);
            samples[i * 2 + 1] = (byte)((sampleInt >> 8) & 0xFF);
        }

        var stream = new AudioStreamWav();
        stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        stream.MixRate = sampleRate;
        stream.Stereo = false;
        stream.Data = samples;

        return stream;
    }
}

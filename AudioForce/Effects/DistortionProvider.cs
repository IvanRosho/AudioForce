using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioForce.Effects
{
    public class DistortionProvider : WaveProvider32
    {
        WaveProvider32 input;

        public float Drive;
        public float Gain;
        public float Tone;

        public DistortionProvider(float drive, float tone, float gain, WaveProvider32 input)
        {
            this.input = input;
            this.Drive = drive;
            this.Tone = tone;
            this.Gain = gain;
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int c = input.Read(buffer, offset, sampleCount);
            // Находим текущее максимальное по модулю значение, для дальнейшей нормализации вычислений
            float absmax = 0;
            for (int i = 0; i < sampleCount; ++i) if (absmax < Math.Abs(buffer[i])) absmax = Math.Abs(buffer[i]);
            // Нормализируем параметр Tone
            float normtone = absmax * Tone;
            for (int i = 0; i < sampleCount; ++i)
            {
                // Эффект дисторшн обладает очень простой амплитудной характеристикой
                // K[A] = clamp[A, -t, t]
                // где t - параметр Tone
                float val = buffer[i] * Drive;
                if (Math.Abs(val) > normtone) val = Math.Sign(val) * normtone;
                buffer[i] = val * Gain;
            }
            return c;
        }
    }
}
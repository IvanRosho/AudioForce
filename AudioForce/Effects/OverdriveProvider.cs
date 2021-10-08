using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioForce.Effects
{
    public class OverdriveProvider : WaveProvider32
    {
        WaveProvider32 input;

        public float Drive;
        public float Tone;
        public float Gain;

        public OverdriveProvider(float drive, float tone, float gain, WaveProvider32 input)
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
            float absmax = 0f;
            for (int i = 0; i < sampleCount; ++i) if (absmax < Math.Abs(buffer[i])) absmax = Math.Abs(buffer[i]);
            // Для избежания неопределенностей в вычислениях, они не будут проводиться если absmax == 0
            if (absmax < float.Epsilon) return c;

            for (int i = 0; i < sampleCount; ++i)
            {
                // Для эффекта овердрай можно выбрать множество различных нелинейных амплитуднах характеристик,
                // все они будут давать разное насыщение сигналу. В данном случае была выбрана следующая характеристика:
                // K[A] = (exp(t * A) - exp(-t * A)) / (exp(t * A) + exp(-t * A))
                // где t - параметр Tone
                float val = buffer[i] * Drive;
                float nv = Tone * val / absmax;
                float od = absmax * (float)((Math.Exp(nv) - Math.Exp(-nv)) / (Math.Exp(nv) + Math.Exp(-nv)));
                buffer[i] = od * Gain;
            }
            return c;
        }
    }
}
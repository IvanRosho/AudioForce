using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioForce.Effects
{
    public class ReverbProvider : WaveProvider32
    {
        WaveProvider32 input;
        public float Wet;

        public ReverbProvider(float wet, WaveProvider32 input)
        {
            this.input = input;
            this.Wet = wet;
        }

        // Вспомогательна ф-ция для циклического доступа к элементам массива
        float circ(float[] arr, int i)
        {
            return arr[i < 0 ? arr.Length - 1 + i : i];
        }

        // Вспомогательные буфера для AP и FBCF фильтров
        float[] ap1, ap2, ap3;
        float[] fb1, fb2, fb3, fb4, fbsum;

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int c = input.Read(buffer, offset, sampleCount);

            // Создаем вспомогательные буфера, если они еще не созданы
            if (ap1 == null)
            {
                ap1 = new float[sampleCount];
                ap2 = new float[sampleCount];
                ap3 = new float[sampleCount];

                fb1 = new float[sampleCount];
                fb2 = new float[sampleCount];
                fb3 = new float[sampleCount];
                fb4 = new float[sampleCount];

                fbsum = new float[sampleCount];
            }

            // Реализация реверберации по Шредеру SATREV проф. John Chowning для CCRMA
            // Диаграма фильтра реверба:
            //      
            //           |---> FBCF[0.805,  901] --\
            //           |                          \
            //           |---> FBCF[0.827,  778] -\  \
            // RevIn --->|                          +  --> AP[0.7, 125] --> AP[0.7, 42] --> AP[0.7, 12] --> RevOut
            //           |---> FBCF[0.783, 1011] -/  /
            //           |                          /
            //           |---> FBCF[0.764, 1123] --/
            // Где:
            //   AP - Allpass filter
            //   AP[g, N] = (-g + z^(-N)) / (1 - g * z^(-N))
            //
            //   FBCF - Feedback comb filter
            //   FBCF[g, N] = 1 / (1 - g * z^(-N))
            float g = 0.7f;
            for (int i = 0; i < sampleCount; ++i)
            {
                // Параллельно соеденены FBCF фильтры
                fb1[i] = buffer[i] + 0.805f * circ(fb1, i - 901);
                fb2[i] = buffer[i] + 0.827f * circ(fb2, i - 778);
                fb3[i] = buffer[i] + 0.783f * circ(fb3, i - 1011);
                fb4[i] = buffer[i] + 0.764f * circ(fb4, i - 1123);

                // Сумма выходов FBCF фильров
                fbsum[i] = fb1[i] + fb2[i] + fb3[i] + fb4[i];

                //  Последовательное соеденение AP фильтров
                ap1[i] = -g * fbsum[i] + circ(fbsum, i - 125) + g * circ(ap1, i - 125);
                ap2[i] = -g * ap1[i] + circ(ap1, i - 42) + g * circ(ap2, i - 42);
                ap3[i] = -g * ap2[i] + circ(ap2, i - 12) + g * circ(ap3, i - 12);

                // Свешиваем чистый сигнал и реверберированый
                buffer[i] = Wet * ap3[i] + (1f - Wet) * buffer[i];
            }
            return c;
        }
    }
}
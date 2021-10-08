using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AudioForce.Effects
{
    // Класс обертки над BufferedWaveProvider
    // для перевода семплов из типа byte[] во float[]
    // для удобной дальнейшей обработки сигнала
    public class WrapperProvider : WaveProvider32
    {
        IWaveProvider input;
        Pcm32BitToSampleProvider pcm32;

        public WrapperProvider(IWaveProvider input)
        {
            this.input = input;
            pcm32 = new Pcm32BitToSampleProvider(this.input);
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            return pcm32.Read(buffer, offset, sampleCount);
        }
    }
}
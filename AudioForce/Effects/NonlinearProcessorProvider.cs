using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioForce.Effects
{
    public class NonlinearProcessorProvider : WaveProvider32
    {
        public bool IsOverdriveEnabled;
        OverdriveProvider overdrive;
        DistortionProvider distortion;

        public NonlinearProcessorProvider(OverdriveProvider overdrive, DistortionProvider distortion, bool isOverdriveEnabled = false)
        {
            this.overdrive = overdrive;
            this.distortion = distortion;
            this.IsOverdriveEnabled = isOverdriveEnabled;
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            // В зависимости от значния поля IsOverdriveEnabled выбирается куда дальше пропускать сигнал
            if (IsOverdriveEnabled) return overdrive.Read(buffer, offset, sampleCount);
            else return distortion.Read(buffer, offset, sampleCount);
        }
    }
}
using System;
using System.Linq;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Dsp;
using AudioForce.Effects;

namespace AudioForce
{
    public partial class MainForm : Form
    {
        // Частота дискретизвции
        int sampleRate;

        // Размер буфера в милисекундах
        // Чем больше размер буффера, тем меньше лагов(шумы, потрескивания, и т.д.) в звуке
        // но, тем больше времени потребуется для обработки сигнала, т.е. больше лагов
        // в работе программы
        int bufferSize = 80;

        WaveIn waveIn;
        DirectSoundOut directOut;

        BufferedWaveProvider bufferedWave;
        WrapperProvider wrapper;
        OverdriveProvider overdrive;
        DistortionProvider distortion;
        NonlinearProcessorProvider nonlin;
        EqualizerProvider equalizer;
        ReverbProvider reverb;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Initialize()
        {
            sampleRate = 44100;

            // Для устройства записи указываем размер буфера bufferSize мс
            // для того чтобы избежать больших задержек.
            // В качеств формата записи указываем одноканальный формат, 32 бит / 44.1 кГц
            waveIn = new WaveIn(WaveCallbackInfo.ExistingWindow(this.Handle));
            waveIn.DeviceNumber = 0;
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.BufferMilliseconds = bufferSize;
            waveIn.WaveFormat = new WaveFormat(sampleRate, 32, 1);

            // Врапер для записаного сигнала
            bufferedWave = new BufferedWaveProvider(waveIn.WaveFormat);
            bufferedWave.DiscardOnBufferOverflow = true;
            
            // Врапер для перевода типа буфера из byte[] во float[]
            wrapper = new WrapperProvider(bufferedWave);

            // Инициализируем цепь эффектов
            overdrive = new OverdriveProvider(1, 0.1f, 1, wrapper);
            distortion = new DistortionProvider(1, 1, 1, wrapper);
            nonlin = new NonlinearProcessorProvider(overdrive, distortion);
            equalizer = new EqualizerProvider(null, nonlin);
            reverb = new ReverbProvider(0, equalizer);

            // Cоздаем и инициализируем устройство вывода.
            // В даном случае будет ипользовано "Primary audio device"
            directOut = new DirectSoundOut(bufferSize);
            directOut.Init(reverb);
            directOut.Play();

            waveIn.StartRecording();
        }

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            bufferedWave.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Initialize();
        }

        private void MainForm_Closing(object sender, FormClosingEventArgs e)
        {
            // Если можем, уничтожаем устройства записи-воспроизведения
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }
            if (directOut != null)
            {
                directOut.Stop();
                directOut.Dispose();
                directOut = null;
            }
        }

        private void equlizer_Scroll(object sender, EventArgs e)
        {
            TrackBar current = sender as TrackBar;

            // Находим TextBox соответствующий данному TrackBar-y
            var tb = equalizerGroupBox.Controls.OfType<TextBox>().FirstOrDefault(t => t.TabIndex == current.TabIndex);
            tb.Text = string.Format("{0} dB", current.Value);

            CreateEqualizerFilter();
        }

        private void CreateEqualizerFilter()
        {
            EqualizerParameters eqparams = new EqualizerParameters();
            // Значение добротности было найдено исходя из формулы:
            //          Q = fc / Df
            // где fc - центральная частота послосы эквалайзера
            //     Df = f2 - f1
            //     f2, f1 находятся из условия normH(f) == -3 dB
            // Так как в данной программе эквалайзер октавный, то
            // не трудно вывести, что для найбольшей гладкости H(f)
            // значине Q = 4/3
            float Q = 4f / 3f;
            foreach (var trackBar in equalizerGroupBox.Controls.OfType<TrackBar>())
                eqparams.Add(trackBar.Value, Q, float.Parse(trackBar.Tag.ToString()));

            equalizer.Bands = EqualizerProvider.ParametricEqualizer(eqparams, sampleRate);
        }

        private void distortionDriveTrackBar_Scroll(object sender, EventArgs e)
        {
            // Переводим дБ в безразмерные величины по формуле
            // A = 10^(Adb / 20)
            distortion.Drive = (float)Math.Pow(10, distortionDriveTrackBar.Value / 20f);
            overdrive.Drive = (float)Math.Pow(10, distortionDriveTrackBar.Value / 20f);
        }

        private void distortionGainTrackBar_Scroll(object sender, EventArgs e)
        {
            // Аналогично distortionDriveTrackBar_Scroll
            distortion.Gain = (float)Math.Pow(10, distortionGainTrackBar.Value / 20f);
            overdrive.Gain = (float)Math.Pow(10, distortionGainTrackBar.Value / 20f);
        }

        private void distortionToneTrackBar_Scroll(object sender, EventArgs e)
        {
            // Для эффекта дисторшн параметр Tone принимает значния ~[0, 0.9]
            // если верхнюю границу установить в 1, то при таком значении параметра Tone
            // на выходе эффекта будет 0, т.к. |s| >= 1 - Tone => |s| >= 0
            // где s - сигнал на входе эффекта
            distortion.Tone = 1.1f - distortionToneTrackBar.Value / 100f;

            // А для эффекта овердрайв - ~[0.1, 5]
            overdrive.Tone = (float)0.1 + distortionToneTrackBar.Value / 20f;
        }

        private void overdriveRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            nonlin.IsOverdriveEnabled = overdriveRadioButton.Checked;
        }

        private void reverbLevelTrackBar_Scroll(object sender, EventArgs e)
        {
            reverb.Wet = reverbLevelTrackBar.Value / 100f;
        }
    }
}

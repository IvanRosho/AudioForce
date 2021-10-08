using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Dsp;
using NAudio.Wave;

namespace AudioForce.Effects
{
    public class EqualizerProvider : WaveProvider32
    {
        WaveProvider32 input;

        public BiQuadFilter[] Bands;

        public EqualizerProvider(BiQuadFilter[] bands, WaveProvider32 input)
        {
            this.input = input;
            this.Bands = bands ?? new BiQuadFilter[0];
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int c = input.Read(buffer, offset, sampleCount);
            for (int i = 0; i < sampleCount; ++i)
            {
                // Есть два метода работы с параметрическим эквайлазером:
                // 1) (реализовано) Поочередно сигнал пропускать через филтьтр каждой полосы эквалайзера.
                // 2) Свернуть коэфициенты все фильтров в в один филтьтр и сигнал пропускать только через результатирующий фильтр.
                // Далее реализван способ 1), т.к. он проще в реализации.
                float sample = buffer[i];
                for (int j = 0; j < Bands.Length; ++j)
                    sample = Bands[j].Transform(sample);
                buffer[i] = sample;
            }
            return c;
        }

        /// <summary>
        /// По заданым параметрам полос эквалайзера создает массив фильтров
        /// </summary>
        /// <param name="parameters">Упорядоченая коллекция параметров эквалайзера</param>
        /// <param name="sampleRate">Частота дискретизации</param>
        /// <returns>Массив фильтров полос эквалайзера</returns>
        public static BiQuadFilter[] ParametricEqualizer(EqualizerParameters parameters, float sampleRate)
        {
            if (parameters.Count < 2) throw new ArgumentException("Equlizer must have at least two bands.");

            // Фильтр полосы с самой низкой частотой среза всегда имеет тип LowShelf
            var ls = parameters[0];
            BiQuadFilter[] res = new BiQuadFilter[parameters.Count];
            res[0] = BiQuadFilter.LowShelf(sampleRate, ls.Frequency, ls.Q, ls.Gain);

            // Для промежуточных полос выбирается тип фильтр Peak(рус. Колокол)
            for (int i = 1; i < parameters.Count - 1; ++i)
                res[i] = BiQuadFilter.PeakingEQ(sampleRate, parameters[i].Frequency, parameters[i].Q, parameters[i].Gain);

            // Для фильтра полосы с самой большой частотой среза выбирается тип HighShelf
            var hs = parameters[parameters.Count - 1];
            res[res.Length - 1] = BiQuadFilter.HighShelf(sampleRate, hs.Frequency, hs.Q, hs.Gain);

            return res;
        }
    }

    /// <summary>
    /// Упорядоченая коллекция параметров полос эквалайзера
    /// </summary>
    public class EqualizerParameters
    {
        /// <summary>
        /// Параметры одной полосы эквалайзера
        /// </summary>
        public class EqulizerBand
        {
            /// <summary>
            /// Добротность
            /// </summary>
            public float Q { get; set; }

            /// <summary>
            /// Центральная частот/Частота среза
            /// </summary>
            public float Frequency { get; set; }

            /// <summary>
            /// Усиление в полосе пропускания
            /// </summary>
            public float Gain { get; set; }

            /// <summary>
            /// Создает новый объект с параметрами
            /// </summary>
            /// <param name="gain">Усиление в полосе пропускания</param>
            /// <param name="Q">Добротность</param>
            /// <param name="frequency">Центральная частота/Частота среза</param>
            public EqulizerBand(float gain, float Q, float frequency)
            {
                this.Gain = gain;
                this.Q = Q;
                this.Frequency = frequency;
            }
        }

        private List<EqulizerBand> bands;

        public List<EqulizerBand> Bands { get { return bands; } }

        public EqulizerBand this[int index]
        {
            get
            {
                return bands[index];
            }
        }

        /// <summary>
        /// Количество полос эквалайзера
        /// </summary>
        public int Count { get { return bands.Count; } }

        /// <summary>
        /// Создает новую пустую коллекцию параметров
        /// </summary>
        public EqualizerParameters()
        {
            this.bands = new List<EqulizerBand>();
        }

        /// <summary>
        /// Добавляет новый объект параметров в коллекцию
        /// </summary>
        /// <param name="band"> <seealso cref="EqulizerBand"/> </param>
        public void Add(EqulizerBand band)
        {
            if (this.bands.Count == 0) this.bands.Add(band);
            else
            {
                for (int i = 0; i < this.bands.Count; ++i)
                    if (this.bands[i].Frequency > band.Frequency)
                    {
                        this.bands.Insert(i, band);
                        return;
                    }
                this.bands.Add(band);
            }
        }

        /// <summary>
        /// Добавляет новый объект параметров в коллекцию
        /// </summary>
        /// <param name="gain">Усиление в полосе пропускания</param>
        /// <param name="Q">Добротность</param>
        /// <param name="frequency">Центральная частота/Частота среза</param>
        public void Add(float gain, float Q, float frequency)
        {
            this.Add(new EqulizerBand(gain, Q, frequency));
        }
    }

}

using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Dsp;

namespace AudioEffectsExample
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputFilePath = "input.wav";
            string outputFilePath = "output.wav";

            Console.WriteLine("Digite os efeitos desejados:");
            Console.WriteLine("r - Reverb");
            Console.WriteLine("d - Distortion");
            Console.WriteLine("b - Aumento de graves");
            Console.WriteLine("o - Overdrive");
            Console.WriteLine("f - Efeito de distância");
            Console.WriteLine("a - Aplicar todos os efeitos");
            Console.WriteLine("Digite a combinação de letras correspondente aos efeitos desejados:");
            string effects = Console.ReadLine().ToLower();

            using (var reader = new AudioFileReader(inputFilePath))
            {
                ISampleProvider processedAudio = reader;

                if (effects.Contains('a'))
                {
                    effects = "rdbof";
                }

                if (effects.Contains('r'))
                {
                    processedAudio = new SimpleReverbProvider(processedAudio);
                }

                if (effects.Contains('d'))
                {
                    processedAudio = new SimpleDistortionProvider(processedAudio);
                }

                if (effects.Contains('b'))
                {
                    processedAudio = new BassBoostProvider(processedAudio);
                }

                if (effects.Contains('o'))
                {
                    processedAudio = new OverdriveProvider(processedAudio);
                }

                if (effects.Contains('f'))
                {
                    processedAudio = new DistantEffectProvider(processedAudio);
                }

                // Salve o arquivo de áudio processado
                WaveFileWriter.CreateWaveFile16(outputFilePath, processedAudio);
            }

            Console.WriteLine("Arquivo processado salvo em: " + outputFilePath);
        }
    }

    public class SimpleReverbProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly float delay;
        private float[] delayBuffer;
        private int delayIndex;

        public SimpleReverbProvider(ISampleProvider sourceProvider, float delay = 0.1f)
        {
            this.sourceProvider = sourceProvider;
            this.delay = delay;
            delayBuffer = new float[(int)(delay * sourceProvider.WaveFormat.SampleRate)];
            delayIndex = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                // Reverb
                float delayedSample = delayBuffer[delayIndex];
                buffer[offset + i] += delayedSample * 0.5f; // 50% de mixagem do reverb

                delayBuffer[delayIndex] = buffer[offset + i];
                delayIndex++;
                if (delayIndex >= delayBuffer.Length)
                    delayIndex = 0;
            }

            return samplesRead;
        }

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;
    }

    public class SimpleDistortionProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly float gain;
        private readonly float threshold;

        public SimpleDistortionProvider(ISampleProvider sourceProvider, float gain = 3f, float threshold = 0.6f)
        {
            this.sourceProvider = sourceProvider;
            this.gain = gain;
            this.threshold = threshold;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                // Distortion
                float inputSample = buffer[offset + i] * gain;

                if (inputSample > threshold)
                    inputSample = threshold;
                else if (inputSample < -threshold)
                    inputSample = -threshold;

                buffer[offset + i] = inputSample;
            }

            return samplesRead;
        }

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;
    }

    public class BassBoostProvider : ISampleProvider
    {
        private readonly BiQuadFilter lowShelfFilter;
        private readonly ISampleProvider sourceProvider;

        public BassBoostProvider(ISampleProvider sourceProvider, float gain = 6f, float cutoffFrequency = 200f)
        {
            this.sourceProvider = sourceProvider;
            lowShelfFilter = BiQuadFilter.LowShelf(sourceProvider.WaveFormat.SampleRate, cutoffFrequency, 1, gain);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                buffer[offset + i] = lowShelfFilter.Transform(buffer[offset + i]);
            }

            return samplesRead;
        }

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;
    }

    public class OverdriveProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly float gain;
        private readonly float threshold;

        public OverdriveProvider(ISampleProvider sourceProvider, float gain = 3f, float threshold = 0.3f)
        {
            this.sourceProvider = sourceProvider;
            this.gain = gain;
            this.threshold = threshold;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                float inputSample = buffer[offset + i] * gain;
                if (inputSample > threshold)
                {
                    inputSample = 1.0f - (float)Math.Exp(-inputSample);
                }
                else if (inputSample < -threshold)
                {
                    inputSample = -(1.0f - (float)Math.Exp(inputSample));
                }

                buffer[offset + i] = inputSample;
            }

            return samplesRead;
        }

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;
    }

    public class DistantEffectProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly float delay;

        private float[] delayBuffer;
        private int delayIndex;

        public DistantEffectProvider(ISampleProvider sourceProvider, float delay = 0.5f)
        {
            this.sourceProvider = sourceProvider;
            this.delay = delay;
            delayBuffer = new float[(int)(delay * sourceProvider.WaveFormat.SampleRate)];
            delayIndex = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                float delayedSample = delayBuffer[delayIndex];

                buffer[offset + i] += delayedSample * 0.5f;

                delayBuffer[delayIndex] = buffer[offset + i] * 0.5f;
                delayIndex++;
                if (delayIndex >= delayBuffer.Length)
                    delayIndex = 0;
            }

            return samplesRead;
        }

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;
    }

}

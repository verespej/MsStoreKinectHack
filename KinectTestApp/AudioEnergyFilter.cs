using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeadAnimation
{
    public delegate void AudioEnergyFilterEventHandler(double time, float normalizedEnergy);

    public class AudioEnergyFilter
    {
        public AudioEnergyFilter(int audioSampleRate, int channelCount, int energySamplesPerSecond = 60, double initialTime = 0) {
            m_SampleRate = audioSampleRate;
            m_ChannelCount = channelCount;
            m_EnergySamplesPerSecond = energySamplesPerSecond;
            m_Time = initialTime;
            m_AccumulatedSquareSum = 0;
            m_AccumulatedSampleCount = m_AudioSamplesPerEnergySample = 0;

            m_AudioSamplesPerEnergySample = (m_SampleRate * m_ChannelCount) / m_EnergySamplesPerSecond;
    }
        
		public void ProcessAudio(byte[] data, int sampleSize) {
            for (int i = 0; i < sampleSize; i += 2)
            {
                // compute the sum of squares of audio samples that will get accumulated
                // into a single energy value.
                short audioSample = BitConverter.ToInt16(data, i);

                //        double normalizedSample = (double) audioSample / (double) SHRT_MAX;
                m_AccumulatedSquareSum += audioSample * audioSample;
                ++m_AccumulatedSampleCount;

                if (m_AccumulatedSampleCount >= m_AudioSamplesPerEnergySample)
                {
                    // Each energy value will represent the logarithm of the mean of the
                    // sum of squares of a group of audio samples.
                    const double MAX_SQUARED = short.MaxValue * short.MaxValue;
                    double normalizedMeanSquare = m_AccumulatedSquareSum / MAX_SQUARED / m_AudioSamplesPerEnergySample;

                    double energy = Math.Sqrt(normalizedMeanSquare);
                    if (energy > 1.0)
                    {
                        energy = 1.0;
                    }

                    if (OnAudioEnergyAvailable != null)
                    {
                        OnAudioEnergyAvailable(m_Time, (float) energy);
                    }

                    m_AccumulatedSquareSum = 0;
                    m_AccumulatedSampleCount = 0;

                    m_Time += (1.0 / (double)m_EnergySamplesPerSecond);
                }
            }
        }

        public event AudioEnergyFilterEventHandler OnAudioEnergyAvailable;

        private int m_SampleRate;
        private int m_ChannelCount;
        private int m_EnergySamplesPerSecond;
        private double m_Time;

        private double m_AccumulatedSquareSum;
        private int m_AccumulatedSampleCount;
        private int m_AudioSamplesPerEnergySample;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeadAnimation
{
    public delegate void LipSyncMouthChangedEvent(float level);

    public class LipSync
    {
        private const double MIN_LIP_SYNC_ENERGY = 0.15;

        public LipSync(double timeWindow, double dt)
        {
            m_TimeWindow = timeWindow;
            m_Dt = dt;
        }

        public void ProcessEnergy(float energyLevel)
        {
            m_Time += m_Dt;

            if (!m_Calibrated)
            {
                m_RecentMax = m_CurrentMax = Math.Max(MIN_LIP_SYNC_ENERGY, energyLevel);
                for (int i = 0; i < m_MaximumEnergy.Length; i++)
                {
                    m_MaximumEnergy[i] = m_CurrentMax;
                }

                m_Calibrated = true;
            }
            
            if (m_Time > m_TimeWindow)
            {
                m_MaximumEnergy[m_CurrentEnergyIndex] = m_CurrentMax;
                m_CurrentEnergyIndex = (m_CurrentEnergyIndex + 1) % m_MaximumEnergy.Length;

                m_RecentMax = m_MaximumEnergy[0];
                for (int i = 1; i < m_MaximumEnergy.Length; i++)
                {
                    m_RecentMax = Math.Max(m_MaximumEnergy[i], m_RecentMax);
                }

                m_RecentMax = Math.Max(MIN_LIP_SYNC_ENERGY, m_RecentMax);

                m_Time = 0;
                m_CurrentMax = Math.Max(MIN_LIP_SYNC_ENERGY, energyLevel);
            }

            m_CurrentMax = Math.Max(m_CurrentMax, energyLevel);

            if (m_RecentMax > 0) {
                if (OnMouthChanged != null)
                {
                    double max = Math.Max(m_CurrentMax, m_RecentMax);
                    double normalizedEnergy = energyLevel / max;

                    const double NOISE_FLOOR = 0.1;
                    normalizedEnergy = Math.Max(normalizedEnergy - NOISE_FLOOR, 0.0) * (1.0 - NOISE_FLOOR);
                    normalizedEnergy = Math.Min(1.0, normalizedEnergy / 0.5);

                    OnMouthChanged((float) normalizedEnergy);
                }
            }
        }

        public event LipSyncMouthChangedEvent OnMouthChanged;

        private bool m_Calibrated = false;
        private int m_CurrentEnergyIndex = 0;
        private double m_CurrentMax = 0;
        private double[] m_MaximumEnergy = new double[3];
        private double m_RecentMax = 0;

        private double m_TimeWindow;
        private double m_Dt;
        private double m_Time;
    }
}

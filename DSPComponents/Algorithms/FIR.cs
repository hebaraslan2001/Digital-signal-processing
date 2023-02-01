using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSPAlgorithms.DataStructures;
using System.IO;

namespace DSPAlgorithms.Algorithms
{
    public class FIR : Algorithm
    {
        public Signal InputTimeDomainSignal { get; set; }
        public FILTER_TYPES InputFilterType { get; set; }
        public float InputFS { get; set; } // Sampling freq
        public float? InputCutOffFrequency { get; set; } //Fc
        public float? InputF1 { get; set; } // Band/Stop pass
        public float? InputF2 { get; set; } // Band/Stop pass
        public float InputStopBandAttenuation { get; set; } // Stop_Band_Attenuation
        public float InputTransitionBand { get; set; } // transition_width
        public Signal OutputHn { get; set; } // w(d) * h(d)
        public Signal OutputYn { get; set; } // Convolution (Hn , Input)

        public void save_coefficients()
        {
            StreamWriter streamSaver = new StreamWriter("../../../__coefficients.txt");
            streamSaver.WriteLine(0);

            if (OutputHn.Periodic)
                streamSaver.WriteLine(1);
            else
                streamSaver.WriteLine(0);

            streamSaver.WriteLine(OutputHn.SamplesIndices.Count);

            for (int i = 0; i < OutputHn.SamplesIndices.Count; ++i)
            {
                streamSaver.Write(OutputHn.SamplesIndices[i]);
                if (i == OutputHn.SamplesIndices.Count - 1)
                    streamSaver.Write(" " + OutputHn.Samples[i]);
                else
                    streamSaver.WriteLine(" " + OutputHn.Samples[i]);
            }
            streamSaver.Flush();
            streamSaver.Close();
        }

        public override void Run()
        {
            //throw new NotImplementedException();
            InputTransitionBand /= 1000;
            InputCutOffFrequency /= 1000;
            InputFS /= 1000;

            // filters
            int N = calculate_N();////1
            List<double> window = get_window(N);////2
            List<double> impulse_response = new List<double>();
            if (InputFilterType == FILTER_TYPES.LOW)////3
            {
                impulse_response = get_impulse_response_low_filter(N);
            }
            else if (InputFilterType == FILTER_TYPES.HIGH)
            {
                impulse_response = get_impulse_response_high_filter(N);
            }
            else if (InputFilterType == FILTER_TYPES.BAND_PASS)
            {
                impulse_response = bandpass(N);
            }
            else if (InputFilterType == FILTER_TYPES.BAND_STOP)
            {
                impulse_response = bandstop(N);
            }

            OutputHn = new Signal(new List<float>(), new List<int>(), InputTimeDomainSignal.Periodic);///4

            for (int i = 0; i < impulse_response.Count; ++i)
            {
                OutputHn.SamplesIndices.Add((impulse_response.Count - i - 1) * -1);
                OutputHn.Samples.Add((float)(impulse_response[impulse_response.Count - 1 - i] * window[impulse_response.Count - 1 - i]));
            }

            for (int i = 1; i < impulse_response.Count; ++i)
            {
                OutputHn.SamplesIndices.Add(i);
                OutputHn.Samples.Add((float)(impulse_response[i] * window[i]));
            }
            //convolution
            DirectConvolution convolution = new DirectConvolution();
            convolution.InputSignal1 = InputTimeDomainSignal;
            convolution.InputSignal2 = OutputHn;
            convolution.Run();
            OutputYn = convolution.OutputConvolvedSignal;/////5
            save_coefficients();
        }

        // calculate filters 
        public List<double> get_impulse_response_low_filter(int N)
        {
            int count = (N - 1) / 2;
            List<double> impulse_response = new List<double>();
            float fc_dash = calculate_fc_dash();
            float w = (float)(2 * Math.PI * fc_dash);
            for (int i = 0; i <= count; ++i)
            {
                if (i == 0)
                    impulse_response.Add(2 * fc_dash);
                else
                    impulse_response.Add(2 * fc_dash * (Math.Sin(i * w) / (i * w)));
            }
            return impulse_response;
        }

        public List<double> get_impulse_response_high_filter(int N)
        {
            int count = (N - 1) / 2;
            List<double> impulse_response = new List<double>();
            float fc_dash = calculate_fc_dashs();
            float w = (float)(2 * Math.PI * fc_dash);

            for (int i = 0; i <= count; ++i)
            {
                if (i == 0)
                    impulse_response.Add(1 - (2 * fc_dash));
                else
                    impulse_response.Add(-2 * fc_dash * (Math.Sin(i * w) / (i * w)));
            }
            return impulse_response;
        }
        public List<double> bandpass(int N)
        {
            List<double> impulse_response = new List<double>();
            int count = (N - 1) / 2;
            float f1 = clcbandpass_f1();
            float w1 = (float)(2 * Math.PI * f1);
            float f2 = clcbandpass_f2();
            float w2 = (float)(2 * Math.PI * f2);

            for (int i = 0; i <= count; ++i)
            {
                if (i == 0)
                    impulse_response.Add(2 * (f2 - f1));
                else
                    impulse_response.Add((2 * f2 * ((Math.Sin(i * w2)) / (i * w2))) - (2 * f1 * ((Math.Sin(i * w1)) / (i * w1))));
            }
            return impulse_response;
        }
        public List<double> bandstop(int N)
        {
            int count = (N - 1) / 2;
            float f1 = clcbandstop_f1();
            float w1 = (float)(2 * Math.PI * f1);
            float f2 = clcbandstop_f2();
            float w2 = (float)(2 * Math.PI * f2);

            List<double> impulse_response = new List<double>();

            for (int i = 0; i <= count; ++i)
            {
                if (i == 0)
                    impulse_response.Add(1 - (2 * (f2 - f1)));
                else
                    impulse_response.Add((2 * f1 * (Math.Sin(i * w1) / (i * w1))) - (2 * f2 * (Math.Sin(i * w2) / (i * w2))));
            }
            return impulse_response;
        }
        public int calculate_N()///depend on InputStopBandAttenuation
        {
            ///normalize InputTransitionBand => divide by InputFS
            int N = -1;
            if (InputStopBandAttenuation <= 21)
                N = (int)Math.Ceiling((0.9 / (InputTransitionBand / InputFS)));
            else if (InputStopBandAttenuation <= 44)
                N = (int)Math.Ceiling((3.1 / (InputTransitionBand / InputFS)));
            else if (InputStopBandAttenuation <= 53)
                N = (int)Math.Ceiling((3.3 / (InputTransitionBand / InputFS)));
            else if (InputStopBandAttenuation <= 74)
                N = (int)Math.Ceiling((5.5 / (InputTransitionBand / InputFS)));
            //N should be odd (type one)
            if (N % 2 == 0) N++;

            return N;
        }
        public float calculate_fc_dash()
        {
            float fc_dash = (float)(InputCutOffFrequency + ((InputTransitionBand) / 2));
            return fc_dash / InputFS;
        }
        public float calculate_fc_dashs()
        {
            float fc_dash = (float)(InputCutOffFrequency - ((InputTransitionBand) / 2));
            return fc_dash / InputFS;
        }
        public float clcbandpass_f1()
        {
            float f1 = (float)(InputF1 - ((InputTransitionBand * 1000) / 2));
            return f1 / (InputFS * 1000);
        }
        public float clcbandpass_f2()
        {
            float f2 = (float)(InputF2 + ((InputTransitionBand * 1000) / 2));
            return f2 / (InputFS * 1000);
        }
        public float clcbandstop_f1()
        {
            float f1 = (float)(InputF1 + ((InputTransitionBand * 1000) / 2));
            return f1 / (InputFS * 1000);
        }
        public float clcbandstop_f2()
        {
            float f2 = (float)(InputF2 - ((InputTransitionBand * 1000) / 2));
            return f2 / (InputFS * 1000);
        }
        public List<double> get_window(int N)
        {
            int count = (N - 1) / 2;
            List<double> window = new List<double>();
            if (InputStopBandAttenuation <= 21)///rectangle
            {
                for (int i = 0; i <= count; ++i)
                    window.Add(1);
            }
            else if (InputStopBandAttenuation <= 44)////hanning
            {
                for (int i = 0; i <= count; ++i)
                    window.Add(0.5 + (0.5 * Math.Cos((2 * Math.PI * i) / (double)(N))));
            }
            else if (InputStopBandAttenuation <= 53)///hamming
            {
                for (int i = 0; i <= count; ++i)
                    window.Add(0.54 + (0.46 * Math.Cos((2 * Math.PI * i) / (double)(N))));
            }
            else if (InputStopBandAttenuation <= 74)///blackman
            {
                for (int i = 0; i <= count; ++i)
                    window.Add(0.42 + (0.5 * Math.Cos((2 * Math.PI * i) / (double)(N - 1))) + (0.08 * Math.Cos((4 * Math.PI * i) / (double)(N - 1))));
            }
            return window;
        }
    }
}
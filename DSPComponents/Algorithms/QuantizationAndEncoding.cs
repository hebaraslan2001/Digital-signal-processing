using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSPAlgorithms.DataStructures;

namespace DSPAlgorithms.Algorithms
{
    public class QuantizationAndEncoding : Algorithm
    {
        // You will have only one of (InputLevel or InputNumBits), the other property will take a negative value
        // If InputNumBits is given, you need to calculate and set InputLevel value and vice versa
        public int InputLevel { get; set; }
        public int InputNumBits { get; set; }
        public Signal InputSignal { get; set; }
        public Signal OutputQuantizedSignal { get; set; }
        public List<int> OutputIntervalIndices { get; set; }
        public List<string> OutputEncodedSignal { get; set; }
        public List<float> OutputSamplesError { get; set; }
        public List<float> levelsMidPoint { get; set; }
        public List<float> levelsEndPoints { get; set; }

        public List<float> quantizedSignal { get; set; }


        public override void Run()
        {
            levelsMidPoint = new List<float>();
            levelsEndPoints = new List<float>();
            OutputSamplesError = new List<float>();
            OutputIntervalIndices = new List<int>();
            OutputEncodedSignal = new List<string>();
            quantizedSignal = new List<float>();

            
            float rangeOfLevel;// delta
            float maximum = 0.0f;
            float minimum = 10000.0f;


            maximum = InputSignal.Samples.Max();
            minimum = InputSignal.Samples.Min();


            if (InputLevel == 0)
            {
                InputLevel = (int)Math.Pow(2, InputNumBits);
            }
            else
            {
                InputNumBits = (int)Math.Log(InputLevel, 2);
            }

            //delta
            rangeOfLevel = (maximum - minimum) / InputLevel;
            //first level
            float x = minimum;
            levelsEndPoints.Add(x);
            while (x <= maximum)
            {
                levelsEndPoints.Add(x + rangeOfLevel);
                x = x + rangeOfLevel;
            }

            // to get midpoint 
            float mid_value;
            for (int k = 0; k < InputLevel; k++)
            {
                mid_value = (levelsEndPoints[k] + levelsEndPoints[k + 1]) / 2;
                levelsMidPoint.Add(mid_value);
            }

            //Compare
            for (int i = 0; i < InputSignal.Samples.Count; i++)
            {
                for (int j = 0; j < levelsEndPoints.Count; j++)
                {
                    if (InputSignal.Samples[i] >= levelsEndPoints[j] && InputSignal.Samples[i] < levelsEndPoints[j + 1] + 0.0001)
                    {
                        quantizedSignal.Add(levelsMidPoint[j]);
                        // from decimal to Binary 
                        OutputEncodedSignal.Add(Convert.ToString(j, 2).PadLeft(InputNumBits, '0'));
                        // to add interval index in OutputIntervalIndices ...
                        OutputIntervalIndices.Add(j + 1);
                        break;
                    }
                }

            }
            OutputQuantizedSignal = new Signal(quantizedSignal, false);
            //error
            for (int i = 0; i < InputSignal.Samples.Count; i++)
            {
                
                float Quan_error = quantizedSignal[i] - InputSignal.Samples[i];
                OutputSamplesError.Add(Quan_error);
            }


        }
    }
}
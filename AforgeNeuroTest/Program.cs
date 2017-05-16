using AForge.Neuro;
using AForge.Neuro.Learning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AforgeNeuroTest
{
    class Program
    {
        private const int ALPHA_VALUE = 2;
        private const int FIRST_LAYER_NEURON_COUNT = 15;

        static void Main(string[] args)
        {
            List<double> inputs = new List<double>();
            List<double> outputs = new List<double>();

            // load training data

            // normalize input data

            // normalize output data

            var network = new ActivationNetwork(new SigmoidFunction(ALPHA_VALUE), 1, FIRST_LAYER_NEURON_COUNT, 1);
            var teacher = new BackPropagationLearning(network);
            network.Randomize();
            teacher.LearningRate = 0.75;


            int iteration = 1;
            double error = 1.0;
            while (error > 0.25)
            {
                iteration++;

                if ((iteration % 1000) == 0)
                {
                    Console.WriteLine("Error {0}\t| It: {1}", error, iteration);
                }
            }

            Console.ReadLine();
        }

        private static double Normalize(double value, double min, double max)
        {
            return (value - min) / (max - min);
        }

        private static double Denormalize(double value, double min, double max)
        {
            return value * (max - min) + min;
        }
    }
}

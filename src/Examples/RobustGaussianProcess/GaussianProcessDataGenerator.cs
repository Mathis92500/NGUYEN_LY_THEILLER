﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Distributions.Kernels;
using System;

namespace RobustGaussianProcess
{
    class GaussianProcessDataGenerator
    {
        public static (Vector[] dataX, double[] dataY) GenerateRandomData(int numData, double proportionCorrupt)
        {
            InferenceEngine engine = Utilities.GetInferenceEngine();

            // The points to evaluate
            Vector[] randomInputs = Utilities.VectorRange(0, 1, numData, true);

            var gaussianProcessGenerator = new GaussianProcessRegressor(randomInputs);
            gaussianProcessGenerator.Block.CloseBlock();

            // The basis
            Vector[] basis = Utilities.VectorRange(0, 1, 6, false);

            // The kernel
            var kf = new SquaredExponential(-1);

            // Fill in the sparse GP prior
            GaussianProcess gp = new GaussianProcess(new ConstantFunction(0), kf);
            gaussianProcessGenerator.Prior.ObservedValue = new SparseGP(new SparseGPFixed(gp, basis));

            // Infer the posterior Sparse GP, and sample a random function from it
            SparseGP sgp = engine.Infer<SparseGP>(gaussianProcessGenerator.F);
            var randomFunc = sgp.Sample();

            Random rng = new Random();

            double[] randomOutputs = new double[randomInputs.Length];

            // get random data
            for (int i = 0; i < randomInputs.Length; i++)
            {
                double post = randomFunc.Evaluate(randomInputs[i]);
                // corrupt data point if it we haven't exceed the proportion we wish to corrupt
                if (i < proportionCorrupt * numData)
                {
                    double sign = rng.NextDouble() > 0.5 ? 1 : -1;
                    double distance = rng.NextDouble() * 1.5;
                    post = (sign * distance) + post;
                }

                randomOutputs[i] = post;
            }

            int numCorrupted = (int)System.Math.Ceiling(numData * proportionCorrupt);
            Console.WriteLine("Model complete: Generated {0} points with {1} corrupted", numData, numCorrupted);

            return (randomInputs, randomOutputs);
        }
    }
}

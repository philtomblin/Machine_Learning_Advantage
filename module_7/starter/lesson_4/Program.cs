﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Pensar;

namespace ml_csharp_lesson4
{
    /// <summary>
    /// The application class.
    /// </summary>
    class Program
    {
        // the number of latent dimensions to use in the generator
        static readonly int latentDimensions = 32;

        // the image dimensions and number of color channels
        static readonly int imageHeight = 32;
        static readonly int imageWidth = 32;
        static readonly int channels = 3;

        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">The command line arguments</param>
        static void Main(string[] args)
        {
            // unpack archive
            if (!System.IO.File.Exists("x_channels_first_8_5.bin"))
            {
                DataUtil.Unzip(@"..\..\..\..\..\frog_pictures.zip", ".");
            }

            // load training and test data
            var trainingData = DataUtil.LoadBinary<float>("x_channels_first_8_5.bin", 5000, channels * imageWidth * imageHeight);

            // create the generator input variable
            var generatorVar = CNTK.Variable.InputVariable(new int[] { latentDimensions }, CNTK.DataType.Float, name: "generator_input");

            // create the generator
            var generator = generatorVar
                .Dense(128 * 16 * 16, v => CNTK.CNTKLib.LeakyReLU(v, 0.1))
                .Reshape(new int[] { 16, 16, 128 })
                .Convolution2D(256, new int[] { 5, 5 }, padding: true, activation: v => CNTK.CNTKLib.LeakyReLU(v, 0.1))
                .ConvolutionTranspose(
                    filterShape: new int[] { 4, 4 },
                    numberOfFilters: 256,
                    strides: new int[] { 2, 2 },
                    outputShape: new int[] { 32, 32 },
                    padding: true,
                    activation: v => CNTK.CNTKLib.LeakyReLU(v, 0.1)
                )
                .Convolution2D(256, new int[] { 5, 5 }, padding: true, activation: v => CNTK.CNTKLib.LeakyReLU(v, 0.1))
                .Convolution2D(256, new int[] { 5, 5 }, padding: true, activation: v => CNTK.CNTKLib.LeakyReLU(v, 0.1))
                .Convolution2D(channels, new int[] { 7, 7 }, padding: true, activation: CNTK.CNTKLib.Tanh)
                .ToNetwork();

            // create the discriminator input variable
            var discriminatorVar = CNTK.Variable.InputVariable(new int[] { imageWidth, imageHeight, channels }, CNTK.DataType.Float, name: "discriminator_input");

            // create the discriminator
            var discriminator = discriminatorVar
                .Convolution2D(128, new int[] { 3, 3 }, activation: v => CNTK.CNTKLib.LeakyReLU(v, 0.1))
                .Convolution2D(128, new int[] { 4, 4 }, strides: new int[] { 2, 2 }, activation: v => CNTK.CNTKLib.LeakyReLU(v, 0.1))
                .Convolution2D(128, new int[] { 4, 4 }, strides: new int[] { 2, 2 }, activation: v => CNTK.CNTKLib.LeakyReLU(v, 0.1))
                .Convolution2D(128, new int[] { 4, 4 }, strides: new int[] { 2, 2 }, activation: v => CNTK.CNTKLib.LeakyReLU(v, 0.1))
                .Dropout(0.4)
                .Dense(1, CNTK.CNTKLib.Sigmoid)
                .ToNetwork();

            // create the Gan
            var gan = Gan.CreateGan(generator, discriminator);

            // create the label variable
            var labelVar = CNTK.Variable.InputVariable(shape: new CNTK.NDShape(0), dataType: CNTK.DataType.Float, name: "label_var");

            // set up the loss functions
            var discriminatorLoss = CNTK.CNTKLib.BinaryCrossEntropy(discriminator, labelVar);
            var ganLoss = CNTK.CNTKLib.BinaryCrossEntropy(gan, labelVar);

            // set up the learners
            var discriminatorLearner = discriminator.GetAdaDeltaLearner(1);
            var ganLearner = gan.GetAdaDeltaLearner(1);

            // set up the trainers
            var discriminatorTrainer = discriminator.GetTrainer(discriminatorLearner, discriminatorLoss, discriminatorLoss);
            var ganTrainer = gan.GetTrainer(ganLearner, ganLoss, ganLoss);

            // make sure we have an images folder to write to
            var outputFolder = "images";
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // train the gan during multiple epochs
            var numEpochs = 100000;
            var batchSize = 20;
            var start = 0;
            for (var epoch = 0; epoch < numEpochs; epoch++)
            {
                // run the generator and create a set of fake frog images
                var generatedImages = Gan.GenerateImages(generator, batchSize, latentDimensions);

                // get a training batch: a mix of fake and real images labelled correctly
                start = Math.Min(start, trainingData.Length - batchSize);
                var batch = Gan.GetTrainingBatch(discriminatorVar, generatedImages, trainingData, batchSize, start);
                start += batchSize;
                if (start >= trainingData.Length)
                {
                    start = 0;
                }

                // train the discriminator
                var discriminatorResult = discriminatorTrainer.TrainBatch(
                    new[] {
                        (discriminator.Arguments[0], batch.featureBatch),
                        (labelVar, batch.labelBatch)
                    }, true);

                // get a misleading batch: all fake images but labelled as real
                var misleadingBatch = Gan.GetMisleadingBatch(gan, batchSize, latentDimensions);

                // train the gan
                var ganResult = ganTrainer.TrainBatch(
                    new[] {
                        (gan.Arguments[0], misleadingBatch.featureBatch),
                        (labelVar, misleadingBatch.labelBatch)
                    }, true);

                // report result
                if (epoch % 10 == 0)
                {
                    Console.WriteLine($"Epoch: {epoch}, Discriminator loss: {discriminatorResult.Loss}, Gan loss: {ganResult.Loss}");
                    var path = Path.Combine(outputFolder, $"generated_frog_{epoch}.png");
                    Gan.SaveImage(generatedImages[0].ToArray(), imageWidth, imageHeight, path);
                    path = Path.Combine(outputFolder, $"actual_frog_{epoch}.png");
                    Gan.SaveImage(trainingData[Math.Max(start - batchSize, 0)], imageWidth, imageHeight, path);
                }
            }

            
        }
    }
}

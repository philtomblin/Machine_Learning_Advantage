using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Pensar;

namespace ml_csharp_lesson3
{
    /// <summary>
    /// The application class.
    /// </summary>
    class Program
    {
        // paths to the content and style images
        static readonly string contentImagePath = "content.png";
        static readonly string styleImagePath = "style.png";

        // the width and height to resize the images to
        static readonly int imageHeight = 400;
        static readonly int imageWidth = 381;
        
        // the number of training epochs and reporting interval
        static readonly int numEpochs = 300;
        static readonly int reportInterval = 25;


        /// <summary>
        /// Show the inferred image.
        /// </summary>
        /// <param name="imageData">The image data of the inferred image.</param>
        static void ShowImage(byte[] imageData)
        {
            var mat = new OpenCvSharp.Mat(imageHeight, imageWidth, OpenCvSharp.MatType.CV_8UC3, imageData, 3 * imageWidth);
            Cv2.ImShow("Image With Style Transfer", mat);
            Cv2.WaitKey(0);
        }

        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">The command line arguments</param>
        static void Main(string[] args)
        {
            // load images
            var contentImage = StyleTransfer.LoadImage(contentImagePath, imageWidth, imageHeight);
            var styleImage = StyleTransfer.LoadImage(styleImagePath, imageWidth, imageHeight);

            // create the feature variable
            var featureVariable = CNTK.Variable.InputVariable(new int[] { imageWidth, imageHeight, 3 }, CNTK.DataType.Float);

            // create the neural network base (just the content and style layers)
            var model = featureVariable
                .VGG19(freeze: true)
                .StyleTransferBase();

            // calculate the labels
            var labels = StyleTransfer.CalculateLabels(model, contentImage, styleImage);

            // add the dream layer
            model = model
                .DreamLayer(contentImage, imageWidth, imageHeight);

            // create the label variable
            var contentAndStyle = model.GetContentAndStyleLayers();
            var labelVariable = new List<CNTK.Variable>();
            for (int i = 0; i < labels.Length; i++)
            {
                var shape = contentAndStyle[i].Shape;
                var input_variable = CNTK.Variable.InputVariable(shape, CNTK.DataType.Float, "content_and_style_" + i);
                labelVariable.Add(input_variable);
            }

            // create the loss function
            var lossFunction = StyleTransfer.CreateLossFunction(model, contentAndStyle, labelVariable);

            // set up an AdamLearner
            var learner = model.GetAdamLearner(10, 0.95);

            // get the model trainer
            var trainer = model.GetTrainer(learner, lossFunction, lossFunction);

            // create the batch to train on
            var trainingBatch = StyleTransfer.CreateBatch(lossFunction, labels);

            // train the model
            Console.WriteLine();
            for (int i = 1; i <= numEpochs; i++)
            {
                trainer.TrainMinibatch(trainingBatch, true, NetUtil.CurrentDevice);
                if (i % reportInterval == 0)
                    Console.WriteLine($"epoch {i}, training loss = {trainer.PreviousMinibatchLossAverage()}");
            }

            // evaluate the model on a batch
            var evaluationBatch = StyleTransfer.CreateBatch(model, labels);
            var img = model.InferImage(evaluationBatch);

            // show image
            ShowImage(img);
        }
    }
}

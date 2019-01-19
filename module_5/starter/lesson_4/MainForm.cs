﻿using CppInterop.LandmarkDetector;
using FaceAnalyser_Interop;
using FaceDetectorInterop;
using MoreLinq;
using OpenCVWrappers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using UtilitiesOF;

namespace ml_csharp_lesson4
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// A dictionary to map action units to emotions.
        /// </summary>
        /*
        public Dictionary<string, List<int>> EmotionList = new Dictionary<string, List<int>>()
        {
            { "Happy",              new List<int>() { 12, 25, 6 } },
            { "Sad",                new List<int>() { 4, 15, 1, 6, 11, 17 } },
            { "Afraid",             new List<int>() { 1, 4, 20, 25, 2, 5, 26 } },
            { "Angry",              new List<int>() { 4, 7, 24, 10, 17, 23 } },
            { "Surprised",          new List<int>() { 1, 2, 25, 26, 5 } },
            { "Disgusted",          new List<int>() { 9, 10, 17, 4, 24 } },
            { "Happy Surprised",    new List<int>() { 1, 2, 12, 25, 5, 26 } },
            { "Happy Disgusted",    new List<int>() { 10, 12, 25, 4, 6, 9 } },
            { "Sad Fearful",        new List<int>() { 1, 4, 20, 25, 2, 5, 6, 15 } },
            { "Sad Angry",          new List<int>() { 4, 15, 6, 7, 11, 17 } },
            { "Sad Surprised",      new List<int>() { 1, 4, 25, 26, 2, 6 } },
            { "Sad Disgusted",      new List<int>() { 4, 10, 1, 6, 9, 11, 15, 17, 25 } },
            { "Afraid Angry",       new List<int>() { 4, 20, 25, 5, 7, 10, 11 } },
            { "Afraid Surprised",   new List<int>() { 1, 2, 5, 20, 25, 4, 10, 11, 26 } },
            { "Afraid Disgusted",   new List<int>() { 1, 4, 10, 20, 25, 2, 5, 6, 9, 15 } },
            { "Angry Surprised",    new List<int>() { 4, 25, 26, 5, 7, 10 } },
            { "Angry Disgusted",    new List<int>() { 4, 10, 17, 7, 9, 24 } },
            { "Disgusted Surprised",new List<int>() { 1, 2, 5, 10, 4, 9, 17, 24 } },
            { "Appalled",           new List<int>() { 4, 10, 6, 9, 17, 24 } },
            { "Hatred",             new List<int>() { 4, 10, 7, 9, 17, 24 } },
            { "Awed",               new List<int>() { 1, 2, 5, 25, 4, 20, 26 } }
        };
        */
        
        public class Emotion
        {
            public string Name;
            public List<int> ActionUnits;

            public Emotion(string name, List<int> actionUnits)
            {
                Name = name;
                ActionUnits = actionUnits;
            }
        }

        public List<Emotion> EmotionList = new List<Emotion>()
        {
            new Emotion("Happy", new List<int>() { 12 } ),
            new Emotion("Happy", new List<int>() { 6, 12 } ),
            new Emotion("Sad", new List<int>() { 1, 4 } ),
            new Emotion("Sad", new List<int>() { 1, 4, 11 } ),
            new Emotion("Sad", new List<int>() { 1, 4, 15 } ),
            new Emotion("Sad", new List<int>() { 1, 4, 15, 17 } ),
            new Emotion("Sad", new List<int>() { 6, 15 } ),
            new Emotion("Sad", new List<int>() { 11, 17 } ),
            new Emotion("Sad", new List<int>() { 1 } ),
            new Emotion("Surprise", new List<int>() { 1, 2, 5, 26 } ),
            //new Emotion("Surprise", new List<int>() { 1, 2, 5, 27 } ),
            new Emotion("Surprise", new List<int>() { 1, 2, 5 } ),
            new Emotion("Surprise", new List<int>() { 1, 2, 26 } ),
            //new Emotion("Surprise", new List<int>() { 1, 2, 27 } ),
            new Emotion("Surprise", new List<int>() { 5, 26 } ),
            //new Emotion("Surprise", new List<int>() { 5, 27 } ),
            new Emotion("Disgust", new List<int>() { 9, 17 } ),
            new Emotion("Disgust", new List<int>() { 10, 17 } ),
            //new Emotion("Disgust", new List<int>() { 9, 16, 25 } ),
            //new Emotion("Disgust", new List<int>() { 9, 16, 26 } ),
            //new Emotion("Disgust", new List<int>() { 10, 16, 25 } ),
            //new Emotion("Disgust", new List<int>() { 10, 16, 26 } ),
            new Emotion("Disgust", new List<int>() { 9 } ),
            new Emotion("Disgust", new List<int>() { 10 } ),
            new Emotion("Fear", new List<int>() { 1, 2, 4 } ),
            new Emotion("Fear", new List<int>() { 1, 2, 4, 5, 20, 25 } ),
            new Emotion("Fear", new List<int>() { 1, 2, 4, 5, 20, 26 } ),
            //new Emotion("Fear", new List<int>() { 1, 2, 4, 5, 20, 27 } ),
            new Emotion("Fear", new List<int>() { 1, 2, 4, 5, 25 } ),
            new Emotion("Fear", new List<int>() { 1, 2, 4, 5, 26 } ),
            //new Emotion("Fear", new List<int>() { 1, 2, 4, 5, 27 } ),
            new Emotion("Fear", new List<int>() { 1, 2, 4, 5 } ),
            new Emotion("Fear", new List<int>() { 1, 2, 5, 25 } ),
            new Emotion("Fear", new List<int>() { 1, 2, 5, 26 } ),
            //new Emotion("Fear", new List<int>() { 1, 2, 5, 27 } ),
            new Emotion("Fear", new List<int>() { 5, 20, 25 } ),
            new Emotion("Fear", new List<int>() { 5, 20, 26 } ),
            //new Emotion("Fear", new List<int>() { 5, 20, 27 } ),
            new Emotion("Fear", new List<int>() { 5, 20 } ),
            new Emotion("Fear", new List<int>() { 20 } ),
            //new Emotion("Anger", new List<int>() { 4, 5, 7, 10, 22, 23, 25 } ),
            //new Emotion("Anger", new List<int>() { 4, 5, 7, 10, 22, 23, 26 } ),
            new Emotion("Anger", new List<int>() { 4, 5, 7, 10, 23, 25 } ),
            new Emotion("Anger", new List<int>() { 4, 5, 7, 10, 23, 26 } ),
            new Emotion("Anger", new List<int>() { 4, 5, 7, 17, 23 } ),
            //new Emotion("Anger", new List<int>() { 4, 5, 7, 17, 24 } ),
            new Emotion("Anger", new List<int>() { 4, 5, 7, 23 } ),
            //new Emotion("Anger", new List<int>() { 4, 5, 7, 24 } ),
            new Emotion("Anger", new List<int>() { 4, 5 } ),
            new Emotion("Anger", new List<int>() { 4, 7 } ),
            //new Emotion("Anger", new List<int>() { 17, 24 } )
        };

        /// <summary>
        /// Initialize MainForm.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when MainForm loads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // load the input image
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\images");
            var reader = new SequenceReader(folder, true);

            // process image
            var bitmap = ProcessImage(reader);

            // show new image
            pictureBox.Image = bitmap;
        }

        private Bitmap ProcessImage(SequenceReader reader)
        {
            // set up the face model
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\");
            var faceModel = new FaceModelParameters(root, false);
            faceModel.optimiseForImages();

            // set up a face detector, a landmark detector, and a face analyzer
            var faceDetector = new FaceDetector();
            var landmarkDetector = new CLNF(faceModel);
            var faceAnalyser = new FaceAnalyserManaged(root, true, 0);

            // read the image from the sequence reader
            var frame = new RawImage(reader.GetNextImage());
            var grayFrame = new RawImage(reader.GetCurrentFrameGray());

            // detect faces
            var faces = new List<Rect>();
            var confidences = new List<double>();
            faceDetector.DetectFacesHOG(faces, grayFrame, confidences);

            // detect landmarks
            var image = frame.ToBitmap();
            foreach (var face in faces)
            {
                landmarkDetector.DetectFaceLandmarksInImage(grayFrame, face, faceModel);
                var points = landmarkDetector.CalculateAllLandmarks();

                // calculate action units
                var features = faceAnalyser.PredictStaticAUsAndComputeFeatures(grayFrame, points);

                // find the action units
                var actionUnits = (from au in features.Item2
                                   where au.Value > 0
                                   orderby au.Key
                                   select au.Key);

                // get top emotions
                var topEmotions = GetTopEmotions(actionUnits);

                // draw the emotion on the face
                using (Graphics g = Graphics.FromImage(image))
                {
                    string name = string.Join(Environment.NewLine, topEmotions);
                    Font fnt = new Font("Verdana", 15, GraphicsUnit.Pixel);
                    Brush brs = new SolidBrush(Color.Black);
                    System.Drawing.SizeF stringSize = g.MeasureString(name, fnt);
                    var x = Math.Max((int)face.X, 0);
                    var y = Math.Max((int)face.Y, 0);
                    g.FillRectangle(new SolidBrush(Color.Yellow), x, y - 80, stringSize.Width, stringSize.Height);
                    g.DrawString(name, fnt, brs, x, y - 80);
                }
            }

            return image;
        }

        /// <summary>
        /// Get the top 3 emotions from the specified action units.
        /// </summary>
        /// <param name="actionUnits">The current action units.</param>
        /// <returns>The top 3 emotions corresponding to the action units.</returns>
        private IEnumerable<string> GetTopEmotions(IEnumerable<string> actionUnits)
        {
            // for each emotion, count how many action units match
            var emotionMatches = (from e in EmotionList
                                  let total = e.ActionUnits.Count()
                                  let conf = 100.0 * e.ActionUnits.Count(au => actionUnits.Contains($"AU{au:00}")) / total
                                  select new
                                  {
                                      Emotion = e.Name,
                                      Confidence = conf
                                  });

            // remove dupicates
            emotionMatches = emotionMatches.OrderByDescending(e => e.Confidence).DistinctBy(e => e.Emotion);

            // get the top emotions
            return (from e in emotionMatches
                    orderby e.Confidence descending
                    select $"{e.Emotion} {e.Confidence:0}%").Take(3);
        }
    }
}

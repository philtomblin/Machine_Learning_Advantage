using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Accord.Imaging.Filters;
using Accord.Imaging;
using Accord;
using System.Collections.Generic;

namespace ml_csharp_lesson2
{
    public struct LineRecord
    {
        public HoughLine HoughLine;
        public int ThetaBucket;
        public int StartFrame;
    }

    public partial class MainForm : Form
    {
        public const int EdgeThreshold = 130; // 130
        public const int LineCount = 50; // 50
        public const int RadiusLimit = 80; // 80
        public const int ThetaLimit = 12; // 12
        public const int ProcessNFrames = 5; // 5
        public const int CachedFrames = 30; // 100
        public const int OverwriteFrames = 50; // 100
        public const int BucketSize = 30; // 30


        // a list of recently detected lane lines
        private List<LineRecord> lineRecords = new List<LineRecord>();

        // the running frame counter
        private int frameCount = 0;

        /// <summary>
        /// Initialize MainForm.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the video player to read a video file from disk.
        /// </summary>
        /// <param name="fileName"></param>
        private void SetVideo(string fileName)
        {
            var source = new Accord.Video.FFMPEG.VideoFileSource(fileName);
            videoPlayer.VideoSource = source;
        }

        /// <summary>
        /// Called when MainForm loads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            SetVideo("./input.mp4");

            // start the player
            videoPlayer.Start();
        }

        /// <summary>
        /// Called when MainForm is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            videoPlayer.Stop();
        }

        /// <summary>
        /// Called when videoPlayer receives a new frame. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="image"></param>
        private void videoPlayer_NewFrame(object sender, ref Bitmap image)
        {
            // detect lane lines
            if (frameCount++ % ProcessNFrames == 0)
            {
                // remove old lines
                lineRecords.RemoveAll(lr => frameCount - lr.StartFrame > CachedFrames);

                var laneLines = DetectLaneLines(image);

                // bucket the lines by angle (to avoid duplicates)
                foreach (var line in laneLines)
                {
                    var lineRecord = new LineRecord()
                    {
                        HoughLine = line,
                        ThetaBucket = (int)line.Theta / BucketSize,
                        StartFrame = frameCount
                    };

                    // ensure there are never too many lines at once - first remove the older line that is in the same bucket
                    if (lineRecords.Count > 5)
                    {
                        var oldLr = lineRecords.SingleOrDefault(lr => lr.ThetaBucket == lineRecord.ThetaBucket);
                        if (frameCount - oldLr.StartFrame > OverwriteFrames)
                            lineRecords.Remove(oldLr);
                    }

                    // if a line does not exist in the bucket already then add it
                    if (!lineRecords.Any(l => l.ThetaBucket == lineRecord.ThetaBucket))
                        lineRecords.Add(lineRecord);
                }
            }

            // create array of HoughLines from remaining lines
            var linesToPlot = lineRecords.Select(lr => lr.HoughLine).ToArray();

            // draw the lanes on the main camera image
            if (linesToPlot != null)
            {
                Utility.DrawLaneLines(linesToPlot, image, Color.LightGreen, 2);

                // ... and in the bottom right box
                var laneImg = new Bitmap(image.Width, image.Height);
                Utility.DrawLaneLines(linesToPlot, laneImg, Color.White, 1);
                laneBox.Image = laneImg;
            }
        }

        /// <summary>
        /// Draw rectangles in the specified image.
        /// </summary>
        /// <param name="rectangles">The array of rectangles to draw</param>
        /// <param name="image">The image to draw the rectangles in</param>
        /// <param name="color">The drawing color to use</param>
        private void DrawRectangles(Rectangle rect, Bitmap image, Color color)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                var pen = new Pen(color)
                {
                    Width = 2f
                };
                g.DrawLine(pen, rect.X, rect.Y, rect.X + rect.Width, rect.Y);
                g.DrawLine(pen, rect.X, rect.Y, rect.X, rect.Y + rect.Height);
                g.DrawLine(pen, rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                g.DrawLine(pen, rect.X, rect.Y + rect.Height, rect.X + rect.Width, rect.Y + rect.Height);
            }
        }

        /// <summary>
        /// Detect the highway lane boundaries.
        /// </summary>
        /// <param name="image">The camera frame to process</param>
        /// <returns>The detected lane lines in the frame</returns>
        private HoughLine[] DetectLaneLines(Bitmap image)
        {
            // convert the image to grayscale
            var frame = Grayscale.CommonAlgorithms.BT709.Apply(image);

            // threshold the edges
            var threshold = new Threshold(EdgeThreshold);
            threshold.ApplyInPlace(frame);

            // draw a rectangle to black-out everything above the horizon
            var fill = new CanvasFill(new Rectangle(0, 0, image.Width, (int)(image.Height * 0.62)), Color.Black);
            fill.ApplyInPlace(frame);

            // detect edges
            var edgeDetector = new CannyEdgeDetector();
            edgeDetector.ApplyInPlace(frame);

            // do a hough line transformation, which will search for straight lines in the frame
            var transform = new HoughLineTransformation();
            transform.ProcessImage(frame);
            var candidateLines = transform.GetMostIntensiveLines(LineCount);

            // limit only to lines within a specified angle
            var lines = candidateLines.Where(line => Math.Abs(line.Radius) <= RadiusLimit && Math.Abs(line.Theta - 90) >= ThetaLimit);

            // show the edge detection view in the bottom left box
            edgeBox.Image = (Bitmap)frame.Clone();

            // return lines
            return lines.ToArray();
        }
    }
}

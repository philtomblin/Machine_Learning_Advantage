using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Accord.Imaging.Filters;
using Accord.Imaging;
using System.Collections.Generic;
using Accord.Math.Geometry;

namespace ml_csharp_lesson1
{
    public partial class MainForm : Form
    {
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
        private void VideoPlayer_NewFrame(object sender, ref Bitmap image)
        {
            // detect all traffic signs in the image
            var trafficSigns = FindTrafficSigns(image);

            // draw the rectangles in the bottom right picturebox
            var trafficSignsImg = new Bitmap(image.Width, image.Height);
            DrawRectangles(trafficSigns, trafficSignsImg, Color.White);
            trafficSignBox.Image = trafficSignsImg;

            // highlight each sign in the main image with a green rectangle
            DrawRectangles(trafficSigns, image, Color.LightGreen);
        }

        /// <summary>
        /// Draw rectangles in the specified image.
        /// </summary>
        /// <param name="rectangles">The array of rectangles to draw</param>
        /// <param name="image">The image to draw the rectangles in</param>
        /// <param name="color">The drawing color to use</param>
        private void DrawRectangles(Rectangle[] rectangles, Bitmap image, Color color)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                foreach (var rect in rectangles)
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
        }

        /// <summary>
        /// Find all traffic signs in the specified bitmap.
        /// </summary>
        /// <param name="bitmap">The bitmap in which to search for traffic signs</param>
        /// <returns>A Rectangle[] array of found traffic signs</returns>
        private Rectangle[] FindTrafficSigns(Bitmap bitmap)
        {
            // convert the image to grayscale
            var grayFrame = Grayscale.CommonAlgorithms.BT709.Apply(bitmap);

            // use a sobel edge detector to find color edges
            var edgeDetector = new SobelEdgeDetector();
            var edgeFrame = edgeDetector.Apply(grayFrame);

            // threshold the edges
            var thresholdConverter = new Threshold(230);
            thresholdConverter.ApplyInPlace(edgeFrame);

            // show the thresholded image in the pip window
            edgeBox.Image = (Bitmap)edgeFrame.Clone();

            // use a blobcounter to find all shapes
            //var blobsFilter = new BlobsFilter();
            var detector = new BlobCounter()
            {
                FilterBlobs = true,
                CoupledSizeFiltering = false,
                //BlobsFilter = blobsFilter,
                MinWidth = 8,
                MinHeight = 8,
                MaxWidth = 200,
                MaxHeight = 200
            };
            detector.ProcessImage(edgeFrame);
            var blobs = detector.GetObjectsInformation();

            // Limit the search to the right-hand verge and overhead
            var verge = new Rectangle(
                (int)(edgeFrame.Width * 0.5),
                (int)(edgeFrame.Height * 0.35),
                (int)(edgeFrame.Width * 0.5),
                (int)(edgeFrame.Height * 0.4));

            var overhead = new Rectangle(
                (int)(edgeFrame.Width * 0.1),
                (int)(edgeFrame.Height * 0.05),
                (int)(edgeFrame.Width * 0.8),
                (int)(edgeFrame.Height * 0.6));

            // plot the search rectangles on the image
            DrawRectangles(new Rectangle[] { verge, overhead }, bitmap, Color.Red);

            // filter out objects outside the search rectangles and the wrong shape
            var matchesInSearchArea = blobs.Where(blob => verge.Contains(blob.Rectangle) || overhead.Contains(blob.Rectangle));

            // filter down based on shapes - we only want rectangular blobs
            var shapeChecker = new SimpleShapeChecker();
            var rectangleMatches = matchesInSearchArea.Where(blob => shapeChecker.IsQuadrilateral(detector.GetBlobsEdgePoints(blob)));

            // filter down based on colour
            var colourMatches = new List<Blob>();
            var colourDetector = new BlobCounter();
            colourDetector.ProcessImage(bitmap);
            foreach (var blob in rectangleMatches)
            {
                var newBlob = new Blob(blob);
                colourDetector.ExtractBlobsImage(bitmap, newBlob, false);
                if (newBlob.ColorMean != Color.Black)
                    colourMatches.Add(newBlob);
            }
            //var colourMatches = rectangleMatches.Where(blob => blob.ColorMean != Color.Black);

            // build a list of all matches
            return (from shape in matchesInSearchArea
                    select shape.Rectangle).ToArray();
        }
    }
}

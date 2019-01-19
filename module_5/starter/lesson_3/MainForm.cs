using Accord.Video.DirectShow;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DlibDotNet;
using DlibDotNet.Extensions;
using OpenCvSharp;
using System.Drawing;
using System.Collections.Generic;
using Accord.Math;

namespace ml_csharp_lesson3
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// The face detector to use.
        /// </summary>
        protected FrontalFaceDetector faceDetector = null;

        /// <summary>
        /// The shape predictor to use to detect landmarks
        /// </summary>
        protected ShapePredictor shapePredictor = null;

        /// <summary>
        /// The bounding box of the last-detected face on camera.
        /// </summary>
        protected DlibDotNet.Rectangle currentFace = default(DlibDotNet.Rectangle);

        /// <summary>
        /// The current list of facial landmark points.
        /// </summary>
        protected FullObjectDetection landmarkPoints = null;

        /// <summary>
        /// The current head rotation euler matrix.
        /// </summary>
        protected MatOfDouble headRotation = null;

        /// <summary>
        /// The index of the current video frame.
        /// </summary>
        protected int frameIndex = 0;

        public double leftEAR = 0;
        public double rightEAR = 0;
        public double rotationUD = 0;
        public double rotationLR = 0;
        public double rotationRot = 0;

        /// <summary>
        /// Initialize MainForm.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Detect all 68 landmarks on the face on camera
        /// </summary>
        /// <param name="image">The current camera frame to analyze</param>
        /// <param name="frameIndex">The index number of the current camera frame</param>
        /// <returns>A FullObjectDetection object containing all 68 facial landmark points</returns>
        private FullObjectDetection DetectLandmarks(Bitmap image, int frameIndex)
        {
            // convert image to dlib format
            var dlibImage = image.ToArray2D<RgbPixel>();

            // detect faces every 5 frames
            if (frameIndex % 5 == 0)
            {
                var faces = faceDetector.Detect(dlibImage);
                if (faces.Length > 0)
                {
                    // grab the first face
                    currentFace = faces.First();
                }
            }

            // detect all 68 facial landmarks on the face
            if (currentFace != default(DlibDotNet.Rectangle))
            {
                return shapePredictor.Detect(dlibImage, currentFace);
            }
            return null;
        }

        /// <summary>
        /// Detect the eye state of the face on camera.
        /// </summary>
        /// <param name="shape">The detected facial landmark points.</param>
        /// <returns>The surface area of both eyes.</returns>
        private bool AreEyesOpen(FullObjectDetection shape)
        {
            // get all landmark points of the left eye
            var leftEye = from i in Enumerable.Range(36, 6)
                          let p = shape.GetPart((uint)i)
                          select new OpenCvSharp.Point(p.X, p.Y);

            // get all landmark points of the right eye
            var rightEye = from i in Enumerable.Range(42, 6)
                           let p = shape.GetPart((uint)i)
                           select new OpenCvSharp.Point(p.X, p.Y);

            leftEAR = eyeAspectRatio(leftEye);
            rightEAR = eyeAspectRatio(rightEye);

            return leftEAR > 0.25 || rightEAR > 0.25;
        }

        private double eyeAspectRatio(IEnumerable<OpenCvSharp.Point> eye)
        {
            var v1 = Distance.Euclidean(getPointCoords(eye.ElementAt(1)), getPointCoords(eye.ElementAt(5)));
            var v2 = Distance.Euclidean(getPointCoords(eye.ElementAt(2)), getPointCoords(eye.ElementAt(4)));
            var h = Distance.Euclidean(getPointCoords(eye.ElementAt(0)), getPointCoords(eye.ElementAt(3)));

            // compute EAR
            double ear = (v1 + v2) / (2.0 * h);
            return ear;
        }

        private double[] getPointCoords(OpenCvSharp.Point point)
        {
            return new double[] { point.X, point.Y };
        }

        /// <summary>
        /// Check if the driver is facing forward.
        /// </summary>
        /// <param name="headRotation">The head rotation angles.</param>
        /// <returns>True if the driver is facing forward, false if not.</returns>
        private bool IsDriverFacingForward(MatOfDouble headRotation)
        {
            // calculate head rotation in degrees
            var leftRight = 180 * headRotation.At<double>(0, 2) / Math.PI;
            var upDown = 180 * headRotation.At<double>(0, 1) / Math.PI;
            var rotation = 180 * headRotation.At<double>(0, 0) / Math.PI;

            // looking straight ahead wraps at -180/180, so make the range smooth
            upDown = Math.Sign(upDown) * 180 - upDown;

            rotationLR = leftRight;
            rotationUD = upDown;
            rotationRot = rotation;

            return Math.Abs(leftRight) < 35 && upDown > -15 && upDown < 5;
        }

        /// <summary>
        /// Set the camera player to read from the built-in camera.
        /// </summary>
        private void SetCamera()
        {
            // use the first video input device
            var deviceName = (from d in new FilterInfoCollection(FilterCategory.VideoInputDevice)
                              select d).FirstOrDefault();
            var captureDevice = new VideoCaptureDevice(deviceName.MonikerString);

            // switch to 640x480 resolution
            captureDevice.VideoResolution = (from r in captureDevice.VideoCapabilities
                                             where r.FrameSize.Width == 640
                                             select r).First();

            videoPlayer.VideoSource = captureDevice;
        }

        /// <summary>
        /// Called when MainForm loads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // initialize video player
            SetCamera();

            // load the car picture
            carBox.Image = Bitmap.FromFile(@"input.jpg");

            // initialize face detector
            faceDetector = FrontalFaceDetector.GetFrontalFaceDetector();

            // initialize shape predictor to detect landmarks
            shapePredictor = new ShapePredictor("shape_predictor_68_face_landmarks.dat");

            // start the players
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

            // dispose dlib resources
            if (faceDetector != null)
            {
                faceDetector.Dispose();
                faceDetector = null;
            }
            if (shapePredictor != null)
            {
                shapePredictor.Dispose();
                shapePredictor = null;
            }
        }

        /// <summary>
        /// Called when videoPlayer receives a new frame. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="image"></param>
        private void videoPlayer_NewFrameReceived(object sender, Accord.Video.NewFrameEventArgs eventArgs)
        {
            // get the current camera frame
            var frame = eventArgs.Frame;

            // get the landmark points
            landmarkPoints = DetectLandmarks(frame, frameIndex);

            // do we have 68 landmark points?
            if (landmarkPoints != null && landmarkPoints.Parts == 68)
            {
                // draw the landmark points in the bottom right box
                var poseImage = new Bitmap(frame.Width, frame.Height);
                Utility.DrawLandmarkPoints(landmarkPoints, poseImage);

                // draw the eye area in the bottom left box
                var eyeImage = new Bitmap(frame.Width, frame.Height);
                Utility.DrawEyeArea(landmarkPoints, eyeImage);
                eyeBox.Image = eyeImage;

                // build a quick and dirty camera calibration matrix
                var cameraMatrix = Utility.GetCameraMatrix(eventArgs.Frame.Width, eventArgs.Frame.Height);

                // detect head angle
                Utility.DetectHeadAngle(
                    landmarkPoints, cameraMatrix, 
                    out Mat rotation, 
                    out Mat translation, 
                    out MatOfDouble coefficients);

                // draw the pose line in the bottom right box
                Utility.DrawPoseLine(rotation, translation, cameraMatrix, coefficients, landmarkPoints, poseImage);
                headBox.Image = poseImage;

                // get the euler angles
                headRotation = Utility.GetEulerMatrix(rotation);
            }

            // update frame counter
            frameIndex++;
        }

        /// <summary>
        /// Timer event to update the user interface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            if (headRotation == null)
                return;

            // is the driver facing forward?
            var isFacingForward = IsDriverFacingForward(headRotation);
            rotUpDown.Text = rotationUD.ToString();
            rotLeftRight.Text = rotationLR.ToString();
            rotRotation.Text = rotationRot.ToString();

            // are the drivers' eyes open? 
            var areEyesOpen = AreEyesOpen(landmarkPoints);
            leftEyeEAR.Text = leftEAR.ToString();
            rightEyeEAR.Text = rightEAR.ToString();

            // show the autopilot disengage warning if the driver is alert
            autopilotLabel.Visible = !(isFacingForward && areEyesOpen);
        }

    }
}

using ExtensionMethods;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ml_csharp_lesson1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Face API credentials
        // **********************************
        // PUT YOUR FACE API KEY AND URL HERE
        // **********************************
        private const string FACE_KEY = "1de90ec2e6b648b3ad905981455e08c0";
        private const string FACE_API = "https://uksouth.api.cognitive.microsoft.com/face/v1.0";

        /// <summary>
        /// The celebrity image to analyze. 
        /// </summary>
        protected BitmapImage image;

        /// <summary>
        /// The array of detected faces.
        /// </summary>
        protected Face[] faces;

        /// <summary>
        /// The class constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Run a complete face detection on a new image.
        /// </summary>
        private async Task RunFaceDetection()
        {
            // show the spinner
            Spinner.Visibility = Visibility.Visible;

            // clear any existing face rectangles
            var toDelete = new List<UIElement>(MainCanvas.Children.OfType<System.Windows.Shapes.Rectangle>());
            foreach (var element in toDelete)
                MainCanvas.Children.Remove(element);

            // detect all faces in image
            faces = await DetectFaces(image);

            // draw face rectangles on the canvas
            foreach (var face in faces)
            {
                DrawFaceRectangle(face);
            }

            // hide the spinner
            Spinner.Visibility = Visibility.Hidden;

            // tell the canvas to redraw itself
            MainCanvas.InvalidateVisual();
        }

        /// <summary>
        /// Detect all faces in a given image.
        /// </summary>
        /// <param name="image">The image to check for faces.</param>
        /// <returns>An array of Face instances describing each face in the image.</returns>
        private async Task<Face[]> DetectFaces(BitmapImage image)
        {
            // write the image to a stream
            var stream = new MemoryStream();
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);

            // detect faces
            var faceClient = new FaceServiceClient(FACE_KEY, FACE_API);
            var attributes = new FaceAttributeType[] {
                FaceAttributeType.Age,
                FaceAttributeType.Accessories,
                FaceAttributeType.Emotion,
                FaceAttributeType.FacialHair,
                FaceAttributeType.Gender,
                FaceAttributeType.Glasses,
                FaceAttributeType.Hair,
                FaceAttributeType.Makeup };
            return await faceClient.DetectAsync(stream, true, false, attributes);
        }

        /// <summary>
        /// Draw the rectangle of the given face.
        /// </summary>
        /// <param name="face">The face to draw the rectangle for.</param>
        private void DrawFaceRectangle(Face face)
        {
            double scale;
            
            if ((image.PixelWidth / MainCanvas.ActualWidth) < (image.PixelHeight / MainCanvas.ActualHeight))
                scale = MainCanvas.ActualHeight / image.PixelHeight; // height is limiting factor
            else
                scale = MainCanvas.ActualWidth / image.PixelWidth; // width is limiting factor

            // select colour based on gender
            Color colour;
            switch (face.FaceAttributes.Gender)
            {
                case "female":
                    colour = Color.FromRgb(255, 16, 150);
                    break;
                case "male":
                    colour = Color.FromRgb(0, 0, 255);
                    break;
                default:
                    colour = Color.FromRgb(255, 255, 255);
                    break;
            }

            // create face rectangle
            var rectangle = new System.Windows.Shapes.Rectangle
            {
                Stroke = new SolidColorBrush(colour),
                Fill = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)),
                StrokeThickness = 2,
                Width = face.FaceRectangle.Width * scale,
                Height = face.FaceRectangle.Height * scale,
                Tag = face.FaceId
            };
            Canvas.SetLeft(rectangle, face.FaceRectangle.Left * scale);
            Canvas.SetTop(rectangle, face.FaceRectangle.Top * scale);

            // add hover effect
            rectangle.MouseEnter += (sender, e) => {
                ((System.Windows.Shapes.Rectangle)sender).StrokeThickness = 5; };
            rectangle.MouseLeave += (sender, e) => {
                ((System.Windows.Shapes.Rectangle)sender).StrokeThickness = 2; };

            // add click handler
            rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;

            MainCanvas.Children.Add(rectangle);
        }

        /// <summary>
        /// Fires when the main window has initialized.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Initialized(object sender, EventArgs e)
        {
            // load the image
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "EllenSelfie.jpg");
            image = new BitmapImage(new Uri(path));
            var brush = new ImageBrush(image)
            {
                Stretch = Stretch.Uniform,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
            MainCanvas.Background = brush;
        }

        /// <summary>
        /// Fires when the main window has loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RunFaceDetection();
        }

        /// <summary>
        /// Handle a left mouse button down event on a face rectangle.
        /// </summary>
        /// <param name="sender">the face rectangle that sent the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Rectangle_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // helper method to convert Glasses value to string
            string glassesToString(Glasses glasses)
            {
                switch (glasses)
                {
                    case Microsoft.ProjectOxford.Face.Contract.Glasses.NoGlasses:
                        return "None";
                    case Microsoft.ProjectOxford.Face.Contract.Glasses.Sunglasses:
                        return "Sunglasses";
                    case Microsoft.ProjectOxford.Face.Contract.Glasses.ReadingGlasses:
                        return "Reading Glasses";
                    case Microsoft.ProjectOxford.Face.Contract.Glasses.SwimmingGoggles:
                        return "Swimming Goggles";
                    default:
                        return "None";
                }
            };

            // helper method to convert Glasses value to string
            string accessoriesToString(Accessory[] accessories)
            {
                var sb = new StringBuilder();

                foreach (var accessory in accessories)
                    switch (accessory.Type)
                    {
                        case AccessoryType.Headwear:
                            sb.Append(", Hat");
                            break;
                        case AccessoryType.Glasses:
                            sb.Append(", Glasses");
                            break;
                        case AccessoryType.Mask:
                            sb.Append(", Mask");
                            break;
                        default:
                            break;
                    }
                
                if (sb.Length > 0)
                    sb.Remove(0, 2);

                return sb.ToString();
            };

            var rectangle = (System.Windows.Shapes.Rectangle)sender;
            var face = faces.FirstOrDefault(f => f.FaceId == (Guid)rectangle.Tag);

            if (face != null)
            {
                Gender.Content = $"Gender: {face.FaceAttributes.Gender.ToTitle()}";
                Age.Content = $"Age: {face.FaceAttributes.Age}";
                Emotion.Content = $"Emotion: {face.FaceAttributes.Emotion.ToRankedList().First().Key.ToTitle()}";
                Hair.Content = $"Hair: {face.FaceAttributes.Hair.HairColor.OrderByDescending(hc => hc.Confidence).FirstOrDefault()?.Color.ToString().ToTitle()}";
                Beard.Content = $"Beard: {face.FaceAttributes.FacialHair.Beard * 100}%";
                Moustache.Content = $"Moustache: {face.FaceAttributes.FacialHair.Moustache * 100}%";
                Glasses.Content = $"Glasses: {glassesToString(face.FaceAttributes.Glasses)}";
                EyeMakeup.Content = $"Eye Makeup: {(face.FaceAttributes.Makeup.EyeMakeup ? "Yes" : "No")}";
                LipMakeup.Content = $"Lip Makeup: {(face.FaceAttributes.Makeup.LipMakeup ? "Yes" : "No")}";
                Accessories.Content = $"Accessories: {accessoriesToString(face.FaceAttributes.Accessories)}";
            }
        }

        /// <summary>
        /// Called when the OpenButton is clicked.
        /// </summary>
        /// <param name="sender">the button that sent the event.</param>
        /// <param name="e">The event arguments.</param>
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            // open file dialog
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                image = new BitmapImage(new Uri(openFileDialog.FileName));

                // set the new background image
                var brush = new ImageBrush(image)
                {
                    Stretch = Stretch.Uniform,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };
                MainCanvas.Background = brush;

                // run face detection on new image
                await RunFaceDetection();
            }
        }
    }
}

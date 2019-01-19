using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ml_csharp_lesson3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // ************************************
        // PUT YOUR VISION API KEY AND URL HERE
        // ************************************

        // Vision API credentials
        private const string VISION_KEY = "ec137465a84540b5b4a9bf13c4625b30";
        private const string VISION_API = "https://uksouth.api.cognitive.microsoft.com/";

        /// <summary>
        /// The celebrity image to analyze. 
        /// </summary>
        protected BitmapImage image;

        /// <summary>
        /// The current image description.
        /// </summary>
        protected string description;

        /// <summary>
        /// The class constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Run a complete detection on a new image.
        /// </summary>
        private async Task RunDetection()
        {
            // show the spinner
            Spinner.Visibility = Visibility.Visible;

            // detect what's in the image
            description = await DetectScene(image);

            // show the description
            Description.Text = description;

            // hide the spinner
            Spinner.Visibility = Visibility.Hidden;

            // tell the canvas to redraw itself
            MainCanvas.InvalidateVisual();

            // speak the description
            await SpeakDescription(description);
        }

        /// <summary>
        /// Detect what's in the given image.
        /// </summary>
        /// <param name="image">The image to check.</param>
        /// <returns>An AnalysisResult instance that describes what's in the image.</returns>
        private async Task<string> DetectScene(BitmapImage image)
        {
            ImageDescription sceneCaptions;

            using (Stream imageFileStream = File.OpenRead(image.UriSource.LocalPath))
            {

                // analyze image and get a list of potential scenes
                var credentials = new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(VISION_KEY);
                var visionClient = new ComputerVisionClient(credentials) { Endpoint = VISION_API };
                //sceneResult = await visionClient.AnalyzeImageByDomainInStreamAsync("celebrities", imageFileStream);
                sceneCaptions = await visionClient.DescribeImageInStreamAsync(imageFileStream);
            }

            // cast result to c# class
            var sceneCaption = sceneCaptions.Captions.OrderByDescending(sc => sc.Confidence).FirstOrDefault().Text;
            return $"You're looking at {sceneCaption}";
        }

        /// <summary>
        /// Speak the image description.
        /// </summary>
        /// <param name="description">The image description to speak.</param>
        private async Task SpeakDescription(string description)
        {
            // set up an adult female voice synthesizer
            var synth = new System.Speech.Synthesis.SpeechSynthesizer();
            synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);

            // speak the description using the builtin synthesizer
            await Task.Run(() => { synth.SpeakAsync(description); });
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
            path = Path.Combine(path, "BeachScene.jpg");
            image = new BitmapImage(new Uri(path));
            var brush = new ImageBrush(image);
            brush.Stretch = Stretch.UniformToFill;
            brush.AlignmentX = AlignmentX.Left;
            brush.AlignmentY = AlignmentY.Top;
            MainCanvas.Background = brush;
        }

        /// <summary>
        /// Fires when the main window has loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //await Task.Delay(10000);
            await RunDetection();
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
                var brush = new ImageBrush(image);
                brush.Stretch = Stretch.UniformToFill;
                brush.AlignmentX = AlignmentX.Left;
                brush.AlignmentY = AlignmentY.Top;
                MainCanvas.Background = brush;

                // run face detection on new image
                await RunDetection();

            }
        }
    }
}

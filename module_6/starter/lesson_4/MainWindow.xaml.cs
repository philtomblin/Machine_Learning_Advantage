using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.ProjectOxford.Search.Image;
using System;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ml_csharp_lesson4
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

        // ************************************
        // PUT YOUR SEARCH API KEY AND URL HERE
        // ************************************

        // Search API credentials
        private const string SEARCH_KEY = "836689b6bb664771ba17b7f99b1184ec";
        //private const string SEARCH_API = "https://api.cognitive.microsoft.com/bing/v7.0"; // the endpoint provided by Azure doesn't work!
        private const string SEARCH_API = "https://api.cognitive.microsoft.com/bing/v7.0/images/search"; // this one, documented by Microsoft, does...

        // ***************************************
        // PUT YOUR SPEECH API KEY AND REGION HERE
        // ***************************************

        // Speech API credentials
        private const string SPEECH_KEY = "34b0b7f7851b47e8aa6d2a3312bb5896";
        private const string SPEECH_REGION = "westeurope";

        /// <summary>
        /// The Speech API recognition client. 
        /// </summary>
        protected SpeechRecognizer speechRecognizer;

        /// <summary>
        /// The class constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Find the requested image and show it.
        /// </summary>
        /// <param name="searchString">The string to search for.</param>
        private async Task FindImage(string searchString)
        {
            // show the spinner
            Spinner.Visibility = Visibility.Visible;

            // find an image matching the query
            var img = await SearchForImage(searchString);
            if (img != null)
            {
                // describe the image
                var description = await DescribeScene(img.ContentUrl);
                
                // show the image
                var image = new BitmapImage(new Uri(img.ContentUrl));
                var brush = new ImageBrush(image);
                brush.Stretch = Stretch.Uniform;
                brush.AlignmentX = AlignmentX.Left;
                brush.AlignmentY = AlignmentY.Top;
                MainCanvas.Background = brush;

                // hide the spinner
                Spinner.Visibility = Visibility.Hidden;

                // tell the canvas to redraw itself
                MainCanvas.InvalidateVisual();

                //// speak the description
                await SpeakDescription(description);
            }

            // hide the spinner
            else
                Spinner.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Perform a bing image search and return the top image.
        /// </summary>
        /// <param name="celebrity">The celebrity to search for.</param>
        /// <returns>The description of the celebrity.</returns>
        private async Task<Image> SearchForImage(string searchText)
        {
            var client = new ImageSearchClient(SEARCH_KEY)
            {
                Url = SEARCH_API // force use of v7 api
            };

            var request = new ImageSearchRequest()
            {
                Query = searchText
            };

            var response = await client.GetImagesAsync(request);
            return response.Images.FirstOrDefault();
        }

        /// <summary>
        /// Detect what's in the given image.
        /// </summary>
        /// <param name="url">The url of the image to check.</param>
        /// <returns>An AnalysisResult instance that describes what's in the image.</returns>
        private async Task<string> DescribeScene(string url)
        {
            // analyze image and get list of possible captions
            var credentials = new ApiKeyServiceClientCredentials(VISION_KEY);
            var visionClient = new ComputerVisionClient(credentials) { Endpoint = VISION_API };
            ImageDescription result = new ImageDescription();
            
            // sometimes the url cannot be resolved so handle this gracefully...
            try
            {
                result = await visionClient.DescribeImageAsync(url);
            }
            catch (Exception)
            {
                return "I couldn't find an image of that";
            }
            
            // get the caption with the highest confidence
            var caption = (from c in result.Captions
                           orderby c.Confidence descending
                           select c.Text).FirstOrDefault();

            // pick a random response
            string[] responses = new string[]
            {
                "Okay Phil, I found a picture of {0}",
                "Here's a picture of {0}",
                "I found an image of {0}",
                "Check out this picture I found of {0}",
                "Phil, how about this: a picture of {0}"
            };
            var rnd = new Random();
            var response = responses[rnd.Next(responses.Length)];
            return string.Format(response, caption ?? "something");
        }

        /// <summary>
        /// Speak the image description.
        /// </summary>
        /// <param name="description">The image description to speak.</param>
        private async Task SpeakDescription(string description)
        {
            if (description != null)
            {
                // set up an adult female voice synthesizer
                var synth = new System.Speech.Synthesis.SpeechSynthesizer();
                synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);

                // speak the description using the builtin synthesizer
                await Task.Run(() => { synth.SpeakAsync(description); });
            }
        }

        /// <summary>
        /// Fires when the main window loads.
        /// </summary>
        /// <param name="sender">The main window.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // create a speech API client configuration
            var config = SpeechConfig.FromSubscription(SPEECH_KEY, SPEECH_REGION);

            // create an english-language speech recognizer
            speechRecognizer = new SpeechRecognizer(config);
        }

        /// <summary>
        /// Fires when the search button is clicked.
        /// </summary>
        /// <param name="sender">The search button.</param>
        /// <param name="e">The event arguments</param>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            //await Task.Delay(10000);

            // show that the app is listening
            QueryText.Text = "<listening>";

            // listen for a single command
            var result = await speechRecognizer.RecognizeOnceAsync();

            // handle recognized speech
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                // find the image and show the query
                await FindImage(result.Text);
                QueryText.Text = result.Text;
            }
            // handle unrecognized speech
            else if (result.Reason == ResultReason.NoMatch)
                QueryText.Text = "??? I'm sorry, I didn't understand that ???";
            // handle cancelled recognition
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                QueryText.Text = $"RECOGNITION CANCELED: {cancellation.Reason}";
            }
        }

        /// <summary>
        /// Fires when the main window is unloaded.
        /// </summary>
        /// <param name="sender">The main window.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            // dispose the speech recognition engine
            if (speechRecognizer != null)
                speechRecognizer.Dispose();
        }
    }
}

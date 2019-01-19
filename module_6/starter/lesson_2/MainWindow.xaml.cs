using Microsoft.Azure.CognitiveServices.Search.EntitySearch;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ml_csharp_lesson2
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

        // ************************************************
        // PUT YOUR BING ENTITY SEARCH API KEY AND URL HERE
        // ************************************************

        // Bing Entity Search API credentials
        private const string ENTITY_KEY = "400cb02c1d6b43dabb48c02e17fd1cef";
        private const string ENTITY_API = "https://westeurope.api.cognitive.microsoft.com/";

        /// <summary>
        /// The celebrity image to analyze. 
        /// </summary>
        protected BitmapImage image;

        /// <summary>
        /// The current list of celebrities
        /// </summary>
        protected Celebrity[] celebrities;

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

            // clear any existing face rectangles and labels
            var toDelete = new List<UIElement>();
            foreach (var element in MainCanvas.Children.OfType<UIElement>())
            {
                if (element is System.Windows.Shapes.Rectangle || element is Label)
                    toDelete.Add(element);
            }
            foreach (var element in toDelete)
                MainCanvas.Children.Remove(element);

            // detect all faces in image
            celebrities = await DetectCelebrities(image);

            // draw face rectangles on the canvas
            foreach (var celebrity in celebrities)
            {
                DrawFaceRectangle(celebrity);
            }

            // hide the spinner
            Spinner.Visibility = Visibility.Hidden;

            // tell the canvas to redraw itself
            MainCanvas.InvalidateVisual();
        }

        /// <summary>
        /// Detect all celebrities in a given image.
        /// </summary>
        /// <param name="image">The image to check for celebrities.</param>
        /// <returns>An AnalysisResult instance that describes each celebrity in the image.</returns>
        private async Task<Celebrity[]> DetectCelebrities(BitmapImage image)
        {
            DomainModelResults analysisResult;

            using (Stream imageFileStream = File.OpenRead(image.UriSource.LocalPath))
            {

                // analyze image and look for celebrities
                var credentials = new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ApiKeyServiceClientCredentials(VISION_KEY);
                var visionClient = new ComputerVisionClient(credentials) { Endpoint = VISION_API };
                analysisResult = await visionClient.AnalyzeImageByDomainInStreamAsync("celebrities", imageFileStream);
            }
                        
            // cast result to c# class
            var celebritiesResults = JsonConvert.DeserializeObject<CelebritiesResult>(analysisResult.Result.ToString());
            return celebritiesResults.Celebrities;
        }

        /// <summary>
        /// Draw the rectangle of the given celebrity.
        /// </summary>
        /// <param name="celebrity">The celebrity to draw the rectangle for.</param>
        private void DrawFaceRectangle(Celebrity celebrity)
        {
            double scale;

            if ((image.PixelWidth / MainCanvas.ActualWidth) < (image.PixelHeight / MainCanvas.ActualHeight))
                scale = MainCanvas.ActualHeight / image.PixelHeight; // height is limiting factor
            else
                scale = MainCanvas.ActualWidth / image.PixelWidth; // width is limiting factor

            // create face rectangle
            var rectangle = new System.Windows.Shapes.Rectangle
            {
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 0)),
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 255, 255, 255)),
                StrokeThickness = 2,
                Width = celebrity.FaceRectangle.Width * scale,
                Height = celebrity.FaceRectangle.Height * scale,
                Tag = celebrity.Name
            };
            Canvas.SetLeft(rectangle, celebrity.FaceRectangle.Left * scale);
            Canvas.SetTop(rectangle, celebrity.FaceRectangle.Top * scale);

            // add handlers
            rectangle.MouseEnter += (sender, e) => {
                ((System.Windows.Shapes.Rectangle)sender).Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0)); };
            rectangle.MouseLeave += (sender, e) => {
                ((System.Windows.Shapes.Rectangle)sender).Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 0)); };
            rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;

            MainCanvas.Children.Add(rectangle);

            // create celebrity label
            var label = new Label()
            {
                Content = celebrity.Name,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 0)),
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0)),
                FontSize = 12
            };
            Canvas.SetLeft(label, celebrity.FaceRectangle.Left * scale);
            Canvas.SetTop(label, (celebrity.FaceRectangle.Top + celebrity.FaceRectangle.Height) * scale);
            MainCanvas.Children.Add(label);
        }

        /// <summary>
        /// Perform an entity search on the celebrity and return the description.
        /// </summary>
        /// <param name="celebrity">The celebrity to search for.</param>
        /// <returns>The description of the celebrity.</returns>
        private async Task<Thing> EntitySearch(Celebrity celebrity)
        {
            var client = new EntitySearchClient(new Microsoft.Azure.CognitiveServices.Search.EntitySearch.ApiKeyServiceClientCredentials(ENTITY_KEY));
            var data = await client.Entities.SearchAsync(query: celebrity.Name);
            if (data?.Entities?.Value?.Count > 0)
            {
                return (from v in data.Entities.Value
                        where v.EntityPresentationInfo.EntityScenario == EntityScenario.DominantEntity
                        select v).FirstOrDefault();
            }
            else
                return null;
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
        private async void Rectangle_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // start the secondary spinner
            Spinner2.Visibility = Visibility.Visible;

            // remove any existing image
            FaceImage.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));

            // find the celebrity relating to the selected rectangle
            var rectangle = (System.Windows.Shapes.Rectangle)sender;
            var celebrity = celebrities.FirstOrDefault(c => c.Name == (string)rectangle.Tag);

            // perform the search and populate the info
            var info = await EntitySearch(celebrity);
            if (info != null)
            {
                Description.Text = info.Description;

                // show the thumbnail image
                var thumbnail = new BitmapImage(new Uri(info.Image.ThumbnailUrl));
                var brush = new ImageBrush(thumbnail)
                {
                    Stretch = Stretch.Uniform
                };
                FaceImage.Background = brush;
            }

            // stop the secondary spinner
            Spinner2.Visibility = Visibility.Hidden;
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

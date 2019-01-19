using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Models.Regression.Linear;
using Deedle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ml_csharp_lesson4
{
    /// <summary>
    /// The main application class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Return a collection of bins.
        /// </summary>
        /// <param name="start">The starting bin value</param>
        /// <param name="end">The ending bin value</param>
        /// <returns>A collection of tuples that represent each bin</returns>
        private static IEnumerable<(int Min, int Max)> Bins(int start, int end)
        {
            return Enumerable.Range(start, end - start + 1)
                .Select(v => (Min: v, Max: v + 1));
        }

        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            // get data
            Console.WriteLine("Loading data....");
            var path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\california_housing.csv"));
            var housing = Frame.ReadCsv(path, separators: ",");
            housing = housing.Where(kv => ((decimal)kv.Value["median_house_value"]) < 500000);

            // convert the house value range to thousands
            housing["median_house_value"] /= 1000;

            // shuffle row indices
            var rnd = new Random();
            var indices = Enumerable.Range(0, housing.Rows.KeyCount).OrderBy(v => rnd.NextDouble());

            // shuffle the frame using the indices
            housing = housing.IndexRowsWith(indices).SortRowsByKey();

            // create the rooms_per_person feature
            housing.AddColumn("rooms_per_person",
               (housing["total_rooms"] / housing["population"]).Select(v => v.Value <= 4.0 ? v.Value : 4.0));

            // calculate binned latitudes
            var binned_latitude =
                from l in housing["latitude"].Values
                let bin = (from b in Bins(32, 41) where l >= b.Min && l < b.Max select b)
                select bin.First().Min;

            // add one-hot encoding columns
            foreach (var i in Enumerable.Range(32, 10))
            {
                housing.AddColumn($"latitude {i}-{i + 1}",
                    from l in binned_latitude
                    select l == i ? 1 : 0);
            }

            // drop the latitude column
            housing.DropColumn("latitude");

            // create training, validation, and test frames
            var training = housing.Rows[Enumerable.Range(0, 12000)];
            var validation = housing.Rows[Enumerable.Range(12000, 2500)];
            var test = housing.Rows[Enumerable.Range(14500, 2500)];

            // set up model columns
            var columns = (from i in Enumerable.Range(32, 10)
                           select $"latitude {i}-{i + 1}").ToList();
            columns.Add("median_income");
            columns.Add("rooms_per_person");

            // train the model
            var learner = new OrdinaryLeastSquares();
            var regression = learner.Learn(
                training.Columns[columns].ToArray2D<double>().ToJagged(),  // features
                training["median_house_value"].Values.ToArray());          // labels

            // display training results
            Console.WriteLine("TRAINING RESULTS");
            Console.WriteLine($"Weights:     {regression.Weights.ToString<double>("0.00")}");
            Console.WriteLine($"Intercept:   {regression.Intercept}");
            Console.WriteLine();

            // validate the model
            var validation_predictions = regression.Transform(
                validation.Columns[columns].ToArray2D<double>().ToJagged());

            // display validation results
            var validation_labels = validation["median_house_value"].Values.ToArray();
            var validation_rmse = Math.Sqrt(new SquareLoss(validation_labels).Loss(validation_predictions));
            var validation_range = Math.Abs(validation_labels.Max() - validation_labels.Min());
            Console.WriteLine("VALIDATION RESULTS");
            Console.WriteLine($"Label range: {validation_range}");
            Console.WriteLine($"RMSE:        {validation_rmse:0.00} ({validation_rmse / validation_range * 100:0.00}%)");
            Console.WriteLine();

            // test the model
            var test_predictions = regression.Transform(
                test.Columns[columns].ToArray2D<double>().ToJagged());

            // display validation results
            var test_labels = test["median_house_value"].Values.ToArray();
            var test_rmse = Math.Sqrt(new SquareLoss(test_labels).Loss(test_predictions));
            var test_range = Math.Abs(test_labels.Max() - test_labels.Min());
            Console.WriteLine("TEST RESULTS");
            Console.WriteLine($"Label range: {test_range}");
            Console.WriteLine($"RMSE:        {test_rmse:0.00} ({test_rmse / test_range * 100:0.00}%)");
                       
            Console.ReadLine();
        }
    }
}

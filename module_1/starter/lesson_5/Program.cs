﻿using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Models.Regression.Linear;
using Deedle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ml_csharp_lesson5
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

            // shuffle row indices
            var rnd = new Random();
            var indices = Enumerable.Range(0, housing.Rows.KeyCount).OrderBy(v => rnd.NextDouble());

            // shuffle the frame using the indices
            housing = housing.IndexRowsWith(indices).SortRowsByKey();

            // convert the house value range to thousands
            housing["median_house_value"] /= 1000;

            // create the rooms_per_person feature
            housing.AddColumn("rooms_per_person",
               (housing["total_rooms"] / housing["population"]).Select(v => v.Value <= 4.0 ? v.Value : 4.0));

            // bin the longitude in 12 buckets
            var vectors_long =
                from l in housing["longitude"].Values
                select Vector.Create<double>(
                    1,
                    (from b in Bins(-125, -114)
                     select l >= b.Min && l < b.Max).ToArray());

            // bin the latitude in 12 buckets
            var vectors_lat =
                from l in housing["latitude"].Values
                select Vector.Create<double>(
                    1,
                    (from b in Bins(32, 43)
                     select l >= b.Min && l < b.Max).ToArray());

            // get the outer product of the longitude and latitude vectors
            var vectors_cross =
                vectors_long.Zip(vectors_lat, (lng, lat) => lng.Outer(lat));

            // build one-hot columns out of the feature cross vectors
            for (var i = 0; i < 12; i++)
                for (var j = 0; j < 12; j++)
                    housing.AddColumn($"location {i},{j}", from v in vectors_cross select v[i, j]);

            // print out the frame to check it
            //housing.Print();

            // create training, validation, and test frames
            var training = housing.Rows[Enumerable.Range(0, 12000)];
            var validation = housing.Rows[Enumerable.Range(12000, 2500)];
            var test = housing.Rows[Enumerable.Range(14500, 2500)];

            // set up model columns
            var columns = (from i in Enumerable.Range(0, 12)
                           from j in Enumerable.Range(0, 12)
                           select $"location {i},{j}").ToList();
            columns.Add("median_income");
            columns.Add("rooms_per_person");

            // train the model
            var learner = new OrdinaryLeastSquares() { IsRobust = true };
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

            // validate the model
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

using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WTW_Task.Models;

namespace WTW_Task.Helper
{
    public class TriangleHelper
    {
        private readonly ApplicationSettings _applicationSettings;
        public TriangleHelper(ApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public List<Input> LoadFromFile(string fileName)
        {
            using var reader = new StreamReader(fileName);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            csv.Configuration.PrepareHeaderForMatch = (header, index) => Regex.Replace(header, @"\s", string.Empty);
            csv.Configuration.ReadingExceptionOccurred = exception =>
            {
                Console.WriteLine($"Bad data found: '{exception.Message}'");
                return !_applicationSettings.IgnoreBadRows;
            };

            return csv.GetRecords<Input>().ToList();
        }

        public void WriteToFile(string fileName, Report report)
        {
            using var writer = new StreamWriter(fileName);
            writer.WriteLine($"{report.MinYear}, {report.YearCount}");
            foreach (var product in report.Products)
            {
                var name = product.Name.Contains(',') ? $@"""{product.Name}""" : product.Name;
                writer.WriteLine($"{name}, {string.Join(", ", product.Values)}");
            }
        }

        private decimal[] ProcessYear(Dictionary<int, decimal> values, int minYear, int maxYear)
        {
            var res = EmptyYear(minYear, maxYear);

            decimal sum = 0;
            int index = 0;
            for (int year = minYear; year <= maxYear; year++)
            {
                values.TryGetValue(year, out decimal value);
                sum += value;
                res[index++] = sum;
            }

            return res;
        }

        private decimal[] EmptyYear(int minYear, int maxYear)
        {
            int count = maxYear - minYear + 1;
            return new decimal[count];
        }

        private decimal[] ProcessProduct(IEnumerable<Input> inputs, int minYear, int maxYear)
        {
            var byOriginYear = inputs
                .GroupBy(p => p.OriginYear)
                .Select(p => new { year = p.Key, data = ProcessYear(p.ToDictionary(t => t.DevelopmentYear, t => t.IncrementalValue), p.Key, maxYear) })
                .ToDictionary(p => p.year, p => p.data);

            return Enumerable.Range(minYear, maxYear - minYear + 1)
                .SelectMany(year => byOriginYear.ContainsKey(year) ? byOriginYear[year] : EmptyYear(year, maxYear))
                .ToArray();
        }

        public Report Accumulate(List<Input> inputs)
        {
            int minYear = inputs.Min(p => p.OriginYear);
            int maxYear = inputs.Max(p => p.OriginYear);

            return new Report()
            {
                MinYear = minYear,
                YearCount = maxYear - minYear + 1,
                Products = inputs
                    .GroupBy(p => p.Product)
                    .Select(p => new Product()
                    {
                        Name = p.Key,
                        Values = ProcessProduct(p, minYear, maxYear)
                    })
                    .ToArray()
            };
        }
    }
}

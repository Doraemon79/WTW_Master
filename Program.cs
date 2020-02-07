using System;
using System.IO;
using Newtonsoft.Json;
using WTW_Task.Helper;
using WTW_Task.Models;

namespace WTW_Task
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: Wtw <input> <output>");
                    return;
                }

                var json = File.ReadAllText("appsettings.json");
                var applicationSettings = JsonConvert.DeserializeObject<ApplicationSettings>(json);
                
                TriangleHelper triangleHelper = new TriangleHelper(applicationSettings);
                var inputs = triangleHelper.LoadFromFile(args[0]);
                var report = triangleHelper.Accumulate(inputs);
                triangleHelper.WriteToFile(args[1], report);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
    }
}

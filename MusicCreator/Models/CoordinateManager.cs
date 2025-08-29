using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace MusicCreator.Models
{
    public class CoordinateManager
    {
        public string filePath = GetDataFolderPath() + "\\Notelocations.txt";

        public CoordinateManager()
        {

        }
        public CoordinateManager(string path)
        {
            filePath = path;
        }

        // Save coordinates to JSON
        public void SaveCoordinates(List<Point> points)
        {
            var json = JsonSerializer.Serialize(points, new JsonSerializerOptions
            {
                WriteIndented = true // Makes it readable
            });
            File.WriteAllText(filePath, json);
        }

        // Load coordinates from JSON
        public List<Point> LoadCoordinates()
        {
            if (!File.Exists(filePath)) return new List<Point>();

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<Point>>(json) ?? new List<Point>();
        }
        public static string GetDataFolderPath(string foldername = "Coorinates")
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.FullName ?? currentDir;
            var dataPath = Path.Combine(projectRoot, foldername); // Replace "Data" with your folder name

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            return dataPath;
        }
    }
}

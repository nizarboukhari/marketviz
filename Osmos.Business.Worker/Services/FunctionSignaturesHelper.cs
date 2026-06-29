using Microsoft.Extensions.Configuration;
using Osmos.Business.Worker.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osmos.Business.Worker.Services
{
    public static class FunctionSignaturesHelper
    {
        private static string _folderName = Settings.ResourcesFolder;
        private static string _tableName = "func_signs_table.csv";
        private static string _notFoundName = "not_found.txt";

        private static Dictionary<string, string> _table = new Dictionary<string, string>();
        private static List<string> _notFound = new List<string>();

        static FunctionSignaturesHelper()
        {
            ReadTable();
            ReadNotFound();
        }

        private static string CheckNotFoundFolderAndFile()
        {
            if (!Directory.Exists(_folderName)) Directory.CreateDirectory(_folderName);

            string filePath = Path.Combine(_folderName, _notFoundName);

            if (!File.Exists(filePath)) File.Create(filePath);

            return filePath;
        }

        private static void ReadNotFound()
        {
            string filePath = CheckNotFoundFolderAndFile();

            string[] lines = File.ReadAllLines(filePath);

            _notFound.AddRange(lines);
        }

        public static void WriteOneInNotFound(string functionSignature)
        {

            if (IsNotFound(functionSignature)) return;

            _notFound.Add(functionSignature);

            var lines = new string[] {
                $"{functionSignature}"
            };

            string filePath = CheckNotFoundFolderAndFile();
            File.AppendAllLines(filePath, lines);
        }

        public static bool IsNotFound(string functionSignature)
        {
            return _notFound.Contains(functionSignature);
        }

        private static string CheckTableFolderAndFile()
        {
            if (!Directory.Exists(_folderName)) Directory.CreateDirectory(_folderName);

            string filePath = Path.Combine(_folderName, _tableName);

            if (!File.Exists(filePath)) File.Create(filePath);

            return filePath;
        }

        private static void ReadTable()
        {
            string filePath = CheckTableFolderAndFile();

            string[] lines = File.ReadAllLines(filePath);

            _table = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (line.Split(',').Length < 2) continue;

                _table.Add(line.Split(',')[0], line.Split(',')[1]);
            }
        }

        public static string GetFunctionName(string signature)
        {
            if (_table.ContainsKey(signature)) return _table[signature];

            return null;
        }

        public static void WriteOneInTable(string signature, string name)
        {

            if (_table.ContainsKey(signature)) return;

            _table.Add(signature, name);

            var lines = new string[] {
                $"{signature},{name}"
            };

            string filePath = CheckTableFolderAndFile();
            File.AppendAllLines(filePath, lines);
        }

        public static void WriteManyInTable(Dictionary<string, string> keyValues)
        {
            var lines = new List<string>();

            foreach (var item in keyValues)
            {
                if (_table.ContainsKey(item.Key)) continue;

                _table.Add(item.Key, item.Value);

                lines.Add($"{item.Key},{item.Value}");
            }

            if (lines.Any())
            {
                string filePath = CheckTableFolderAndFile();
                File.AppendAllLines(filePath, lines);
            }
        }
    }
}

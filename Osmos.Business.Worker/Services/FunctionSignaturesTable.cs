using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Services
{
    [Obsolete]
    public class FunctionSignaturesTable
    {
        public async Task<Dictionary<string, string>> ReadFileAsync()
        {
            string filePath = CheckFolderAndFile();

            string[] lines = await File.ReadAllLinesAsync(filePath);

            _table = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (line.Split(',').Length < 2) continue;

                _table.Add(line.Split(',')[0], line.Split(',')[1]);
            }

            return _table;
        }

        public async Task<string> GetNameAsync(string signature) {
            
            await ReadFileAsync();

            if (_table.ContainsKey(signature)) return _table[signature];
            
            return null;
        }

        public async Task<bool> HasSignatureAsync(string signature) {

            await ReadFileAsync();

            return _table.ContainsKey(signature);
        }

        public async Task WriteOneAsync(string signature, string name) {

            if (await HasSignatureAsync(signature)) return;

            _table.Add(signature, name);

            var lines = new string[] { 
                $"{signature},{name}"
            };

            string filePath = CheckFolderAndFile();
            await File.AppendAllLinesAsync(filePath, lines);
        }

        public async Task WriteManyAsync(Dictionary<string, string> keyValues) {

            await ReadFileAsync();

            var lines = new List<string>();

            foreach (var item in keyValues)
            {
                if (_table.ContainsKey(item.Key)) continue;

                _table.Add(item.Key, item.Value);

                lines.Add($"{item.Key},{item.Value}");
            }

            if (lines.Any()) {
                string filePath = CheckFolderAndFile();
                await File.AppendAllLinesAsync(filePath, lines);
            }
        }

        private string CheckFolderAndFile() { 
            if (!Directory.Exists(_folderName)) Directory.CreateDirectory(_folderName);

            string filePath = Path.Combine(_folderName, _fileName);

            if (!File.Exists(filePath)) File.Create(filePath);

            return filePath;
        }

        private string _folderName = @"C:\Users\dafri\Desktop\mrkt";
        private string _fileName = "func_signs_table.csv";

        private Dictionary<string, string> _table = new Dictionary<string, string>();
        public Dictionary<string, string> Table
        {
            get
            {
                return _table;
            }
        }
    }
}

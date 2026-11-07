using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ForestGuild.Core.Data
{
    /// <summary>
    /// Читает CSV: разделитель ';', UTF-8, '#' — комментарий, первая непустая строка — заголовок.
    /// Значения не обрезаются до чисел: типы проверяет загрузчик.
    /// </summary>
    public static class CsvReader
    {
        public static List<Dictionary<string, string>> Read(string path)
        {
            if (!File.Exists(path)) throw new DataException(Path.GetFileName(path), "файл не найден");

            var rows = new List<Dictionary<string, string>>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            string[] header = null;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var cells = line.Split(';');
                if (header == null)
                {
                    header = new string[cells.Length];
                    for (var c = 0; c < cells.Length; c++) header[c] = cells[c].Trim();
                    continue;
                }

                if (cells.Length != header.Length)
                {
                    throw new DataException(Path.GetFileName(path) + ":" + (i + 1),
                        "колонок " + cells.Length + ", ожидалось " + header.Length);
                }

                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var c = 0; c < header.Length; c++) row[header[c]] = cells[c].Trim();
                rows.Add(row);
            }

            if (header == null) throw new DataException(Path.GetFileName(path), "нет заголовка");
            return rows;
        }
    }
}

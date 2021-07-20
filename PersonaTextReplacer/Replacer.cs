using PersonaTextReplacer.Properties;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static PersonaTextReplacer.MainWindow;

namespace PersonaTextReplacer
{
    public static class Replacer
    {
        public static int Replace(Dictionary<string, string> words, string input)
        {
            var editedFiles = 0;
            string msg, bmd;
            foreach (var file in Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLower();
                switch (ext)
                {
                    case ".pm1":
                        PM1CMD(file);
                        Globals.logger.WriteLine($"Decompiled MSG from {file}", LoggerType.Info);
                        msg = ChangeExtension(file, ".msg");
                        if (EditMsg(msg, words))
                        {
                            editedFiles++;
                            Globals.logger.WriteLine($"Edited {msg}", LoggerType.Info);
                            PM1CMD(msg);
                            Globals.logger.WriteLine($"Recompiled PM1 from {msg}", LoggerType.Info);
                        }
                        else
                        {
                            Globals.logger.WriteLine($"No words found in {file}, deleting...", LoggerType.Info);
                            File.Delete(file);
                        }
                        File.Delete(msg);
                        break;
                    case ".bf":
                        PEExport(file);
                        Globals.logger.WriteLine($"Extracted bmd from {file}", LoggerType.Info);
                        bmd = ChangeExtension(file, ".BMD");
                        if (File.Exists(bmd))
                        {
                            ASCDecompile(bmd);
                            Globals.logger.WriteLine($"Decompiled MSG from {bmd}", LoggerType.Info);
                            msg = $"{bmd}.msg";
                            if (EditMsg(msg, words))
                            {
                                editedFiles++;
                                Globals.logger.WriteLine($"Edited {msg}", LoggerType.Info);
                                ASCCompile(msg);
                                Globals.logger.WriteLine($"Recompiled {msg}", LoggerType.Info);
                                PEImport(file);
                                DeleteExtraPEFiles(file);
                                break;
                            }
                        }
                        Globals.logger.WriteLine($"No words found in {file}, deleting...", LoggerType.Info);
                        DeleteExtraPEFiles(file);
                        File.Delete(file);
                        break;
                    case ".bmd":
                        ASCDecompile(file);
                        Globals.logger.WriteLine($"Decompiled MSG from {file}", LoggerType.Info);
                        msg = $"{file}.msg";
                        if (EditMsg(msg, words))
                        {
                            editedFiles++;
                            Globals.logger.WriteLine($"Edited {msg}", LoggerType.Info);
                            ASCCompile(msg);
                            Globals.logger.WriteLine($"Recompiled {msg}", LoggerType.Info);
                        }
                        else
                        {
                            Globals.logger.WriteLine($"No words found in {file}, deleting...", LoggerType.Info);
                            File.Delete(file);
                        }
                        File.Delete(msg);
                        break;
                    // Unpack archives and recurse to edit text files within
                    case ".bin":
                    case ".arc":
                    case ".pac":
                    case ".pak":
                        if (!Path.GetFileName(file).StartsWith("EXPORTED_"))
                        {
                            var tempFolder = $"{Path.GetDirectoryName(file)}{Globals.s}EXPORTED_{Path.GetFileNameWithoutExtension(file)}";
                            var tempFile = $"{tempFolder}{Globals.s}EXPORTED_{Path.GetFileName(file)}";
                            Directory.CreateDirectory(tempFolder);
                            File.Copy(file, tempFile, true);
                            PEExport(tempFile);
                            Globals.logger.WriteLine($"Unpacked {file}", LoggerType.Info);
                            var FilesReplaced = Replace(words, tempFolder);
                            if (FilesReplaced > 0)
                            {
                                editedFiles += FilesReplaced;
                                PEImport(tempFile);
                                Globals.logger.WriteLine($"Repacked {file}", LoggerType.Info);
                                File.Copy(tempFile, file, true);
                            }
                            else
                            {
                                Globals.logger.WriteLine($"No words found in {file}, deleting...", LoggerType.Info);
                                File.Delete(file);
                            }
                            Directory.Delete(tempFolder, true);
                        }
                        break;
                    default:
                        Globals.logger.WriteLine($"Unknown extension {ext}, deleting {file}...", LoggerType.Info);
                        File.Delete(file);
                        break;
                }
            }
            return editedFiles;
        }
        public static void DeleteExtraPEFiles(string file)
        {
            foreach (var extra in Directory.GetFiles(Path.GetDirectoryName(file), "*", SearchOption.TopDirectoryOnly)
                .Where(f => f.StartsWith(ChangeExtension(file, String.Empty)) && (f.EndsWith(".msg") || f.EndsWith(".DAT") || f.EndsWith(".BMD"))))
            {
                File.Delete(extra);
            }
        }
        public static string ChangeExtension(string file, string newExtension)
        {
            var parent = Directory.GetParent(file);
            var filename = $"{Path.GetFileNameWithoutExtension(file)}{newExtension}";
            return $"{parent}{Globals.s}{filename}";
        }
        public static void ASCDecompile(string file)
        {
            string gameArgs = "";
            switch ((Game)Settings.Default.Game)
            {
                case Game.Persona4Golden:
                    gameArgs = "-Library p4g -Encoding P4";
                    break;
                case Game.Persona5:
                    gameArgs = "-Library P5 -Encoding P5";
                    break;
            }
            ProcessStartInfo StartInfo = new ProcessStartInfo();
            StartInfo.CreateNoWindow = true;
            StartInfo.UseShellExecute = false;
            StartInfo.FileName = Globals.asc;
            StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            StartInfo.Arguments = $@"""{file}"" -Decompile {gameArgs}";
            using (Process process = new Process())
            {
                process.StartInfo = StartInfo;
                process.Start();
                process.WaitForExit();
            }
        }
        public static void ASCCompile(string file)
        {
            string gameArgs = "";
            switch ((Game)Settings.Default.Game)
            {
                case Game.Persona4Golden:
                    gameArgs = "-OutFormat V1 -Library p4g -Encoding P4";
                    break;
                case Game.Persona5:
                    gameArgs = "-OutFormat V1BE -Library P5 -Encoding P5";
                    break;
            }
            ProcessStartInfo StartInfo = new ProcessStartInfo();
            StartInfo.CreateNoWindow = true;
            StartInfo.UseShellExecute = false;
            StartInfo.FileName = Globals.asc;
            StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            StartInfo.Arguments = $@"""{file}"" -Out ""{ChangeExtension(file, String.Empty)}"" -Compile {gameArgs}";
            using (Process process = new Process())
            {
                process.StartInfo = StartInfo;
                process.Start();
                process.WaitForExit();
            }
            File.Delete(file);
        }
        public static void PM1CMD(string file)
        {
            ProcessStartInfo StartInfo = new ProcessStartInfo();
            StartInfo.CreateNoWindow = true;
            StartInfo.UseShellExecute = false;
            StartInfo.FileName = Globals.pmse;
            StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            StartInfo.Arguments = $@"""{file}"" p4";
            using (Process process = new Process())
            {
                process.StartInfo = StartInfo;
                process.Start();
                process.WaitForExit();
            }
        }

        public static void PEExport(string file)
        {
            ProcessStartInfo StartInfo = new ProcessStartInfo();
            StartInfo.CreateNoWindow = true;
            StartInfo.UseShellExecute = false;
            StartInfo.FileName = Globals.pe;
            StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            StartInfo.Arguments = $@"""{file}"" -expall";
            using (Process process = new Process())
            {
                process.StartInfo = StartInfo;
                process.Start();
                process.WaitForExit();
            }
        }
        public static void PEImport(string file)
        {
            ProcessStartInfo StartInfo = new ProcessStartInfo();
            StartInfo.CreateNoWindow = true;
            StartInfo.UseShellExecute = false;
            StartInfo.FileName = Globals.pe;
            StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            StartInfo.Arguments = $@"""{file}"" -impall -save ""{file}""";
            using (Process process = new Process())
            {
                process.StartInfo = StartInfo;
                process.Start();
                process.WaitForExit();
            }
        }

        public static bool EditMsg(string file, Dictionary<string, string> words)
        {
            string[] lines = new string[0];
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (Exception e)
            {
                Globals.logger.WriteLine(e.Message, LoggerType.Error);
            }
            var replacedLines = 0;
            var editedLines = new List<String>();
            foreach (var line in lines)
            {
                var newLine = line;
                var nameMatch = Regex.Match(line, @"\[[A-Z][a-z\-\' ]*\]");
                var options = Settings.Default.Case ? RegexOptions.None : RegexOptions.IgnoreCase;
                foreach (var key in words.Keys)
                {
                    var word = Settings.Default.WholeWord ? $"\b{key}\b" : key;
                    // Line with name (Replace only name and not index names)
                    if (nameMatch.Success)
                    {
                        var name = newLine[nameMatch.Index..(nameMatch.Index + nameMatch.Length)];
                        var newName = Regex.Replace(name, word, words[key], options);
                        newLine = newLine.Replace(name, newName);
                    }
                    // Normal Line (Replace entire line)
                    else
                    {
                        // Apply regex to everything not surrounded by brackets
                        var split = Regex.Split(newLine, @"(\[[A-Za-z0-9_ -]+\])");
                        newLine = "";
                        foreach (var s in split)
                        {
                            if ((Game)Settings.Default.Game == Game.Persona5 &&
                                (s.Equals("[s]", StringComparison.InvariantCultureIgnoreCase) || s.Equals("[f 2 1]", StringComparison.InvariantCultureIgnoreCase)))
                                newLine += $"[f 0 5 -258]{s}";
                            else if (s.StartsWith("[") && s.EndsWith("]"))
                                newLine += s;
                            else
                                newLine += Regex.Replace(s, word, words[key], options);
                        }
                    }
                }
                if (line != newLine)
                {
                    replacedLines++;
                    Globals.logger.WriteLine($@"{Environment.NewLine}BEFORE{Environment.NewLine}{line}{Environment.NewLine}AFTER{Environment.NewLine}{newLine}", LoggerType.Info);
                }
                editedLines.Add(newLine);
            }
            File.WriteAllLines(file, editedLines);
            return replacedLines > 0;
        }
    }
}

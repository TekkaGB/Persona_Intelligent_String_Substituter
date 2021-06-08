using Microsoft.WindowsAPICodePack.Dialogs;
using PersonaTextReplacer.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PersonaTextReplacer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum Game
        {
            Persona4Golden,
            Persona5
        }
        public MainWindow()
        {
            InitializeComponent();
            Globals.logger = new Logger(Console);
            GameBox.SelectedIndex = Settings.Default.Game;
            CaseCheckbox.IsChecked = Settings.Default.Case;
            WholeWordCheckbox.IsChecked = Settings.Default.WholeWord;
            if (!String.IsNullOrEmpty(Settings.Default.InputPath))
                InputPathBox.Text = Settings.Default.InputPath;
            if (!String.IsNullOrEmpty(Settings.Default.OutputPath))
                OutputPathBox.Text = Settings.Default.OutputPath;
            Globals.logger.WriteLine("Welcome to PISS!", LoggerType.Info);
            if (!File.Exists(Globals.asc) || !File.Exists(Globals.pe) || !File.Exists(Globals.pmse))
                Globals.logger.WriteLine("Some dependency files are missing!", LoggerType.Error);
        }
        private void SetInputPath(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = (String.IsNullOrEmpty(Settings.Default.InputPath) || !Directory.Exists(Settings.Default.InputPath)) ?
                Globals.AssemblyLocation : Settings.Default.InputPath;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Settings.Default.InputPath = dialog.FileName;
                Settings.Default.Save();
                InputPathBox.Text = Settings.Default.InputPath;
                Globals.logger.WriteLine($"Input Path set to {Settings.Default.InputPath}", LoggerType.Info);
            }
        }
        private void SetOutputPath(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = (String.IsNullOrEmpty(Settings.Default.OutputPath) || !Directory.Exists(Settings.Default.OutputPath)) ?
                Globals.AssemblyLocation : Settings.Default.OutputPath;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Settings.Default.OutputPath = dialog.FileName;
                Settings.Default.Save();
                OutputPathBox.Text = Settings.Default.OutputPath;
                Globals.logger.WriteLine($"Output Path set to {Settings.Default.OutputPath}", LoggerType.Info);
            }
        }
        private async void Replace(object sender, RoutedEventArgs e)
        {
            // Check if we have everything needed
            if (!File.Exists(Globals.asc) || !File.Exists(Globals.pe) || !File.Exists(Globals.pmse))
            {
                Globals.logger.WriteLine("Some dependency files are missing!", LoggerType.Error);
                return;
            }
            if (String.IsNullOrEmpty(Settings.Default.InputPath) || !Directory.Exists(Settings.Default.InputPath))
            {
                Globals.logger.WriteLine("Please select valid input path first", LoggerType.Error);
                return;
            }
            if (String.IsNullOrEmpty(Settings.Default.OutputPath) || !Directory.Exists(Settings.Default.OutputPath))
            {
                Globals.logger.WriteLine("Please select valid output path first", LoggerType.Error);
                return;
            }
            if (Input.Text.Length == 0 || Output.Text.Length == 0)
            {
                Globals.logger.WriteLine("No words inputted", LoggerType.Error);
                return;
            }
            ReplaceButton.IsEnabled = false;
            CaseCheckbox.IsEnabled = false;
            WholeWordCheckbox.IsEnabled = false;
            GameBox.IsEnabled = false;
            OutputButton.IsEnabled = false;
            InputButton.IsEnabled = false;
            // Get input and output and map to dictionary
            var WordsToReplace = ParseWords(Input.Text);
            var WordsToReplaceWith = ParseWords(Output.Text);
            if (WordsToReplace.Count != WordsToReplaceWith.Count)
            {
                Globals.logger.WriteLine("Not an equal amount of lines", LoggerType.Error);
                return;
            }
            Dictionary<String, String> dictionary = new();
            try
            {
                dictionary = WordsToReplace.Zip(WordsToReplaceWith, (k, v) => new { k, v })
                  .ToDictionary(x => x.k, x => x.v);
            }
            catch
            {
                Globals.logger.WriteLine("Remove duplicate words to replace and try again", LoggerType.Error);
                return;
            }
            foreach (var key in dictionary.Keys)
                Globals.logger.WriteLine($"Replacing {key} with {dictionary[key]}", LoggerType.Info);

            var count = 0;
            await Task.Run(() =>
            {
                if (Settings.Default.InputPath != Settings.Default.OutputPath)
                {
                    Globals.logger.WriteLine($"Copying over files from {Settings.Default.InputPath} to {Settings.Default.OutputPath}", LoggerType.Info);
                    CopyDirectory(Settings.Default.InputPath, Settings.Default.OutputPath);
                }
                count += Replacer.Replace(dictionary, Settings.Default.OutputPath);
                // Delete empty folders
                ProcessDirectory(Settings.Default.OutputPath);
                Globals.logger.WriteLine($"Finished replacing words in {count} files!", LoggerType.Info);
            });

            ReplaceButton.IsEnabled = true;
            CaseCheckbox.IsEnabled = true;
            WholeWordCheckbox.IsEnabled = true;
            GameBox.IsEnabled = true;
            OutputButton.IsEnabled = true;
            InputButton.IsEnabled = true;
        }
        private static void ProcessDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                ProcessDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        private static void CopyDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd(Globals.s, ' ');
            var targetPath = target.TrimEnd(Globals.s, ' ');
            var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    Globals.logger.WriteLine($"Copying {file} to {targetFile}", LoggerType.Info);
                    File.Copy(file, targetFile, true);
                }
            }
        }

        // Split string from text by newlines
        private List<String> ParseWords(string text)
        {
            return text.Split(Environment.NewLine).ToList();
        }

        private void ScrollToBottom(object sender, TextChangedEventArgs args)
        {
            Console.ScrollToEnd();
        }

        private void GameBox_Selected(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                Settings.Default.Game = GameBox.SelectedIndex;
                Settings.Default.Save();
            }
        }

        private void CaseCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.Case = (bool)CaseCheckbox.IsChecked;
            Settings.Default.Save();
        }
        private void WholeWordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.WholeWord = (bool)WholeWordCheckbox.IsChecked;
            Settings.Default.Save();
        }
    }
}

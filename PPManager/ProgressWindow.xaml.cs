using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using static PPManager.MainWindow;

namespace PPManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }
        public async void Patch(Mod[] mods)
        {
            Mod[] enabledMods = mods.Where(mod => mod.IsEnabled).ToArray();
            string tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_TEMP");

            if (Directory.Exists(tempFolderPath))
            {
                Directory.Delete(tempFolderPath, true);
            }
            Directory.CreateDirectory(tempFolderPath);

            CopyFilesTo(Settings.packagePath, tempFolderPath);

            for (int i = 0; i < enabledMods.Length; i++)
            {
                Mod mod = enabledMods[i];
                UpdateProgress(i + 1, enabledMods.Length);
                CopyFilesTo(mod.Path, tempFolderPath);
            }

            File.Delete(Path.Combine(Settings.partyFolder, "package.nw"));
            ZipFile.CreateFromDirectory(tempFolderPath, Path.Combine(Settings.partyFolder, "package.nw"));
            Directory.Delete(tempFolderPath, true);

            ProgressText.Visibility = Bar.Visibility = Visibility.Hidden;
        }

        private void UpdateProgress(int current, int total)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressText.Text = $"({current}/{total})";
                Bar.Value = (double)current / total * Bar.Maximum;
            });
        }

        private void CopyFilesTo(string sourceFolder, string destFolder)
        {
            string[] files = Directory.GetFiles(sourceFolder);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Bar.Maximum = files.Length;
                Bar.Value = 0;
            });

            foreach (string file in files)
            {
                string destinationFile = Path.Combine(destFolder, Path.GetFileName(file));
                if (File.Exists(destinationFile))
                {
                    File.Delete(destinationFile);
                }
                File.Copy(file, destinationFile);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Bar.Value++;
                });
            }
        }
    }
}
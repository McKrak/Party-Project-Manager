using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace PPManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public readonly string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "partyFolder.txt");
        protected override void OnStartup(StartupEventArgs e)
        {
            // TODO: link this to the main app to avoid errors with writing to json
            if (!File.Exists(settingsPath))
            {
                System.Windows.MessageBox.Show(
                "There is no directory set for Party Project. Please choose the folder nw.exe is located in.",
                "No directory set",
                MessageBoxButton.OK);
                Settings.partyFolder = GetPartyProjectFolder();
                using (FileStream fs = File.Create(settingsPath));
                File.WriteAllText(settingsPath, Settings.partyFolder);
            }
            else
            {
                Settings.partyFolder = File.ReadAllText(settingsPath);
            }
            if (!Directory.Exists(Settings.packagePath))
            {
                MessageBoxResult res = System.Windows.MessageBox.Show(
                "There is no default resource set for package.nw. Use the current one in the project?\nRemove ANY mods you currently have before setting it.",
                "No package.nw set",
                MessageBoxButton.YesNo);
                if (res.Equals("No"))
                { 
                    Environment.Exit(0);
                }
                UnpackToPackage(Path.Combine(Settings.partyFolder, "package.nw"));
            }
            
        }
        public static string GetPartyProjectFolder()
        {
            while (true)
            {
                using FolderBrowserDialog folderDialog = new();
                {
                    folderDialog.Description = "Select a folder";
                    folderDialog.ShowNewFolderButton = true;
                    if (folderDialog.ShowDialog() != DialogResult.OK)
                    {
                        Environment.Exit(0);
                    }

                    string selectedPath = folderDialog.SelectedPath;
                    bool packageExists = File.Exists(Path.Combine(selectedPath, "package.nw"));
                    bool executableExists = File.Exists(Path.Combine(selectedPath, "nw.exe"));
                    if (packageExists && executableExists)
                    {
                        return selectedPath;
                    }
                    System.Windows.MessageBox.Show(
                    $"package.nw: {(packageExists ? '✔' : '❌')}\nnw.exe: {(executableExists ? '✔' : '❌')}\nTests failed. Please choose the directory that contains Party Project.",
                    "Directory Tests",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                }
            }
        }
        public void UnpackToPackage(string zipPath)
        {
            if (!Directory.Exists(Settings.packagePath))
            { 
                Directory.CreateDirectory(Settings.packagePath);
            }
            ZipFile.ExtractToDirectory(zipPath, Settings.packagePath);
        }
    }

    public static class Settings
    {
        public static string partyFolder = "";
        public static string packagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "package");
        // TODO: start from here and implement save data
    }
}

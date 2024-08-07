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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PPManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Mod : INotifyPropertyChanged
        {
            public string? Name { get; set; }
            public string Assets { get; set; }
            public string Path { get; set; }
            private bool _isEnabled;
            public bool IsEnabled
            {
                get { return _isEnabled; }
                set
                {
                    if (_isEnabled == value) return;
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            RefreshMods();
        }
        private void RefreshMods(object sender, RoutedEventArgs e) => RefreshMods();
        private void RefreshMods()
        {
            string modPath = Path.Combine(Settings.partyFolder, "mods");
            if (!Directory.Exists(modPath))
            {
                Directory.CreateDirectory(modPath);
                System.Windows.MessageBox.Show(
                    $"Created new directory for mods in {modPath}.",
                    "Directory Created",
                    MessageBoxButton.OK);
            }
            string[] paths = Directory.GetDirectories(modPath);
            IEnumerable<Mod> source = paths.Select(path => new Mod
            {
                Name = Path.GetFileName(path),
                Path = path,
                Assets = Directory.GetFiles(path).Length.ToString(),
                // TODO: set this by storing enabled mods in settings
                IsEnabled = false
            });
            DataContext = this;
            ModsListView.ItemsSource = source.ToList();
            mods = new((IEnumerable<Mod>)ModsListView.ItemsSource);
        }
        List<Mod> mods;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            return;
        }
        private void Patch(object sender, RoutedEventArgs e)
        {
            Patch();
            System.Windows.MessageBox.Show(
            "Mods patched successfully.",
            "Success",
            MessageBoxButton.OK);
        }
        private void Patch()
        {
            Mod[] enabledMods = mods.Where(mod => mod.IsEnabled).ToArray();
            // TODO: place an async thread to show properly i dont feel like fixing this right now
            ProgressWindow tempWindow = new();
            tempWindow.Patch(enabledMods);
        }
        private void Patch_Run(object sender, RoutedEventArgs e)
        {
            Patch();
            Process.Start(Path.Combine(Settings.partyFolder, "nw.exe"));
            Environment.Exit(0);
        }
    }
}
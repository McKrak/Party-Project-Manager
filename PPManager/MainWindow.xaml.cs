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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
        public class Board
        {
            public string? Name { get; set; }
            public string? RoomName {  get; set; }
        }
        public class Map
        {
            public string? Name { get; set; }
        }

        List<Mod> mods;
        List<Board> boards = new List<Board>();
        private ObservableCollection<Map> _maps = new ObservableCollection<Map>();
        public ObservableCollection<Map> maps
        {
            get { return _maps; }
            set
            {
                _maps = value;
                OnPropertyChanged(nameof(maps));
            }
        }

        private Board? _selectedBoard;
        public Board? SelectedBoard
        {
            get { return _selectedBoard; }
            set
            {
                _selectedBoard = value;
                OnPropertyChanged(nameof(SelectedBoard));
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            RefreshMods();
            PopulateBoards();
            PopulateMaps();
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

        private void PopulateBoards()
        {
            string dataJSPath = Path.Combine(Settings.packagePath, "data.js");
            string dataJSData = File.ReadAllText(dataJSPath);
            var dataJS = JsonDocument.Parse(dataJSData);

            JsonElement project = dataJS.RootElement.GetProperty("project");

            JsonElement boardsListSource = project[6][16][1][77][6];

            for (int i = 1; i < boardsListSource.GetArrayLength() -1; i++)
            {
                boards.Add(new Board
                {
                    Name = boardsListSource[i][5][1][1][1][1].GetString(),
                    RoomName = boardsListSource[i][5][1][2][1][1].GetString()
                });

            }
            DataContext = this;
            BoardListView.ItemsSource = boards;
        }

        private void BoardListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (BoardListView.SelectedItem != null)
            {
                SelectedBoard = (Board)BoardListView.SelectedItem;
                Console.WriteLine(SelectedBoard.Name);
            }
        }

        private void PopulateMaps()
        {
            string dataJSPath = Path.Combine(Settings.packagePath, "data.js");
            string dataJSData = File.ReadAllText(dataJSPath);
            var dataJS = JsonDocument.Parse(dataJSData);

            JsonElement project = dataJS.RootElement.GetProperty("project");

            JsonElement mapsListSource = project[5];
            maps.Clear();

            for (int i = 0; i < mapsListSource.GetArrayLength(); i++)
            {
                if (mapsListSource[i][4].GetString() == "cd_board")
                {
                    maps.Add(new Map
                    {
                        Name = mapsListSource[i][0].GetString()
                    });
                }
            }
            DataContext = this;
            MapListView.ItemsSource = maps;
        }


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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedBoard != null)
            {
                var comboBox = sender as System.Windows.Controls.ComboBox;
                // Update RoomName based on selected item or text input
                if (comboBox != null)
                {
                    SelectedBoard.RoomName = comboBox.Text;
                }
            }
        }
    }
}
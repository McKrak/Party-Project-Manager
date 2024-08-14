using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        private JObject? _dataJS;

        private JObject LoadDataJS()
        {
            if (_dataJS == null)
            {
                string dataJSPath = Path.Combine(Settings.packagePath, "data.js");
                string dataJSData = File.ReadAllText(dataJSPath);
                _dataJS = JObject.Parse(dataJSData);
            }
            return _dataJS;
        }

        private void ClearDataJS()
        {
            _dataJS = null;
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
            public string? RoomName { get; set; }
            public int ID { get; set; }
            public string? Data { get; set; }
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
            LoadDataJS();

            DataContext = this;

            RefreshMods();
            PopulateBoards();
            PopulateMaps();

            ClearDataJS();
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
            //Parse into Jobject
            JObject dataJS = LoadDataJS();
            JToken? project = dataJS["project"];

            JToken? boardsListSource = project?[6]?[16]?[1]?[77]?[6];

            if (boardsListSource != null && boardsListSource.Type == JTokenType.Array)
            {
                for (int i = 1; i < boardsListSource.Count() - 1; i++)
                {
                    JToken? board = boardsListSource[i];
                    boards.Add(new Board
                    {
                        Name = board?[5]?[1]?[1]?[1]?[1]?.ToString(),
                        RoomName = board?[5]?[1]?[2]?[1]?[1]?.ToString(),
                        ID = i
                    });
                }
            }

            DataContext = this;
            BoardListView.ItemsSource = boards;
        }

        private void SaveDataJS(object sender, RoutedEventArgs e)
        {
            string dataJSPath = Path.Combine(Settings.packagePath, "data.js");

            //Parse into Jobject
            JObject dataJS = LoadDataJS();
            JToken? project = dataJS["project"];

            JToken? boardsListSource = project?[6]?[16]?[1]?[77]?[6];

            if (boardsListSource != null && boardsListSource.Type == JTokenType.Array)
            {
                for (int i = 1; i < boardsListSource.Count() - 1; i++)
                {
                    JToken? board = boardsListSource[i];

                    board?[5]?[1]?[1]?[1]?[1]?.Replace(boards?[i-1].Name?.ToString());
                    board?[5]?[1]?[2]?[1]?[1]?.Replace(boards?[i - 1].RoomName?.ToString());
                }
            }

            File.WriteAllText(dataJSPath, dataJS.ToString());

            System.Windows.MessageBox.Show(
            "Mods patched successfully.",
            "Success",
            MessageBoxButton.OK);
        }

        private void BoardListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BoardListView.SelectedItem != null)
            {
                SelectedBoard = (Board)BoardListView.SelectedItem;
                Uri baseUri = new Uri(Settings.packagePath + "/");

                BoardImage.Source = new BitmapImage(new Uri(baseUri, "boardthumb-default-" + SelectedBoard.ID.ToString("D3") + ".jpg"));
                Console.WriteLine(SelectedBoard.Name);
            }
        }

        private void PopulateMaps()
        {
            JObject dataJS = LoadDataJS();

            JToken? project = dataJS["project"];

            JToken? mapsListSource = project?[5];
            maps.Clear();

            for (int i = 0; i < mapsListSource?.Count(); i++)
            {
                if (mapsListSource?[i]?[4]?.ToString() == "cd_board")
                {
                    maps.Add(new Map
                    {
                        Name = mapsListSource?[i]?[0]?.ToString()
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
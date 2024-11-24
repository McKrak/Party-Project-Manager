using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime;
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

        private JObject? _schemaJS;
        private JObject LoadSchema()
        {
            if ((_schemaJS == null) && (_dataJS != null)) {
                JToken? versionEntry = _dataJS?["project"]?[16];
                if (versionEntry != null)
                {
                    string version = versionEntry.ToString().Split('-')[0].Replace('.','_');
                    string schemaJSPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Resources/Schema/{version}.schema");
                    string schemaJSData = File.ReadAllText(schemaJSPath);
                    _schemaJS = JObject.Parse(schemaJSData);
                } 
                else
                {
                    System.Windows.MessageBox.Show("This Party Project installation " +
                        "is either corrupt, or unsupported as of right now.", "Error",
                        MessageBoxButton.OK);
                }
            }
            return _schemaJS;
        }

        private void ClearDataJS()
        {
            _dataJS = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
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

            public string? BGM { get; set; }
            public string? BGMPinch { get; set; }
            public string? BGMNight { get; set; }

            public bool? TypeClassic { get; set; }
            public bool? TypeDayNight { get; set; }
            public string? Desc { get; set; }

            public string? Data { get; set; }
        }
        public class Map
        {
            public string? Name { get; set; }
            public int? Index { get; set; }
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
        private ObservableCollection<string> _musicTracks = new ObservableCollection<string>();
        public ObservableCollection<string> musicTracks
        {
            get { return _musicTracks; }
            set
            {
                _musicTracks = value;
                OnPropertyChanged(nameof(musicTracks));
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

        private string? _selectedMusic;
        public string? SelectedMusic
        {
            get { return _selectedMusic; }
            set
            {
                _selectedMusic = value;
                OnPropertyChanged(nameof(SelectedMusic));
            }
        }

        private Map? _selectedMap;
        public Map? SelectedMap
        {
            get { return _selectedMap; }
            set
            {
                _selectedMap = value;
                OnPropertyChanged(nameof(SelectedMap));
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
            PopulateMusic();

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
            //Parse into Jobject and convert data to board objects
            JObject? dataJS = LoadDataJS();
            JObject? schemaJS = LoadSchema();

            string? boardsListDatapath = schemaJS?["datapathSchema"]?["menuBoardList"]?.ToString();
            if (boardsListDatapath != null)
            {
                JToken? boardsListSource = dataJS?.SelectToken(boardsListDatapath);
                if (boardsListSource != null && boardsListSource.Type == JTokenType.Array)
                {
                    for (int i = 1; i < boardsListSource.Count() - 1; i++)
                    {
                        JToken? board = boardsListSource[i];

                        JToken? gimmicks = board?[5]?[1]?[6]?[1]?[1];

                        bool isClassic = false;
                        bool isDayNight = false;

                        if (gimmicks != null && gimmicks.Type == JTokenType.Array)
                        {
                            if (gimmicks?[0]?.ToObject<int>() == 10)
                            {
                                for (int j = 1; j < gimmicks?.Count(); j++)
                                {
                                    if (gimmicks?[j]?[1]?.ToString() == "STG_TYPECLASSIC" && isClassic == false)
                                    {
                                        isClassic = true;
                                    }
                                    else if (gimmicks?[j]?[1]?.ToString() == "STG_DAYNIGHT")
                                    {
                                        isDayNight = true;
                                    }
                                }
                            }
                            else if (gimmicks?[0]?.ToObject<int>() == 23)
                            {
                                if (gimmicks?[1]?.ToString() == "STG_TYPECLASSIC" && isClassic == false)
                                {
                                    isClassic = true;
                                }
                                else if (gimmicks?[1]?.ToString() == "STG_DAYNIGHT")
                                {
                                    isDayNight = true;
                                }
                            }
                        }

                        boards.Add(new Board
                        {
                            Name = board?[5]?[1]?[1]?[1]?[1]?.ToString(),
                            RoomName = board?[5]?[1]?[2]?[1]?[1]?.ToString(),
                            BGM = board?[5]?[1]?[3]?[1]?[1]?.ToString(),
                            BGMPinch = board?[5]?[1]?[4]?[1]?[1]?.ToString(),
                            BGMNight = board?[5]?[1]?[5]?[1]?[1]?.ToString(),
                            Desc = board?[5]?[1]?[6]?[1]?[2]?[1]?.ToString(),
                            TypeClassic = isClassic,
                            TypeDayNight = isDayNight,
                            ID = i
                        });
                    }
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

            //First, we add the board entries for each board
            JToken? boardsListSource = project?[6]?[16]?[1]?[92]?[6];

            if (boardsListSource != null && boardsListSource.Type == JTokenType.Array)
            {
                for (int i = 1; i < boardsListSource.Count() - 1; i++)
                {
                    JToken? board = boardsListSource[i];
                    Board? source = boards?[i - 1];

                    board?[5]?[1]?[1]?[1]?[1]?.Replace(source?.Name?.ToString());
                    board?[5]?[1]?[2]?[1]?[1]?.Replace(source?.RoomName?.ToString());
                    board?[5]?[1]?[3]?[1]?[1]?.Replace(source?.BGM?.ToString());
                    board?[5]?[1]?[4]?[1]?[1]?.Replace(source?.BGMPinch?.ToString());
                    board?[5]?[1]?[5]?[1]?[1]?.Replace(source?.BGMNight?.ToString());
                    board?[5]?[1]?[6]?[1]?[2]?[1]?.Replace(source?.Desc?.ToString());
                    if (boards?[i - 1].TypeDayNight == true)
                    {
                        JArray gimmicks = new JArray(10, new JArray(23, (source?.TypeClassic == true) ? "STG_TYPECLASSIC" : "STG_TYPESPEC"), new JArray(23, "STG_DAYNIGHT"));
                        board?[5]?[1]?[6]?[1]?[1]?.Replace(gimmicks);
                    }
                    else
                    {
                        JArray gimmicks = new JArray(23, (source?.TypeClassic == true) ? "STG_TYPECLASSIC" : "STG_TYPESPEC");
                        board?[5]?[1]?[6]?[1]?[1]?.Replace(gimmicks);
                    }
                }
            }

            //Then, we configure the board list in the party menu so there's enough for each board
            JToken? boardSelectTextures = project?[3]?[493]?[7]?[0]?[7];
            JToken? boardSelectButtons = project?[5]?[222]?[6]?[1]?[14];
            int boardButtonCount = 0;

            //for (int i = 0; i < boardSelectButtons?.Count(); i++)
            //{
            //    if (boardSelectButtons?[i]?[1]?.ToObject<int>() == 493)
            //    {
            //        boardButtonCount++;
            //        if (boardButtonCount < boards.Count())
            //        {

            //        }
            //    }
            //}

            File.WriteAllText(dataJSPath, dataJS.ToString());

            System.Windows.MessageBox.Show(
            "Mods patched successfully.",
            "Success",
            MessageBoxButton.OK);
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
                        Name = mapsListSource?[i]?[0]?.ToString(),
                        Index = i
                    });
                }
            }
            DataContext = this;
            MapListView.ItemsSource = maps;
        }

        private void ExtractMap(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject dataJS = LoadDataJS();
                JToken? project = dataJS["project"];
                dynamic ppbJS = new JObject();

                JToken? mapsListSource = project?[5];
                JToken? map = mapsListSource?[SelectedMap.Index]?[6]?[1];
                JToken? spaceList = map?[14];

                JObject schemaJS = LoadSchema();
                JToken? schema = schemaJS["objectSchema"];
                Console.WriteLine(schema);

                for (int i = 0; i < spaceList?.Count(); i++)
                {
                    for (int j = 0; j < schema?.Count(); j++)
                    {
                        Console.WriteLine(spaceList?[i]?[1]);
                        Console.WriteLine(schema?[j]?[1]);
                        if (spaceList?[i]?[1]?.ToString() == (schema?[j]?[1]?.ToString()))
                        {
                            spaceList?[i]?[1]?.Replace(schema?[j]?[0]?.ToString());
                        }
                    }
                }
                ppbJS.spaceList = spaceList;

                List<string> BGList = new List<string> { };

                JToken? mapBGSource = mapsListSource?[SelectedMap.Index]?[6]?[0]?[14]?[0]?[1];
                int? mapBGID = mapBGSource?.ToObject<int>();
                if (mapBGID != null) {
                    JToken? mapBGList = project?[3]?[mapBGID]?[7]?[0]?[7];
                    if (mapBGList != null)
                    {
                        for (int i = 0; i <  mapBGList?.Count(); i++)
                        {
                            string imagePath = Path.Combine(Settings.packagePath, mapBGList?[i]?[0]?.ToObject<string>());
                            Console.WriteLine(imagePath);
                            if (File.Exists(imagePath))
                            {
                                Byte[] bytes = File.ReadAllBytes(imagePath);
                                string imB64 = Convert.ToBase64String(bytes);
                                Console.WriteLine(imB64);
                                BGList.Add(imB64);
                            }
                        }
                    }
                }
                ppbJS.BGList = JToken.FromObject(BGList);
                SaveToPPB(ppbJS);


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void SaveToPPB(JObject? _map)
        {
            using SaveFileDialog fileDialog = new SaveFileDialog();
            {
                fileDialog.InitialDirectory = Settings.partyFolder;
                fileDialog.Filter = ".Party Project Board (*.ppb)|*.ppb";
                fileDialog.FilterIndex = 0;
                fileDialog.FileName = SelectedMap?.Name;
                fileDialog.RestoreDirectory = true;

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    File.WriteAllText(fileDialog.FileName, _map?.ToString());

                }
            }
        }

        private void PopulateMusic()
        {
            foreach (string f in Directory.GetFiles(Settings.packagePath, "*.ogg"))
            {
                musicTracks.Add(Path.GetFileName(f));
            }
            DataContext = this;
            MusicListView.ItemsSource = musicTracks;
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
                if (comboBox != null)
                {
                    SelectedBoard.RoomName = comboBox.Text;
                }
            }
        }
        private void ComboBox_MusicSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedMusic != null)
            {
                var comboBox = sender as System.Windows.Controls.ComboBox;
                if (comboBox != null)
                {
                    SelectedMusic = comboBox.Text;
                }
            }
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
        private void MapListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MapListView.SelectedItem != null)
            {
                SelectedMap = (Map)MapListView.SelectedItem;
            }
        }

        private void MusicListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MusicListView.SelectedItem != null)
            {
                SelectedMusic = (string)MusicListView.SelectedItem;
            }
        }

        private void ReplaceBoardImage(object sender, RoutedEventArgs e)
        {

        }

        private void LoadExternalBoardData(object sender, RoutedEventArgs e)
        {
            using OpenFileDialog fileDialog = new OpenFileDialog();
            {
                fileDialog.InitialDirectory = Settings.partyFolder;
                fileDialog.Filter = ".json files (*.json)|*.json";
                fileDialog.FilterIndex = 0;
                fileDialog.Multiselect = true;
                fileDialog.RestoreDirectory = true;

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    for (int i = 0; i < fileDialog.FileNames.Length; i++)
                    {
                        Console.WriteLine(fileDialog.FileNames[i]);
                    }

                }

                //string selectedPath = fileDialog.SelectedPath;
                //bool jsonExists = File.Exists(Path.Combine(selectedPath, "package.nw"));
                //bool executableExists = File.Exists(Path.Combine(selectedPath, "nw.exe"));
                //if (packageExists && executableExists)
                //{
                //    return selectedPath;
                //}
                //System.Windows.MessageBox.Show(
                //$"package.nw: {(packageExists ? '✔' : '❌')}\nnw.exe: {(executableExists ? '✔' : '❌')}\nTests failed. Please choose the directory that contains Party Project.",
                //"Directory Tests",
                //MessageBoxButton.OK,
                //MessageBoxImage.Error);
            }
        }
    }
}
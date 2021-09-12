using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace ZipPickuper {
    /// <summary>
    /// FileListWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class FileListWindow : Window {
        FileListWindowViewModel vm;
        public FileListWindow(string zipPath) {
            InitializeComponent();

            vm = new FileListWindowViewModel(Convert.ToInt32(this.Width));
            vm.ZipPath = zipPath;
            this.DataContext = vm;
        }

        private void extractItem_Click(object sender, RoutedEventArgs e) {
            if (dataGrid1.SelectedItems.Count <= 0) return;

            var fsd = new FolderBrowserDialog();
            if (fsd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Common.ExtractSelectedItem(((EntryInfo)dataGrid1.SelectedItem).ZipPath, dataGrid1.SelectedItems.Cast<EntryInfo>().Select(e => e.FullName), fsd.SelectedPath);
            }
        }

        private void dataGrid1_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var rowIndex = -1;
            var dropP = e.GetPosition(dataGrid1);
            var hitRes = VisualTreeHelper.HitTest(dataGrid1, dropP);
            if (hitRes != null) {
                var obj = hitRes.VisualHit;
                while (obj != null) {
                    if (obj is DataGridRow) {
                        rowIndex = ((DataGridRow)obj).GetIndex();
                        break;
                    }
                    obj = VisualTreeHelper.GetParent(obj);
                }
            }
            if (rowIndex < 0) return;
            new ShowWindow(vm.Entries[rowIndex].GetBytes(), vm.Entries[rowIndex].Name.IsImageFile()).ShowDialog();
        }
    }
    public class FileListWindowViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        string _ZipPath = "";
        public string ZipPath {
            get { return _ZipPath; }
            set {
                _ZipPath = value;
                OnPropertyChanged(nameof(ZipPath));

                Entries.Clear();
                using (var zip = ZipFile.OpenRead(_ZipPath)) {
                    foreach (var entry in zip.Entries) {
                        Entries.Add(new EntryInfo(_ZipPath, entry));
                    }
                }
            }
        }

        public ObservableCollection<EntryInfo> Entries { get; set; } = new ObservableCollection<EntryInfo>();

        int _WindowHeight = Convert.ToInt32(SystemParameters.PrimaryScreenHeight) - 50;
        public int WindowHeight {
            get {
                return _WindowHeight;
            }
            set {
                _WindowHeight = value;
                OnPropertyChanged(nameof(WindowHeight));
            }
        }

        int _WindowLeft = 0;
        public int WindowLeft {
            get { return _WindowLeft; }
            set {
                _WindowLeft = value;
                OnPropertyChanged(nameof(WindowLeft));
            }
        }

        public FileListWindowViewModel(int windowWidth) {
            _WindowLeft = (Convert.ToInt32(SystemParameters.PrimaryScreenWidth) - windowWidth) / 2;
        }
    }

    public class EntryInfo {
        public string ZipPath { get; set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public BitmapImage Thumbnail { get; private set; }
        public EntryInfo(string zipPath, ZipArchiveEntry entry) {
            ZipPath = zipPath;
            Name = entry.Name;
            FullName = entry.FullName;
            try {
                if (Name.IsImageFile()) {
                    byte[] bs;
                    using (var s = entry.Open()) {
                        bs = s.ReadAll();
                    }
                    using (var ms = new MemoryStream(bs)) {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = ms;
                        bmp.DecodePixelHeight = 200;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        Thumbnail = bmp;
                    }
                }
            } catch (Exception ex) {
                //Windows10標準のZIP圧縮機能を使うとentry.Open()の後エラーになる（s.Lengthが取得できない）
                //Read自体は可能なのでCommonに読み込みメソッド追加（したけど、エラーになるとプロセスが残るのでキャッチしとく）
                Debug.Print(ex.Message);
            }
        }
        internal byte[] GetBytes() {
            using (var zip = ZipFile.OpenRead(ZipPath)) {
                using (var s = zip.Entries.Where(e => e.FullName == FullName).FirstOrDefault().Open()) {
                    return s.ReadAll();
                }
            }
        }
    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZipPickuper {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        ViewModel vm;
        public MainWindow() {
            InitializeComponent();

            vm = new ViewModel();
            this.DataContext = vm;
        }

        private void openBtn_Click(object sender, RoutedEventArgs e) {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Zip file|*.zip";
            if (ofd.ShowDialog() == true) {
                vm.ZipPath = ofd.FileName;
                ShowFileList();
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            var fs = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var f in fs) {
                if (f.ToLower().EndsWith(".zip")) {
                    vm.ZipPath = f;
                    ShowFileList();
                }
            }
            e.Handled = true;
        }

        private void ShowFileList() {
            new FileListWindow(vm.ZipPath).Show();
        }
    }
    public class ViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        string _ZipPath = "";
        public string ZipPath {
            get { return _ZipPath; }
            set {
                _ZipPath = value;
                OnPropertyChanged(nameof(ZipPath));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
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

namespace ZipPickuper {
    /// <summary>
    /// ShowWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ShowWindow : Window {
        public ShowWindow(byte[] bs, bool isImage = true) {
            InitializeComponent();

            if (isImage) {
                grid1Height.Height = new GridLength(0);
                var bmp = new BitmapImage();
                using (var ms = new MemoryStream(bs)) {
                    bmp.BeginInit();
                    bmp.StreamSource = ms;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                }

                image1.Source = bmp;
            } else {
                var enc = bs.Take(1024).ToArray().GetEncoding();
                if (enc != null) {
                    grid2Height.Height = new GridLength(0);
                    textBox1.Text = enc.GetString(bs);
                } else {
                    MessageBox.Show("This file is not image and can't detect encoding.");
                }
            }
        }

        #region MouseEvent
        private void image1_MouseWheel(object sender, MouseWheelEventArgs e) {
            const double scale = 1.1;

            var matrix = image1.RenderTransform.Value;
            if (e.Delta > 0) matrix.ScaleAt(scale, scale, e.GetPosition(this).X, e.GetPosition(this).Y);
            else matrix.ScaleAt(1.0 / scale, 1.0 / scale, e.GetPosition(this).X, e.GetPosition(this).Y);
            image1.RenderTransform = new MatrixTransform(matrix);
        }
        bool down = false;
        Point downPos;
        private void image1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            down = true;
            downPos = e.GetPosition(canvas1);
        }

        private void image1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            down = false;
        }

        private void image1_MouseMove(object sender, MouseEventArgs e) {
            if (down) {
                var p = e.GetPosition(canvas1);
                var matrix = image1.RenderTransform.Value;
                matrix.OffsetX += p.X - downPos.X;
                matrix.OffsetY += p.Y - downPos.Y;
                image1.RenderTransform = new MatrixTransform(matrix);
                downPos = p;
            }
        }

        private void image1_MouseLeave(object sender, MouseEventArgs e) {
            down = false;
        }
        #endregion

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape)
                Close();
        }

        private void closeItem_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void closeItem2_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}

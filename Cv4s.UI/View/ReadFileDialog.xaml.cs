using Cv4s.Common.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

namespace Cv4s.UI.View
{
    /// <summary>
    /// Interaction logic for ReadFileDialog.xaml
    /// </summary>
    public partial class ReadFileDialog : Window
    {
        public string[] SelectedFiles { get; set; }

        public ScanFileFormat SelectedFormat { get; set; }
        public double XRes { get; set; }
        public double YRes { get; set; }
        public double ZRes { get; set; }

        public ReadFileDialog()
        {
            InitializeComponent();
            ResizeMode = ResizeMode.NoResize;
            DataContext = this;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResPanel.Visibility = (SelectedFormat == ScanFileFormat.PNG ? Visibility.Visible : Visibility.Hidden);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var filter = "";
            switch (SelectedFormat)
            {
                case ScanFileFormat.DICOM:
                    filter = "DICOM files (.dcm)|*.dcm";
                    break;
                case ScanFileFormat.PNG:
                    filter = "PNG image files (.png)|*.png";
                    break;
            }
            dialog.Filter = filter;
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == true)
            {
                DialogResult = true;
                SelectedFiles = dialog.FileNames;
                Close();
            }
        }
    }
}

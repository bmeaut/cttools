using Core.Services;
using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ViewModel;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace View
{
    /// <summary>
    /// Interaction logic for MeasurementEditor.xaml
    /// </summary>
    public partial class MeasurementEditor : RibbonWindow
    {
        private readonly MeasurementEditorViewModel _viewModel;

        public MeasurementEditor(MeasurementEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Canvas.MeasurementEditorViewModel = viewModel;
            _viewModel.RefreshBackgroundAction =
                Canvas.SetInkCanvasSize;
            Canvas.SetInkCanvasSize(_viewModel.Background.Width,
                _viewModel.Background.Height);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs args)
        {

        }

        public void SetMeasurement(MeasurementDto measurement)
        {

        }

        private async void RibbonButton_Undo(object sender, RoutedEventArgs e)
        {
            _viewModel.UndoStep();
        }

        private async void RibbonButton_Redo(object sender, RoutedEventArgs e)
        {
            _viewModel.RedoStep();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Window_Loaded();
        }

        private async void RunOperation_Click(object sender, RoutedEventArgs e)
        {
            // Note: if the operation is run this way, no further args
            //  are related to the call.
            await _viewModel.RunSelectedOperation(new OperationRunEventArgs());
        }

        private async void SaveSessionButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveSessionAsync();
        }

        private void OpenMeasurementsButton_Click(object sender, RoutedEventArgs e)
        {
            var application = (App)Application.Current;
            var window = application.ServiceProvider.GetService<ResultsViewer>();
            window?.Show();
        }

        private async void OpenMeasurementCloningButton_Click(object sender, RoutedEventArgs e)
        {
            var application = (App)Application.Current;
            var window = application.ServiceProvider.GetService<MeasurementOperationCloningWindow>();
            bool? result = window.ShowDialog();
            if (result == null) return;
            if (result == true)
                await _viewModel.CloneMeasurementOperations();
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OperationSequenceListView.SelectedItems.Clear();

            ListViewItem item = sender as ListViewItem;
            if (item != null)
            {
                item.IsSelected = true;
                OperationSequenceListView.SelectedItem = item;
            }
        }

        private void ListViewItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                int index = OperationSequenceListView.SelectedIndex;
                _viewModel.OperationSelectedFromSequence(index);
            }
        }

        private void DeleteOperationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveOperationFromSequence(OperationSequenceListView.SelectedIndex);
        }

        private async void RunOperationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.RunSequence(OperationSequenceListView.SelectedIndex);
        }

        private void AddToSequence_Click(object sender, RoutedEventArgs e) => _viewModel.AddSelectedOperationToSequence();

        private void CancelOperationButton_Click(object sender, RoutedEventArgs e) => _viewModel.CancelCurrentOperation();

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            string text = " Change layer: Mousewheel\n" +
                          " Zoom in/out: CTRL + Mousewheel\n" +
                          " Appearances: 0-5\n" +
                          " Hide actual: H\n";
            MessageBox.Show(text);
        }

        private void OptionsRibbonMI_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This function is not implemented in this version");
        }

        private void ExitRibbonMI_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PropertyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            _viewModel.OperationSettingsChanged();
        }
    }
}

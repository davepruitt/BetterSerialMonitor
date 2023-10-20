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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BetterSerialMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Set the view-model
            DataContext = MainWindowViewModel.GetInstance();
        }

        private void RefreshPortsButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetInstance().RefreshAvailablePorts();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowViewModel.GetInstance().IsPortConnected)
            {
                MainWindowViewModel.GetInstance().Disconnect();
            }
            else
            {
                MainWindowViewModel.GetInstance().ConnectToDevice();
            }
        }

        private void SendImmediatelyButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetInstance().SendImmediateMessage(ImmediateText.Text);
            ImmediateText.Text = string.Empty;
        }

        private void AddToMessageButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetInstance().AddTextToMessage(ImmediateText.Text);
            ImmediateText.Text = string.Empty;
        }

        private void AddAsByteSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetInstance().AddBytesToMessage(ImmediateText.Text);
            ImmediateText.Text = string.Empty;
        }

        private void ClearMessageButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetInstance().ClearCurrentMessage();
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetInstance().SendCurrentMessage();
        }

        private void ClearBufferButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetInstance().ClearBuffer();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DataReceivedBuffer.ScrollToEnd();
        }

        private void DataTypeAddButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetInstance().AddAsDataType(ImmediateText.Text);
            ImmediateText.Text = string.Empty;
        }
    }
}

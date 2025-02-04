using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Project_FREAK.Views;  // Imports Views namespace and allows us to change the page displayed in the MainFrame.

namespace Project_FREAK
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new HomePage());     // Navigate to HomePage upon startup
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement settings page
            MessageBox.Show("Settings selected");
        }

        private void HomeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage());
        }

        private void RecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RecordPage());
        }

        private void ReplayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReplayPage());
        }
    }
}
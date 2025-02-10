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
using Project_FREAK.Views.Settings;  // Imports Settings namespace and allows us to change the page displayed in the SettingsFrame.

namespace Project_FREAK
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new HomePage());     // Navigate to HomePage upon startup
            UpdateMenuItems("Home");
        }


        private void UpdateMenuItems(string currentPage)
        {
            foreach (var subItem in NavigationMenu.Items)
            {
                if (subItem is MenuItem subMenuItem)
                {   // Enable the menu item if it is not the current page, and disable it if it is the current page
                    subMenuItem.IsEnabled = subMenuItem.Header.ToString() != currentPage;
                    subMenuItem.IsChecked = subMenuItem.Header.ToString() == currentPage;
                }
            }
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();  // Create a new instance of the SettingsWindow
            settingsWindow.Owner = this;    // Set the owner of the SettingsWindow to be the MainWindow
            settingsWindow.Show();
        }

        private void HomeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage());
            UpdateMenuItems("Home");
        }

        private void RecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RecordPage());
            UpdateMenuItems("Record");
        }

        private void ReplayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReplayPage());
            UpdateMenuItems("Replay");
        }
    }
}
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
using System.Windows.Threading;
using Project_FREAK.Views;  // Imports Views namespace and allows us to change the page displayed in the MainFrame.
using Project_FREAK.Views.Settings;  // Imports Settings namespace and allows us to change the page displayed in the SettingsFrame.

namespace Project_FREAK
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded; // Subscribe to the Loaded event
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new LoadingPage()); // Navigate to LoadingPage first (Static page to allow for async loading of the app)

            // Force UI to render LoadingPage
            await Task.Delay(10); // Small delay to allow rendering
            MainFrame.UpdateLayout();

            // Navigate to RecordPage without blocking
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    MainFrame.Navigate(new RecordPage());
                });
            });
        }

        public void NavigateToPage(Page page)
        {
            MainFrame.Navigate(page);
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this // Set the owner of the SettingsWindow to be the MainWindow
            };
            settingsWindow.Show();
        }

        private void RecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new RecordPage());
        }
    }
}
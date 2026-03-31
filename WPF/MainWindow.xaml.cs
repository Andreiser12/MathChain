using MathChain.WPF.ViewModels;
using MathChain.WPF.Views;
using System.Windows;


namespace MathChain.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DashboardPage _dashboardPage;
        private readonly ChapterPage _chapterPage;
        //private readonly SettingsPage _settingsPage;
        //private readonly AccountPage _accountPage;


        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new LoginPage());
            _chapterPage = new ChapterPage();
            _dashboardPage = new DashboardPage();
            //_settingsPage = new SettingsPage();
            //_accountPage = new AccountPage();
            //MainFrame.Navigate(_dashboardPage);
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(_dashboardPage);
        }

        private void ChaptersButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(_chapterPage);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;


namespace MathChain.WPF.Views
{
    /// <summary>
    /// Interaction logic for ChapterPage.xaml
    /// </summary>
    public partial class ChapterPage : Page
    {
        private readonly CalculusPage _calculusPage;
        public ChapterPage()
        {
            InitializeComponent();
            _calculusPage = new CalculusPage();
        }

        private void CalculusButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CalculusPage());
        }
    }
}

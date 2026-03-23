using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MathChain.WPF.Views
{
    /// <summary>
    /// Interaction logic for IntegralListPage.xaml
    /// </summary>
    public partial class CalculusPage : Page
    {
        private readonly IntegralsPage _integralsPage;
        public CalculusPage()
        {
            InitializeComponent();
            //DataContext = new IntegralListViewModel();
            _integralsPage = new IntegralsPage();
        }

        private void IntegralsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new IntegralsPage());
        }


    }
}

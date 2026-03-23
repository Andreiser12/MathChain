using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace MathChain.WPF.Views
{
    /// <summary>
    /// Interaction logic for IntegralsPage.xaml
    /// </summary>
    public partial class IntegralsPage : Page
    {
        private readonly ExercisePage _exercisePage;
        public IntegralsPage()
        {
            InitializeComponent();
            _exercisePage = new ExercisePage();
        }

        private void BasicFormulasButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new BasicFormulasPage());
        }
    }
}

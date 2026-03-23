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

namespace MathChain.WPF.Views
{
    /// <summary>
    /// Interaction logic for BasicFormulasPage.xaml
    /// </summary>
    public partial class BasicFormulasPage : Page
    {
        public BasicFormulasPage()
        {
            InitializeComponent();
        }

        private void BasicFormulasButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to the exercises page or start the exercises
            MessageBox.Show("Starting integral exercises...");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void StartExercisesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ExercisePage());
        }
    }
}

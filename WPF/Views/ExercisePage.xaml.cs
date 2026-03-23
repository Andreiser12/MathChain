using MathChain.WPF.Services;
using MathChain.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;


namespace MathChain.WPF.Views
{
    /// <summary>
    /// Interaction logic for ExercisePage.xaml
    /// </summary>
    public partial class ExercisePage : Page
    {
        private double exactSolution = 1.0;
        private double epsilon = 0.001;
        public ExercisePage()
        {
            InitializeComponent();
            string mockWolframAlphaResponse = @"\int_{1}^{e} \frac{1}{x} \,dx";
            FormulaDisplay.Formula = mockWolframAlphaResponse;
        }

        private void GoToFormulas_Click(object sender, RoutedEventArgs e)
        {
        }

        private void VerifyAnswer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string userInput = AnswerBox.Text.Trim();

            if (double.TryParse(userInput, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double userAnswer))
            {
                if (Math.Abs(userAnswer - exactSolution) <= epsilon)
                {
                    System.Windows.MessageBox.Show("Corect! Ai rezolvat perfect integrala.", "Rezultat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                    AnswerBox.BorderBrush = System.Windows.Media.Brushes.LimeGreen;
                }
                else
                {
                    System.Windows.MessageBox.Show("Incorect. Mai verifică o dată calculele!", "Rezultat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

                    AnswerBox.BorderBrush = System.Windows.Media.Brushes.Red;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Te rog să introduci un număr valid (folosește punctul pentru zecimale, ex: 1.002).", "Eroare format", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

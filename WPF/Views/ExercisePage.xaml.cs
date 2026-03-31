using MathChain.WPF.Services;
using MathChain.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace MathChain.WPF.Views
{
    /// <summary>
    /// Interaction logic for ExercisePage.xaml
    /// </summary>
    public partial class ExercisePage : Page
    {
        private WolframService _wolfram;
        private ApiService _apiService;
        private Guid _currentProblemId;
        private double _currentExactSolution = 0;
        private double _epsilon = 10;
        public ExercisePage()
        {
            InitializeComponent();
            _wolfram = new WolframService();
            _apiService = new ApiService();

            LoadNewProblemAsync();
        }

        private async void LoadNewProblemAsync()
        {
            _currentProblemId = Guid.NewGuid();

            var (query, latex) = _wolfram.GenerateRandomIntegral();

            FormulaDisplay.Formula = "\\text{Conectare Wolfram...}";

            VerifyAnswer.IsEnabled = false;

            _currentExactSolution = await _wolfram.GetExactSolutionAsync(query);

            FormulaDisplay.Formula = latex;
            VerifyAnswer.IsEnabled = true;
        }

        private void GoToFormulas_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void VerifyAnswer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string userInput = AnswerBox.Text.Replace(",", ".");

            if (double.TryParse(userInput, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double userAnswer))
            {
                if (Math.Abs(userAnswer * 1000 - _currentExactSolution * 1000) <= _epsilon)
                {
                    try
                    {
                        var txHash = await _apiService.SubmitSolutionAsync(
                        _currentProblemId,
                        AppSession.WalletAddress,
                        userAnswer);
                        MessageBox.Show($"Răspuns Corect! Tx: {txHash[..10]}...", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                        AnswerBox.BorderBrush = Brushes.LimeGreen;
                    }
                   catch (Exception ex)
                    {
                        MessageBox.Show($"Răspuns Corect, dar eroare blockchain: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                        AnswerBox.BorderBrush = Brushes.Orange;
                    }
                }
                else
                {
                    MessageBox.Show($"Greșit! Încearcă din nou.", "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
                    AnswerBox.BorderBrush = Brushes.Red;
                }
            }
            else
            {
                MessageBox.Show("Introdu un număr valid!", "Atenție", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void NextProblemButton_Click(object sender, RoutedEventArgs e)
        {
            AnswerBox.Text = "";
            AnswerBox.BorderBrush = Brushes.Gray;
            LoadNewProblemAsync();
        }
    }
}

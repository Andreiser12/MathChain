using CommunityToolkit.Mvvm.Input;
using MathChain.Domain.Entities;
using MathChain.WPF.Services;
using System.Windows.Input;

namespace MathChain.WPF.ViewModels
{
    public class IntegralViewModel : ViewModelBase
    {
        private IntegralProblem _currentProblem;
        public IntegralProblem CurrentProblem
        {
            get => _currentProblem;
            set
            {
                _currentProblem = value;
                OnPropertyChanged(nameof(CurrentProblem));
            }
        }

        private string _userSolution;
        public string UserSolution
        {
            get => _userSolution;
            set
            {
                _userSolution = value;
                OnPropertyChanged(nameof(UserSolution));
            }
        }

        private string _resultMessage;
        public string ResultMessage
        {
            get => _resultMessage;
            set
            {
                _resultMessage = value;
                OnPropertyChanged(nameof(ResultMessage));
            }
        }

        private string _walletAddress;
        public string WalletAddress
        {
            get => _walletAddress;
            set
            {
                _walletAddress = value;
                OnPropertyChanged(nameof(WalletAddress));
            }
        }

        private decimal _walletBalance;
        public decimal WalletBalance
        {
            get => _walletBalance;
            set
            {
                _walletBalance = value;
                OnPropertyChanged(nameof(WalletBalance));
            }
        }

        private bool _isCorrect;
        public bool IsCorrect
        {
            get => _isCorrect;
            set
            {
                _isCorrect = value;
                OnPropertyChanged(nameof(IsCorrect));
            }
        }

        private bool _isSolutionVisible;
        public bool IsSolutionVisible
        {
            get => _isSolutionVisible;
            set
            {
                _isSolutionVisible = value;
                OnPropertyChanged(nameof(IsSolutionVisible));
            }
        }

        private readonly ApiService _apiService;

        public ICommand VerifyCommand { get; }
        public ICommand PayForSolutionCommand { get; }
        public ICommand LoadBalanceCommand { get; }

        public IntegralViewModel(): this(new ApiService()) {}

        public IntegralViewModel(ApiService apiService)
        {
            _apiService = apiService;
            VerifyCommand = new RelayCommand(async () => await VerifySolutionAsync());
            PayForSolutionCommand = new RelayCommand(async () => await PayForSolutionAsync());
            LoadBalanceCommand = new RelayCommand(async () => await GetWalletBalanceAsync());

            CurrentProblem = new IntegralProblem
            {
                Expression = "x^2",
                CorrectSolution = 0.333,
                Variable = "x",
                LowerBound = 0,
                UpperBound = 1
            };
        }

        private async Task GetWalletBalanceAsync()
        {
            try
            {
                var balance = await _apiService.GetWalletBalanceAsync(WalletAddress);
                WalletBalance = balance;
            }
            catch (Exception ex)
            {
                ResultMessage = $"Error loading balance: {ex.Message}";
            }
        }

        private async Task PayForSolutionAsync()
        {
            try
            {
                var result = await _apiService.PayForSolutionAsync(CurrentProblem.Id, WalletAddress);
                if (result != null)
                {
                    IsSolutionVisible = true;
                    WalletBalance = await _apiService.GetWalletBalanceAsync(WalletAddress);
                }
            }
            catch (Exception ex)
            {
                ResultMessage = $"Error paying for solution: {ex.Message}";
            }
        }

        private async Task VerifySolutionAsync()
        {
            try
            {
                double solution = double.Parse(UserSolution);
                var result = await _apiService.VerifySolutionAsync(CurrentProblem.Id, solution);

                IsCorrect = result;

                if (IsCorrect)
                {
                    ResultMessage = "Correct solution!";
                    IsSolutionVisible = true;
                }
                else
                {
                    ResultMessage = "Incorrect solution. Please try again.";
                    IsSolutionVisible = false;
                }
            }
            catch (Exception ex)
            {
                ResultMessage = $"Error verifying solution: {ex.Message}";
            }
        }
    }
}

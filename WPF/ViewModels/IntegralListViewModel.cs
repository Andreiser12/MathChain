using System.Collections.ObjectModel;


namespace MathChain.WPF.ViewModels
{
    public class IntegralListViewModel : ViewModelBase
    {
        public ObservableCollection<string> Formulas { get; }

        private string _selectedFormula;

        public string SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                _selectedFormula = value;
                OnPropertyChanged(nameof(SelectedFormula));
            }
        }

        public IntegralListViewModel()
        {
            Formulas = new ObservableCollection<string>
            {
                "∫adx = ax + C",
                "∫xⁿdx = xⁿ⁺¹/n+1 + C",
                "∫(1/x)dx = ln|x| + C",
                "∫aˣdx = aˣ/lna + C",
                "∫eˣdx = eˣ + C"
            };
        }
    }
}

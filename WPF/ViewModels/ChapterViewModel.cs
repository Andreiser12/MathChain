using System.Collections.ObjectModel;


namespace MathChain.WPF.ViewModels
{
    public class ChapterViewModel : ViewModelBase
    {
        public ObservableCollection<string> Chapters { get; set; }

        private string _selectedChapter;
        public string SelectedChapter 
        {
            get => _selectedChapter;
            set
            {
                _selectedChapter = value;
                OnPropertyChanged(nameof(SelectedChapter));
            }
        }

        public ChapterViewModel()
        {
            Chapters = new ObservableCollection<string> { "Calculus" };
        }
    }
}

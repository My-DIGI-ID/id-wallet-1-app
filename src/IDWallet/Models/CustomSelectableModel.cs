namespace IDWallet.Models
{
    public class CustomSelectableModel<TType> : CustomObservableModel
    {
        private bool _isSelected;

        private TType _model;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public TType Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }
    }
}
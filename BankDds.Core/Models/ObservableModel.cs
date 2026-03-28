using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Provides property change notification for editable domain models bound directly to WPF forms.
    /// </summary>
    public abstract class ObservableModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, params string[] dependentPropertyNames)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged();

            foreach (var propertyName in dependentPropertyNames)
            {
                OnPropertyChanged(propertyName);
            }

            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

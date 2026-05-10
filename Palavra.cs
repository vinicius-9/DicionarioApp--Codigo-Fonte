using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DicionarioApp
{
    public class Palavra : INotifyPropertyChanged
    {
        private string _palavraTexto = "";
        private string _significado  = "";
        private string _exemplo      = "";

        public string PalavraTexto
        {
            get => _palavraTexto;
            set { _palavraTexto = value; OnPropertyChanged(); }
        }

        public string Significado
        {
            get => _significado;
            set { _significado = value; OnPropertyChanged(); }
        }

        public string Exemplo
        {
            get => _exemplo;
            set { _exemplo = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

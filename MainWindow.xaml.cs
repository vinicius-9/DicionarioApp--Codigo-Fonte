using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DicionarioApp
{
    public partial class MainWindow : Window
    {
        //  Serviço e estado 
        private readonly DicionarioServico _servico = new();
        private Palavra? _palavraSelecionada;   // para edição via formulário
        private Palavra? _palavraNoDetalhe;     // palavra exibida no painel de detalhe
        private bool     _editando;

        // Construtor
        public MainWindow()
        {
            InitializeComponent();
            DefinirIconeJanela();
            _servico.Carregar();
            AtualizarLista();
        }

        // P/Invoke para liberar o HICON após uso
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        // Ícone da janela / taskbar (desenhado via WPF puro) 
        private void DefinirIconeJanela()
        {
            // Canvas de 64×64 px representando o logo violeta com livro aberto
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // fundo violeta com cantos arredondados simulados por elipse + rect
                var violeta = new SolidColorBrush(Color.FromRgb(0x7c, 0x3a, 0xed));
                dc.DrawRoundedRectangle(violeta, null, new Rect(0, 0, 64, 64), 14, 14);

                // página esquerda
                var branco90 = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
                var geomEsq  = Geometry.Parse(
                    "M 8,14 C 14,13 22,12.5 30,14 L 30,50 C 22,48.5 14,47.5 8,49 Z");
                dc.DrawGeometry(branco90, null, geomEsq);

                // página direita
                var branco55 = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255));
                var geomDir  = Geometry.Parse(
                    "M 34,14 C 42,12.5 50,13 56,14 L 56,49 C 50,47.5 42,48.5 34,50 Z");
                dc.DrawGeometry(branco55, null, geomDir);

                // lombada
                var lombada = new SolidColorBrush(Color.FromArgb(160, 233, 213, 255));
                dc.DrawRectangle(lombada, null, new Rect(29, 13, 6, 37));

                // linhas na página esquerda
                var linhaEsq = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1.8);
                dc.DrawLine(linhaEsq, new Point(12, 24), new Point(26, 24));
                dc.DrawLine(linhaEsq, new Point(12, 30), new Point(26, 30));
                dc.DrawLine(linhaEsq, new Point(12, 36), new Point(24, 36));

                // linhas na página direita
                var linhaDir = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1.8);
                dc.DrawLine(linhaDir, new Point(38, 24), new Point(52, 24));
                dc.DrawLine(linhaDir, new Point(38, 30), new Point(52, 30));
                dc.DrawLine(linhaDir, new Point(38, 36), new Point(50, 36));

                // estrela acima
                var estrela = new SolidColorBrush(Color.FromArgb(220, 233, 213, 255));
                dc.DrawEllipse(estrela, null, new Point(32, 7), 3.5, 3.5);
            }

            var rtb = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            // Converte para PNG em memória e cria um HICON nativo.
            // Isso garante que o ícone apareça corretamente na barra de tarefas
            // e no Alt+Tab — o RenderTargetBitmap direto não funciona na taskbar.
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var ms = new System.IO.MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            using var bmp = new System.Drawing.Bitmap(ms);
            var hIcon = bmp.GetHicon();
            try
            {
                Icon = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        
        //  SCROLL — repassa o scroll do ListBox para o ScrollViewer pai
        

        /// <summary>
        /// O ListBox intercepta o MouseWheel antes do ScrollViewer externo.
        /// Este handler captura o evento e rola o ScrollViewer manualmente.
        /// </summary>
        private void ListaPalavras_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollLista.ScrollToVerticalOffset(scrollLista.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }

       
        //  FORMULÁRIO

        /// <summary>Adiciona ou salva a edição de uma palavra.</summary>
        private void Adicionar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPalavra.Text) ||
                string.IsNullOrWhiteSpace(txtSignificado.Text))
            {
                MessageBox.Show("Preencha a palavra e o significado.",
                                "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_editando && _palavraSelecionada != null)
                {
                    _palavraSelecionada.PalavraTexto = txtPalavra.Text.Trim();
                    _palavraSelecionada.Significado  = txtSignificado.Text.Trim();
                    _palavraSelecionada.Exemplo      = txtExemplo.Text.Trim();
                    _servico.Salvar();
                    MessageBox.Show("Palavra atualizada com sucesso!",
                                    "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _servico.Adicionar(
                        txtPalavra.Text.Trim(),
                        txtSignificado.Text.Trim(),
                        txtExemplo.Text.Trim());
                    MessageBox.Show("Palavra adicionada com sucesso!",
                                    "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                LimparFormulario();
                AtualizarLista();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar palavra: {ex.Message}",
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Limpa todos os campos e volta ao modo de adição.</summary>
        private void Limpar_Click(object sender, RoutedEventArgs e) => LimparFormulario();

        /// <summary>Filtra a lista em tempo real.</summary>
        private void Buscar_TextChanged(object sender, TextChangedEventArgs e)
            => AtualizarLista(txtBusca.Text.Trim());

        
        //  LISTA DE PALAVRAS
      

        /// <summary>
        /// Clique no card da palavra (área fora dos botões editar/remover).
        /// Abre o painel de detalhe.
        /// </summary>
        private void ItemCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Se o clique partiu de dentro de um Button, ignoramos
            if (OrigemEhBotao(e.OriginalSource as DependencyObject))
                return;

            if (sender is Border border && border.DataContext is Palavra p)
                MostrarDetalhe(p);
        }

        /// <summary>Botão de editar dentro do card — carrega no formulário.</summary>
        private void EditarItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Palavra p)
                CarregarNoFormulario(p);
        }

        /// <summary>Botão de remover dentro do card — pede confirmação.</summary>
        private void RemoverItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Palavra p)
                RemoverPalavra(p);
        }

        
        //  PAINEL DE DETALHE
       

        /// <summary>Exibe o painel de detalhe para a palavra escolhida.</summary>
        private void MostrarDetalhe(Palavra p)
        {
            _palavraNoDetalhe = p;

            lblDetalhePalavra.Text    = p.PalavraTexto;
            lblDetalheSignificado.Text = p.Significado;
            lblDetalheExemplo.Text    = p.Exemplo;

            boxExemplo.Visibility = string.IsNullOrWhiteSpace(p.Exemplo)
                ? Visibility.Collapsed
                : Visibility.Visible;

            scrollLista.Visibility   = Visibility.Collapsed;
            scrollDetalhe.Visibility = Visibility.Visible;

            // Volta ao topo do scroll de detalhe
            scrollDetalhe.ScrollToTop();
        }

        /// <summary>Fecha o painel de detalhe e volta à lista.</summary>
        private void Voltar_Click(object sender, RoutedEventArgs e)
        {
            scrollDetalhe.Visibility = Visibility.Collapsed;
            scrollLista.Visibility   = Visibility.Visible;
            _palavraNoDetalhe        = null;
        }

        /// <summary>Botão Editar dentro do painel de detalhe.</summary>
        private void EditarDetalhe_Click(object sender, RoutedEventArgs e)
        {
            if (_palavraNoDetalhe == null) return;
            CarregarNoFormulario(_palavraNoDetalhe);
            Voltar_Click(sender, e);
        }

        /// <summary>Botão Remover dentro do painel de detalhe.</summary>
        private void RemoverDetalhe_Click(object sender, RoutedEventArgs e)
        {
            if (_palavraNoDetalhe == null) return;
            var p = _palavraNoDetalhe;
            if (RemoverPalavra(p))
            {
                Voltar_Click(sender, e);
                AtualizarLista(txtBusca.Text.Trim());
            }
        }

    

        private void CarregarNoFormulario(Palavra p)
        {
            _palavraSelecionada   = p;
            _editando             = true;
            txtPalavra.Text       = p.PalavraTexto;
            txtSignificado.Text   = p.Significado;
            txtExemplo.Text       = p.Exemplo;
            btnAdicionar.Content  = "Salvar";
            txtPalavra.Focus();
        }

        /// <summary>Remove uma palavra com confirmação. Retorna true se removeu.</summary>
        private bool RemoverPalavra(Palavra p)
        {
            var res = MessageBox.Show(
                $"Tem certeza que quer remover \"{p.PalavraTexto}\"?",
                "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return false;

            _servico.Deletar(p.PalavraTexto);
            AtualizarLista(txtBusca.Text.Trim());
            return true;
        }

        private void LimparFormulario()
        {
            txtPalavra.Clear();
            txtSignificado.Clear();
            txtExemplo.Clear();
            txtBusca.Clear();
            _palavraSelecionada  = null;
            _editando            = false;
            btnAdicionar.Content = "Adicionar";
            listaPalavras.SelectedItem = null;
            AtualizarLista();
        }

        private void AtualizarLista(string filtro = "")
        {
            var palavras = _servico.Buscar(filtro);
            listaPalavras.ItemsSource = palavras;
            lblTotal.Text = $"{palavras.Count} palavras";
        }

        /// <summary>
        /// Verifica se um elemento ou qualquer ancestral seu é um Button.
        /// Usado para evitar que o clique nos botões de ação acione o detalhe.
        /// </summary>
        private static bool OrigemEhBotao(DependencyObject? elemento)
        {
            var atual = elemento;
            while (atual != null)
            {
                if (atual is Button) return true;
                atual = VisualTreeHelper.GetParent(atual);
            }
            return false;
        }
    }
}

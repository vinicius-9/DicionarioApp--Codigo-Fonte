using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DicionarioApp
{
    public class DicionarioServico
    {
        private readonly string _arquivo = "dicionario.json";

        public List<Palavra> Palavras { get; set; } = new();

        public void Carregar()
        {
            if (File.Exists(_arquivo))
            {
                var json = File.ReadAllText(_arquivo);
                Palavras = JsonSerializer.Deserialize<List<Palavra>>(json) ?? new();
            }
        }

        public void Salvar()
        {
            var json = JsonSerializer.Serialize(Palavras,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_arquivo, json);
        }

        public void Adicionar(string palavra, string significado, string exemplo)
        {
            Palavras.Add(new Palavra
            {
                PalavraTexto = palavra,
                Significado  = significado,
                Exemplo      = exemplo
            });
            Salvar();
        }

        public void Deletar(string palavra)
        {
            Palavras.RemoveAll(p =>
                p.PalavraTexto.ToLower() == palavra.ToLower());
            Salvar();
        }

        public List<Palavra> Buscar(string termo)
        {
            if (string.IsNullOrWhiteSpace(termo))
                return Palavras;

            return Palavras.FindAll(p =>
                p.PalavraTexto.ToLower().Contains(termo.ToLower()) ||
                p.Significado .ToLower().Contains(termo.ToLower()));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RotaViagem.Testes
{
    public class TesteRotaViagem
    {
        // Método para executar os testes
        public static void ExecutarTestes()
        {
            // Teste de inicialização de rotas
            TestarCarregarRotas();

            // Teste de encontrar a melhor rota
            TestarEncontrarMelhorRota();
        }

        // Teste do método CarregarRotas
        private static void TestarCarregarRotas()
        {
            // Criando o arquivo de teste com algumas rotas
            string caminhoArquivo = "rotas_teste.csv";
            string conteudo = "GRU,BRC,10\nBRC,SCL,5\nGRU,CDG,75\nGRU,SCL,20\nGRU,ORL,56\nORL,CDG,5\nSCL,ORL,20\n";
            File.WriteAllText(caminhoArquivo, conteudo);

            // Carregando as rotas usando o método CarregarRotas
            Programa.CarregarRotas(caminhoArquivo);

            // Validando se as rotas foram carregadas corretamente
            if (Programa.rotas.Count == 5) // 5 origens diferentes
            {
                Console.WriteLine("Teste CarregarRotas PASSED");
            }
            else
            {
                Console.WriteLine("Teste CarregarRotas FAILED");
            }

            // Limpando o arquivo após o teste
            File.Delete(caminhoArquivo);
        }

        // Teste do método EncontrarMelhorRota
        private static void TestarEncontrarMelhorRota()
        {
            // Adicionando manualmente as rotas
            Programa.AdicionarRota("GRU", "BRC", 10);
            Programa.AdicionarRota("BRC", "SCL", 5);
            Programa.AdicionarRota("GRU", "CDG", 75);
            Programa.AdicionarRota("GRU", "SCL", 20);
            Programa.AdicionarRota("GRU", "ORL", 56);
            Programa.AdicionarRota("ORL", "CDG", 5);
            Programa.AdicionarRota("SCL", "ORL", 20);

            // Testando o método de encontrar a melhor rota
            string resultado = Programa.EncontrarMelhorRota("GRU", "CDG");
            string resultadoEsperado = "Melhor Rota: GRU - BRC - SCL - ORL - CDG ao custo de $40";

            if (resultado == resultadoEsperado)
            {
                Console.WriteLine("Teste EncontrarMelhorRota PASSED");
            }
            else
            {
                Console.WriteLine("Teste EncontrarMelhorRota FAILED");
                Console.WriteLine($"Esperado: {resultadoEsperado}");
                Console.WriteLine($"Obtido: {resultado}");
            }
        }
    }

    class Programa
    {
        public static Dictionary<string, List<Rota>> rotas = new Dictionary<string, List<Rota>>();

        public static void CarregarRotas(string caminhoArquivo)
        {
            if (string.IsNullOrEmpty(caminhoArquivo) || !File.Exists(caminhoArquivo))
            {
                Console.WriteLine("Arquivo de rotas não encontrado.");
                return;
            }

            var linhas = File.ReadAllLines(caminhoArquivo);

            foreach (var linha in linhas)
            {
                var partes = linha.Split(',');

                if (partes.Length == 3 && int.TryParse(partes[2], out int custo))
                {
                    AdicionarRota(partes[0], partes[1], custo);
                }
            }
        }

        public static void AdicionarRota(string origem, string destino, int custo)
        {
            if (!rotas.ContainsKey(origem))
            {
                rotas[origem] = new List<Rota>();
            }
            rotas[origem].Add(new Rota(destino, custo));
        }

        public static string EncontrarMelhorRota(string origem, string destino)
        {
            if (origem == null || destino == null)
                return "Origem ou destino não especificado.";

            var visitados = new HashSet<string>();
            var filaPrioridade = new SortedSet<EstadoRota>(Comparer<EstadoRota>.Create((x, y) =>
            {
                int comparacao = x.Custo.CompareTo(y.Custo);
                if (comparacao == 0)
                {
                    return string.Compare(x.Caminho, y.Caminho, StringComparison.Ordinal);
                }
                return comparacao;
            }));

            filaPrioridade.Add(new EstadoRota(origem, 0, origem));

            while (filaPrioridade.Count > 0)
            {
                var atual = filaPrioridade.First();
                filaPrioridade.Remove(atual);

                if (atual.Localizacao == destino)
                {
                    return $"Melhor Rota: {atual.Caminho} ao custo de ${atual.Custo}";
                }

                if (!visitados.Contains(atual.Localizacao))
                {
                    visitados.Add(atual.Localizacao);

                    if (rotas.ContainsKey(atual.Localizacao))
                    {
                        foreach (var rota in rotas[atual.Localizacao])
                        {
                            if (!visitados.Contains(rota.Destino))
                            {
                                filaPrioridade.Add(new EstadoRota(rota.Destino, atual.Custo + rota.Custo, $"{atual.Caminho} - {rota.Destino}"));
                            }
                        }
                    }
                }
            }

            return "Rota não encontrada.";
        }
    }

    public class Rota
    {
        public string Destino { get; set; }
        public int Custo { get; set; }

        public Rota(string destino, int custo)
        {
            Destino = destino ?? throw new ArgumentNullException(nameof(destino));
            Custo = custo;
        }
    }

    public class EstadoRota
    {
        public string Localizacao { get; set; }
        public int Custo { get; set; }
        public string Caminho { get; set; }

        public EstadoRota(string localizacao, int custo, string caminho)
        {
            Localizacao = localizacao ?? throw new ArgumentNullException(nameof(localizacao));
            Custo = custo;
            Caminho = caminho ?? throw new ArgumentNullException(nameof(caminho));
        }
    }
}


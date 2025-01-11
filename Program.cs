using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RotaViagem
{
    public class Programa
    {
        // Dicionário que armazena as rotas, onde a chave é a origem e o valor é uma lista de rotas
        static Dictionary<string, List<Rota>> rotas = new Dictionary<string, List<Rota>>();
        // Caminho do arquivo CSV que contém as rotas
        static string? caminhoArquivo;

        // Função principal que inicia o programa
        public static void Main(string[] args)
        {
            // Verifica se o caminho do arquivo foi passado como argumento
            if (args.Length == 0)
            {
                Console.WriteLine("Uso: executavel <arquivo.csv>");
                return;
            }

            // Armazena o caminho do arquivo
            caminhoArquivo = args[0];
            // Carrega as rotas a partir do arquivo
            CarregarRotas(caminhoArquivo);

            // Pergunta ao usuário qual interface ele deseja utilizar
            Console.WriteLine("Escolha o modo: console ou rest");
            string? modo = Console.ReadLine();

            // Se o modo for "console", inicia a interface de console
            if (modo?.ToLower() == "console")
            {
                IniciarInterfaceConsole();
            }
            // Se o modo for "rest", inicia a interface REST
            else if (modo?.ToLower() == "rest")
            {
                IniciarInterfaceRest();
            }
            // Se o modo for inválido, exibe mensagem de erro
            else
            {
                Console.WriteLine("Modo inválido.");
            }
        }

        // Função que carrega as rotas a partir de um arquivo CSV
        static void CarregarRotas(string? arquivo)
        {
            // Verifica se o arquivo é válido
            if (arquivo == null || !File.Exists(arquivo))
            {
                Console.WriteLine($"Arquivo {arquivo} não encontrado.");
                return;
            }

            // Lê todas as linhas do arquivo
            var linhas = File.ReadAllLines(arquivo);

            // Itera por cada linha do arquivo
            foreach (var linha in linhas)
            {
                var partes = linha.Split(',');
                // Se a linha estiver no formato correto (origem, destino, custo)
                if (partes.Length == 3 && int.TryParse(partes[2], out int custo))
                {
                    // Adiciona a rota ao dicionário
                    AdicionarRota(partes[0], partes[1], custo);
                }
            }
        }

        // Função que adiciona uma rota ao dicionário de rotas
        static void AdicionarRota(string origem, string destino, int custo)
        {
            // Se a origem ainda não existe no dicionário, cria uma nova lista de rotas para ela
            if (!rotas.ContainsKey(origem))
            {
                rotas[origem] = new List<Rota>();
            }

            // Adiciona a nova rota à lista de rotas da origem
            rotas[origem].Add(new Rota(destino, custo));
        }

        // Função que encontra a melhor rota entre origem e destino
        static string EncontrarMelhorRota(string? origem, string? destino)
        {
            // Verifica se a origem ou destino não foram fornecidos
            if (origem == null || destino == null)
                return "Origem ou destino não especificado.";

            // Conjunto que armazena os locais já visitados para evitar ciclos
            var visitados = new HashSet<string>();
            // Fila de prioridade para armazenar os estados das rotas
            var filaPrioridade = new SortedSet<EstadoRota>(Comparer<EstadoRota>.Create((x, y) =>
            {
                // Compara os custos das rotas
                int comparacao = x.Custo.CompareTo(y.Custo);
                if (comparacao == 0)
                {
                    // Se os custos forem iguais, compara pelo caminho
                    return string.Compare(x.Caminho, y.Caminho, StringComparison.Ordinal);
                }
                return comparacao;
            }));

            // Adiciona a origem à fila de prioridade com custo 0 e caminho inicial
            filaPrioridade.Add(new EstadoRota(origem, 0, origem));

            // Enquanto houver elementos na fila
            while (filaPrioridade.Count > 0)
            {
                var atual = filaPrioridade.First();
                filaPrioridade.Remove(atual);

                // Se o local atual for o destino, retorna o caminho encontrado
                if (atual.Localizacao == destino)
                {
                    return $"Melhor Rota: {atual.Caminho} ao custo de ${atual.Custo}";
                }

                // Se o local ainda não foi visitado
                if (!visitados.Contains(atual.Localizacao))
                {
                    visitados.Add(atual.Localizacao);

                    // Verifica se a origem tem rotas registradas
                    if (rotas.ContainsKey(atual.Localizacao) && rotas[atual.Localizacao] != null)
                    {
                        // Para cada rota a partir do local atual
                        foreach (var rota in rotas[atual.Localizacao])
                        {
                            // Se o destino da rota ainda não foi visitado, adiciona à fila
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

        // Função que inicia a interface de console
        static void IniciarInterfaceConsole()
        {
            while (true)
            {
                // Solicita ao usuário a entrada de uma rota no formato "DE-PARA"
                Console.WriteLine("Digite a rota (formato DE-PARA) ou 'sair' para encerrar:");
                string? entrada = Console.ReadLine();

                // Se o usuário digitar "sair", encerra o loop
                if (entrada?.ToLower() == "sair") break;

                var partes = entrada?.Split('-');
                // Se a entrada estiver no formato correto
                if (partes?.Length == 2)
                {
                    // Encontra a melhor rota e exibe o resultado
                    string resultado = EncontrarMelhorRota(partes[0], partes[1]);
                    Console.WriteLine(resultado);
                }
                else
                {
                    // Se o formato for inválido, informa o usuário
                    Console.WriteLine("Formato inválido. Use DE-PARA.");
                }
            }
        }

        // Função que inicia a interface REST
        static void IniciarInterfaceRest()
        {
            // Cria um ouvinte HTTP para receber as requisições
            HttpListener ouvinte = new HttpListener();
            ouvinte.Prefixes.Add("http://localhost:5000/");
            ouvinte.Start();
            Console.WriteLine("Servidor REST iniciado em http://localhost:5000/");

            // Loop contínuo para tratar as requisições
            while (true)
            {
                var contexto = ouvinte.GetContext();
                var requisicao = contexto.Request;
                var resposta = contexto.Response;

                // Verifica se a URL não é nula antes de acessar suas propriedades
                if (requisicao.Url != null)
                {
                    // Se for uma requisição POST para "/registrar", adiciona uma nova rota
                    if (requisicao.HttpMethod == "POST" && requisicao.Url.AbsolutePath == "/registrar")
                    {
                        using var leitor = new StreamReader(requisicao.InputStream);
                        var corpo = leitor.ReadToEnd();
                        var novaRota = JsonSerializer.Deserialize<EntradaRota>(corpo);

                        // Se a rota for válida, adiciona no dicionário e no arquivo
                        if (novaRota?.Origem != null && novaRota?.Destino != null)
                        {
                            AdicionarRota(novaRota.Origem, novaRota.Destino, novaRota.Custo);
                            File.AppendAllText(caminhoArquivo ?? string.Empty, $"{novaRota.Origem},{novaRota.Destino},{novaRota.Custo}\n");
                        }

                        resposta.StatusCode = 200;
                        resposta.Close();
                    }
                    // Se for uma requisição GET para "/melhor-rota", retorna a melhor rota
                    else if (requisicao.HttpMethod == "GET" && requisicao.Url.AbsolutePath == "/melhor-rota")
                    {
                        var consulta = requisicao.QueryString;
                        string? origem = consulta["origem"];
                        string? destino = consulta["destino"];

                        string resultado = EncontrarMelhorRota(origem, destino);
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(resultado);

                        resposta.ContentLength64 = buffer.Length;
                        resposta.OutputStream.Write(buffer, 0, buffer.Length);
                        resposta.Close();
                    }
                    // Se o caminho for inválido, retorna erro 404
                    else
                    {
                        resposta.StatusCode = 404;
                        resposta.Close();
                    }
                }
                else
                {
                    // Caso a URL seja nula, retorna erro 400 (Bad Request)
                    resposta.StatusCode = 400;
                    resposta.Close();
                }
            }
        }
    }

    // Classe que representa uma rota com destino e custo
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

    // Classe que representa o estado de uma rota, incluindo local, custo e caminho
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

    // Classe usada para deserializar dados de entrada de rota para API REST
    public class EntradaRota
    {
        public string? Origem { get; set; }
        public string? Destino { get; set; }
        public int Custo { get; set; }
    }
}

//Para consultar no POSTMAN em POST pode colocar: http://localhost:5000/registrar e em Body selecionar Raw e colocar este código como exemplo: {"origem": "GRU", "destino": "SCL", "custo": 20} e deixar como JSON.

//Para consultar o GET é só colocar como exemplo: http://localhost:5000/melhor-rota?origem=GRU&destino=SCL
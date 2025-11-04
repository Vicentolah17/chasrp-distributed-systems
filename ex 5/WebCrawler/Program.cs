using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

public class WebCrawler
{
    private static int MaxUrls;
    private static int MaxThreads;
    
    private static readonly HashSet<string> _urlsVisitadas = new HashSet<string>();
    private static readonly Queue<string> _urlsParaVisitar = new Queue<string>();
    private static readonly List<string> _palavrasChave = new List<string>();
    
    private static readonly object _lockUrls = new object();
    private static readonly object _lockResultados = new object();
    
    private static Dictionary<string, Dictionary<string, int>> _resultados = new Dictionary<string, Dictionary<string, int>>();
    
    private static readonly HttpClient _httpClient = new HttpClient();

    public static void Main(string[] args)
    {
        Console.WriteLine("--- WebCrawler Multi-thread ---");

        Console.Write("Numero maximo de URLs a pesquisar: ");
        if (!int.TryParse(Console.ReadLine(), out MaxUrls) || MaxUrls <= 0) MaxUrls = 10;

        Console.Write("Numero maximo de Threads para crawling: ");
        if (!int.TryParse(Console.ReadLine(), out MaxThreads) || MaxThreads <= 0) MaxThreads = 5;

        if (!CarregaArquivos())
        {
            Console.WriteLine("ERRO: Nao foi possivel carregar arquivos iniciais. Verifique 'urls_iniciais.txt' e 'palavras_chave.txt'.");
            return;
        }

        Console.WriteLine($"Iniciando crawling. Max URLs: {MaxUrls}, Max Threads: {MaxThreads}");
        
        Thread[] threads = new Thread[MaxThreads];
        
        for (int i = 0; i < MaxThreads; i++)
        {
            threads[i] = new Thread(ProcessarUrl);
            threads[i].Name = $"CrawlerThread-{i + 1}";
            threads[i].Start();
        }

        
        DateTime inicio = DateTime.Now;
        bool crawlingAtivo = true;

        while (crawlingAtivo && (DateTime.Now - inicio).TotalSeconds < 60) 
        {
            Thread.Sleep(500); 

            lock (_lockUrls)
            {
                if (_urlsVisitadas.Count >= MaxUrls)
                {
                    crawlingAtivo = false;
                    break;
                }
                
                if (_urlsParaVisitar.Count == 0 && _urlsVisitadas.Count > 0)
                {
                    crawlingAtivo = false;
                    break;
                }
            }
        }
        
        
        foreach (var t in threads)
        {
            if (t.IsAlive)
            {
                t.Join(5000); 
            }
        }

        GeraArquivoResultados("resultados_crawler.txt");
        
        Console.WriteLine("\n--- FIM DA EXECUCAO ---");
        Console.WriteLine($"Total de URLs visitadas: {_urlsVisitadas.Count}");
        Console.WriteLine($"Resultados salvos em: resultados_crawler.txt");
    }

    private static bool CarregaArquivos()
    {
        try
        {
            string[] urlsIniciais = File.ReadAllLines("urls_iniciais.txt");
            lock (_lockUrls)
            {
                foreach (var url in urlsIniciais)
                {
                    if (!string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url.Trim(), UriKind.Absolute))
                    {
                        _urlsParaVisitar.Enqueue(url.Trim());
                    }
                }
            }
            
            _palavrasChave.AddRange(File.ReadAllLines("palavras_chave.txt")
                                        .Where(p => !string.IsNullOrWhiteSpace(p))
                                        .Select(p => p.ToLower().Trim()));
            
            return _urlsParaVisitar.Any() && _palavrasChave.Any();
        }
        catch
        {
            return false;
        }
    }

    private static void ProcessarUrl()
    {
        while (_urlsVisitadas.Count < MaxUrls)
        {
            string urlAtual = null;

            lock (_lockUrls)
            {
                if (_urlsVisitadas.Count >= MaxUrls) break;
                if (_urlsParaVisitar.Any())
                {
                    urlAtual = _urlsParaVisitar.Dequeue();
                    _urlsVisitadas.Add(urlAtual);
                }
            }

            if (urlAtual == null)
            {
                Thread.Sleep(1000); 
                continue;
            }

            Console.WriteLine($"[{Thread.CurrentThread.Name}] Visitando: {urlAtual}");

            try
            {
                var response = _httpClient.GetAsync(urlAtual).Result;
                if (!response.IsSuccessStatusCode) continue;

                string htmlContent = response.Content.ReadAsStringAsync().Result;
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                var textoPagina = htmlDoc.DocumentNode.SelectSingleNode("//body")?.InnerText.ToLower() ?? string.Empty;
                var frequencias = new Dictionary<string, int>();

                foreach (var palavra in _palavrasChave)
                {
                    int contagem = System.Text.RegularExpressions.Regex.Matches(textoPagina, $@"\b{palavra}\b").Count;
                    if (contagem > 0)
                    {
                        frequencias.Add(palavra, contagem);
                    }
                }

                if (frequencias.Any())
                {
                    lock (_lockResultados)
                    {
                        _resultados[urlAtual] = frequencias;
                    }
                }

                var links = htmlDoc.DocumentNode.SelectNodes("//a[@href]")
                                    ?.Select(n => n.GetAttributeValue("href", string.Empty))
                                    .Where(href => !string.IsNullOrEmpty(href))
                                    .ToList();

                lock (_lockUrls)
                {
                    foreach (var link in links)
                    {
                        if (Uri.TryCreate(new Uri(urlAtual), link, out Uri uriAbsoluta) && _urlsVisitadas.Count < MaxUrls)
                        {
                            string novaUrl = uriAbsoluta.AbsoluteUri;
                            if (!_urlsVisitadas.Contains(novaUrl) && !_urlsParaVisitar.Contains(novaUrl))
                            {
                                _urlsParaVisitar.Enqueue(novaUrl);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Thread.CurrentThread.Name}] ERRO ao processar {urlAtual}: {ex.Message}");
            }
        }
    }

    private static void GeraArquivoResultados(string nomeArquivo)
    {
        Console.WriteLine("\nGerando arquivo de resultados...");
        var sb = new StringBuilder();
        
        lock (_lockResultados)
        {
            foreach (var kvpUrl in _resultados)
            {
                sb.AppendLine($"URL: {kvpUrl.Key}");
                foreach (var kvpPalavra in kvpUrl.Value)
                {
                    sb.AppendLine($"- Encontrada: {kvpPalavra.Key}, Frequencia: {kvpPalavra.Value}");
                }
                sb.AppendLine("---");
            }
        }

        try
        {
            File.WriteAllText(nomeArquivo, sb.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO ao escrever o arquivo de resultados: {ex.Message}");
        }
    }
}
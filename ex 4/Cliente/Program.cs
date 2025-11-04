using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

public class Cliente
{
    private static string _diretorioLocalAtual = Environment.CurrentDirectory;

    public static void Main(string[] args)
    {
        const string ipServidor = "127.0.0.1";
        const int portaServidor = 5003;

        TcpClient cliente = null;
        
        try
        {
            cliente = new TcpClient(ipServidor, portaServidor);
            NetworkStream stream = cliente.GetStream();
            Console.WriteLine($"Conectado ao Mini-FTP em {ipServidor}:{portaServidor}");
            Console.WriteLine($"Diretorio local inicial: {_diretorioLocalAtual}");
            Console.WriteLine("Comandos disponiveis: CD, LS, PUT <arq_local>, GET <arq_remoto>, LCD <dir>, EXIT");

            while (true)
            {
                Console.Write($"\nMINI-FTP> ");
                string linha = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(linha)) continue;

                string[] partes = linha.Split(' ', 2);
                string comando = partes[0].ToUpper();
                string argumento = partes.Length > 1 ? partes[1] : string.Empty;

                if (comando == "EXIT") break;
                
                if (comando == "LCD")
                {
                    ComandoLCD(argumento);
                    continue;
                }
                
                if (comando == "LSD")
                {
                    ComandoLSD();
                    continue;
                }
                
                string resposta = string.Empty;
                
                if (comando == "PUT")
                {
                    resposta = ComandoPUT(stream, argumento);
                }
                else if (comando == "GET")
                {
                    resposta = ComandoGET(stream, argumento);
                }
                else
                {
                    EnviarMensagem(stream, linha);
                    resposta = ReceberResposta(stream);
                }
                
                Console.WriteLine(resposta);
            }
        }
        catch (SocketException)
        {
            Console.WriteLine("Nao foi possivel conectar ao servidor. Verifique se o Servidor 4 esta ativo na porta 5003.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu um erro: {ex.Message}");
        }
        finally
        {
            cliente?.Close();
            Console.WriteLine("\nSessao encerrada.");
        }
    }
    
    private static void ComandoLCD(string path)
    {
        try
        {
            string novoCaminho = Path.Combine(_diretorioLocalAtual, path);
            if (path == "..")
            {
                novoCaminho = Directory.GetParent(_diretorioLocalAtual)?.FullName ?? _diretorioLocalAtual;
            }
            
            if (Directory.Exists(novoCaminho))
            {
                _diretorioLocalAtual = Path.GetFullPath(novoCaminho);
                Console.WriteLine($"Diretorio local alterado para {_diretorioLocalAtual}");
            }
            else
            {
                Console.WriteLine("ERRO: Diretorio local nao encontrado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO LCD: {ex.Message}");
        }
    }
    
    private static void ComandoLSD()
    {
        try
        {
            string[] arquivos = Directory.GetFiles(_diretorioLocalAtual);
            string[] diretorios = Directory.GetDirectories(_diretorioLocalAtual);
            
            Console.WriteLine($"Conteudo de {_diretorioLocalAtual}");
            foreach (string dir in diretorios)
            {
                Console.WriteLine($"[DIR] {Path.GetFileName(dir)}");
            }
            foreach (string file in arquivos)
            {
                Console.WriteLine($"[ARQ] {Path.GetFileName(file)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO LSD: {ex.Message}");
        }
    }

    private static string ComandoPUT(NetworkStream stream, string nomeArquivoLocal)
    {
        try
        {
            string caminhoLocal = Path.Combine(_diretorioLocalAtual, nomeArquivoLocal);
            if (!File.Exists(caminhoLocal))
            {
                return $"ERRO: Arquivo local '{nomeArquivoLocal}' nao encontrado.";
            }
            
            string comandoEnvio = $"PUT {nomeArquivoLocal}";
            EnviarMensagem(stream, comandoEnvio);
            
            string ack = ReceberResposta(stream);
            if (ack != "INICIO_PUT")
            {
                return $"ERRO: Servidor nao iniciou PUT. Resposta: {ack}";
            }

            long tamanhoArquivo = new FileInfo(caminhoLocal).Length;
            
            byte[] tamanhoBytes = BitConverter.GetBytes(tamanhoArquivo);
            stream.Write(tamanhoBytes, 0, 8);

            using (FileStream fs = new FileStream(caminhoLocal, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[4096];
                int lido;
                while ((lido = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, lido);
                }
            }
            
            return ReceberResposta(stream);
        }
        catch (Exception ex)
        {
            return $"ERRO PUT: {ex.Message}";
        }
    }
    
    private static string ComandoGET(NetworkStream stream, string nomeArquivoRemoto)
    {
        try
        {
            string comandoEnvio = $"GET {nomeArquivoRemoto}";
            EnviarMensagem(stream, comandoEnvio);
            
            string ack = ReceberResposta(stream);
            if (ack == "ERRO: Arquivo remoto nao encontrado.")
            {
                return ack;
            }
            if (ack != "PRONTO_GET")
            {
                return $"ERRO: Servidor nao iniciou GET. Resposta: {ack}";
            }

            byte[] tamanhoBytes = new byte[8];
            stream.Read(tamanhoBytes, 0, 8);
            long tamanhoArquivo = BitConverter.ToInt64(tamanhoBytes, 0);

            string caminhoLocal = Path.Combine(_diretorioLocalAtual, nomeArquivoRemoto);
            Console.WriteLine($"(GET) Recebendo arquivo '{nomeArquivoRemoto}' ({tamanhoArquivo} bytes) para {caminhoLocal}...");

            using (FileStream fs = new FileStream(caminhoLocal, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[4096];
                long totalLido = 0;
                int lido;
                
                while (totalLido < tamanhoArquivo && (lido = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, tamanhoArquivo - totalLido))) > 0)
                {
                    fs.Write(buffer, 0, lido);
                    totalLido += lido;
                }
            }
            
            return ReceberResposta(stream);
        }
        catch (Exception ex)
        {
            return $"ERRO GET: {ex.Message}";
        }
    }

    private static void EnviarMensagem(NetworkStream stream, string mensagem)
    {
        byte[] bytesAEnviar = Encoding.ASCII.GetBytes(mensagem);
        stream.Write(bytesAEnviar, 0, bytesAEnviar.Length);
    }
    
    private static string ReceberResposta(NetworkStream stream)
    {
        byte[] bytesRecebidos = new byte[1024];
        int bytesLidos = stream.Read(bytesRecebidos, 0, bytesRecebidos.Length);
        return Encoding.ASCII.GetString(bytesRecebidos, 0, bytesLidos).Trim();
    }
}
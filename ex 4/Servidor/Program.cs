using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Servidor
{
    private static string _diretorioRemotoAtual = Environment.CurrentDirectory;

    public static void Main(string[] args)
    {
        const int porta = 5003;
        IPAddress ip = IPAddress.Parse("127.0.0.1");

        TcpListener listener = null;
        try
        {
            listener = new TcpListener(ip, porta);
            listener.Start();
            Console.WriteLine($"Servidor 4 (Mini-FTP) iniciado em {ip}:{porta}. Dir. inicial: {_diretorioRemotoAtual}");

            while (true)
            {
                TcpClient cliente = listener.AcceptTcpClient();
                Thread threadCliente = new Thread(HandleClientComm);
                threadCliente.Start(cliente);
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Erro de Socket: {e.Message}");
        }
        finally
        {
            listener?.Stop();
        }
    }

    private static void HandleClientComm(object clientObj)
    {
        TcpClient cliente = (TcpClient)clientObj;
        NetworkStream stream = null;

        try
        {
            stream = cliente.GetStream();
            byte[] bytesRecebidos = new byte[1024];
            int bytesLidos;

            while ((bytesLidos = stream.Read(bytesRecebidos, 0, bytesRecebidos.Length)) != 0)
            {
                string comandoCompleto = Encoding.ASCII.GetString(bytesRecebidos, 0, bytesLidos).Trim();
                string[] partes = comandoCompleto.Split(' ', 2);
                string comando = partes[0].ToUpper();
                string argumento = partes.Length > 1 ? partes[1] : string.Empty;

                string resposta = ProcessaComando(stream, comando, argumento);
                
                if (comando != "PUT" && comando != "GET")
                {
                    EnviarResposta(stream, resposta);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no tratamento do cliente: {ex.Message}");
        }
        finally
        {
            stream?.Close();
            cliente.Close();
            Console.WriteLine($"[CLIENTE {cliente.Client.RemoteEndPoint}] Conexao encerrada.");
        }
    }

    private static string ProcessaComando(NetworkStream stream, string comando, string argumento)
    {
        switch (comando)
        {
            case "CD":
                return ComandoCD(argumento);
            
            case "LS":
                return ComandoLS();

            case "PUT":
                return ComandoPUT(stream, argumento);

            case "GET":
                return ComandoGET(stream, argumento);
                
            default:
                return $"ERRO: Comando '{comando}' nao reconhecido. Use CD, LS, PUT ou GET.";
        }
    }
    
    private static string ComandoCD(string path)
    {
        try
        {
            string novoCaminho = Path.Combine(_diretorioRemotoAtual, path);
            if (path == "..")
            {
                novoCaminho = Directory.GetParent(_diretorioRemotoAtual)?.FullName ?? _diretorioRemotoAtual;
            }
            
            if (Directory.Exists(novoCaminho))
            {
                _diretorioRemotoAtual = Path.GetFullPath(novoCaminho);
                return $"OK: Diretorio remoto alterado para {_diretorioRemotoAtual}";
            }
            return "ERRO: Diretorio nao encontrado.";
        }
        catch (Exception ex)
        {
            return $"ERRO CD: {ex.Message}";
        }
    }
    
    private static string ComandoLS()
    {
        try
        {
            string[] arquivos = Directory.GetFiles(_diretorioRemotoAtual);
            string[] diretorios = Directory.GetDirectories(_diretorioRemotoAtual);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"OK: Conteudo de {_diretorioRemotoAtual}");
            
            foreach (string dir in diretorios)
            {
                sb.AppendLine($"[DIR] {Path.GetFileName(dir)}");
            }
            foreach (string file in arquivos)
            {
                sb.AppendLine($"[ARQ] {Path.GetFileName(file)}");
            }
            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"ERRO LS: {ex.Message}";
        }
    }

    private static string ComandoPUT(NetworkStream stream, string nomeArquivo)
    {
        try
        {
            string caminhoCompleto = Path.Combine(_diretorioRemotoAtual, nomeArquivo);
            Console.WriteLine($"(PUT) Recebendo arquivo: {caminhoCompleto}...");

            EnviarResposta(stream, "INICIO_PUT");

            byte[] tamanhoBytes = new byte[8];
            stream.Read(tamanhoBytes, 0, 8);
            long tamanhoArquivo = BitConverter.ToInt64(tamanhoBytes, 0);

            using (FileStream fs = new FileStream(caminhoCompleto, FileMode.Create, FileAccess.Write))
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
            
            string respostaFinal = $"OK: Arquivo '{nomeArquivo}' recebido com sucesso ({tamanhoArquivo} bytes).";
            EnviarResposta(stream, respostaFinal);
            return respostaFinal; 
        }
        catch (Exception ex)
        {
            EnviarResposta(stream, "ERRO_TRANSFERENCIA"); 
            return $"ERRO PUT: {ex.Message}";
        }
    }

    private static string ComandoGET(NetworkStream stream, string nomeArquivo)
    {
        try
        {
            string caminhoCompleto = Path.Combine(_diretorioRemotoAtual, nomeArquivo);
            if (!File.Exists(caminhoCompleto))
            {
                return "ERRO: Arquivo remoto nao encontrado.";
            }

            Console.WriteLine($"(GET) Enviando arquivo: {caminhoCompleto}...");
            
            long tamanhoArquivo = new FileInfo(caminhoCompleto).Length;
            
            byte[] tamanhoBytes = BitConverter.GetBytes(tamanhoArquivo);
            EnviarResposta(stream, "PRONTO_GET"); 
            stream.Write(tamanhoBytes, 0, 8);

            using (FileStream fs = new FileStream(caminhoCompleto, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[4096];
                int lido;
                while ((lido = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, lido);
                }
            }
            
            return $"OK: Arquivo '{nomeArquivo}' enviado com sucesso ({tamanhoArquivo} bytes).";
        }
        catch (Exception ex)
        {
            EnviarResposta(stream, "ERRO_TRANSFERENCIA"); 
            return $"ERRO GET: {ex.Message}";
        }
    }
    
    private static void EnviarResposta(NetworkStream stream, string mensagem)
    {
        byte[] bufferResposta = Encoding.ASCII.GetBytes(mensagem);
        stream.Write(bufferResposta, 0, bufferResposta.Length);
        stream.Flush();
    }
}
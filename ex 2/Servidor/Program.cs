
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.Json;

public class Servidor
{
    public static void Main(string[] args)
    {
        const int porta = 5001;
        IPAddress ip = IPAddress.Parse("127.0.0.1");

        TcpListener listener = null;
        try
        {
            listener = new TcpListener(ip, porta);
            listener.Start();
            Console.WriteLine($"Servidor 2 iniciado em {ip}:{porta}. Aguardando conexoes...");

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
            byte[] bytesRecebidos = new byte[4096];
            int bytesLidos = stream.Read(bytesRecebidos, 0, bytesRecebidos.Length);

            string jsonRecebido = Encoding.UTF8.GetString(bytesRecebidos, 0, bytesLidos);
            Quadrilatero q = JsonSerializer.Deserialize<Quadrilatero>(jsonRecebido);

            Console.WriteLine($"\n[CLIENTE {cliente.Client.RemoteEndPoint}] Objeto Recebido:");
            q.MostraDados();

            q.IndicaTipoQuadrilatero();
            Console.WriteLine($"[SERVICO] Tipo calculado: {q.TipoQuadrilatero}");

            string jsonResposta = JsonSerializer.Serialize(q);
            byte[] bufferResposta = Encoding.UTF8.GetBytes(jsonResposta);
            
            stream.Write(bufferResposta, 0, bufferResposta.Length);
            stream.Flush();
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
}
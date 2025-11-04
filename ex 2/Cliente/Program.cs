
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public class Cliente
{
    public static void Main(string[] args)
    {
        const string ipServidor = "127.0.0.1";
        const int portaServidor = 5001;

        TcpClient cliente = null;
        NetworkStream stream = null;

        try
        {
            cliente = new TcpClient(ipServidor, portaServidor);
            stream = cliente.GetStream();
            Console.WriteLine($"Conectado ao servidor 2 em {ipServidor}:{portaServidor}");

            Quadrilatero q = new Quadrilatero();
            Console.WriteLine("\nCLIENTE - Digite os lados do quadrilatero para analise:");
            q.LeDados();
            Console.WriteLine("\nObjeto antes do envio:");
            q.MostraDados();

            string jsonEnvio = JsonSerializer.Serialize(q);
            byte[] bytesAEnviar = Encoding.UTF8.GetBytes(jsonEnvio);
            
            stream.Write(bytesAEnviar, 0, bytesAEnviar.Length);
            Console.WriteLine("-> Objeto Quadrilatero enviado ao Servidor...");

            byte[] bytesRecebidos = new byte[4096];
            int bytesLidos = stream.Read(bytesRecebidos, 0, bytesRecebidos.Length);
            
            string jsonResposta = Encoding.UTF8.GetString(bytesRecebidos, 0, bytesLidos);
            
            Quadrilatero qAtualizado = JsonSerializer.Deserialize<Quadrilatero>(jsonResposta);
            
            Console.WriteLine("\n<- Objeto Atualizado recebido do Servidor:");
            qAtualizado.MostraDados();
        }
        catch (SocketException)
        {
            Console.WriteLine("Nao foi possivel conectar ao servidor. Verifique se o servidor 2 esta ativo na porta 5001.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu um erro: {ex.Message}");
        }
        finally
        {
            stream?.Close();
            cliente?.Close();
            Console.WriteLine("\nConexao com o servidor encerrada.");
        }
    }
}
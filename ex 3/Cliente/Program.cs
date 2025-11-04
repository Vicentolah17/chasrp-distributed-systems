using System;
using System.Net.Sockets;
using System.Text;

public class Cliente
{
    public static void Main(string[] args)
    {
        const string ipServidor = "127.0.0.1";
        const int portaServidor = 5002;

        TcpClient cliente = null;
        NetworkStream stream = null;

        try
        {
            cliente = new TcpClient(ipServidor, portaServidor);
            stream = cliente.GetStream();
            Console.WriteLine($"Conectado ao servidor de Estoque em {ipServidor}:{portaServidor}");

            while (true)
            {
                Console.WriteLine("\n--- NOVA OPERACAO ---");
                Console.Write("Produto (ex: 'Banana') ou 'terminar': ");
                string produto = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(produto)) continue;

                if (produto.ToLower() == "terminar")
                {
                    string msgFinalizacao = "terminar,0";
                    EnviarMensagem(stream, msgFinalizacao);
                    break;
                }

                Console.Write("Quantidade (positiva=entrada, negativa=saida): ");
                string strQuantidade = Console.ReadLine();

                if (!int.TryParse(strQuantidade, out int quantidade))
                {
                    Console.WriteLine("ERRO: Quantidade invalida.");
                    continue;
                }

                string mensagem = $"{produto},{quantidade}";

                EnviarMensagem(stream, mensagem);
                
                byte[] bytesRecebidos = new byte[1024];
                int bytesLidos = stream.Read(bytesRecebidos, 0, bytesRecebidos.Length);
                string resposta = Encoding.ASCII.GetString(bytesRecebidos, 0, bytesLidos);

                Console.WriteLine($"<- Resposta do Servidor: **{resposta}**");
                
                if (resposta.Contains("AVISO: Comando 'terminar'"))
                {
                    Console.WriteLine("Servidor finalizou. Encerrando cliente.");
                    break;
                }
            }
        }
        catch (SocketException)
        {
            Console.WriteLine("Nao foi possivel conectar ao servidor. Verifique se o servidor 3 esta ativo na porta 5002.");
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
    
    private static void EnviarMensagem(NetworkStream stream, string mensagem)
    {
        byte[] bytesAEnviar = Encoding.ASCII.GetBytes(mensagem);
        stream.Write(bytesAEnviar, 0, bytesAEnviar.Length);
        Console.WriteLine($"-> Enviado: {mensagem}");
    }
}
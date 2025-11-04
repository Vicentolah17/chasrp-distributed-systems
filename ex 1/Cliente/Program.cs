using System;
using System.Net.Sockets;
using System.Text;

public class Cliente
{
    public static void Main(string[] args)
    {
        Console.WriteLine("CLIENTE - Exercício 1");

        Console.Write("Digite o IP do servidor (ex: 127.0.0.1): ");
        string ipServidor = Console.ReadLine() ?? "127.0.0.1";
        
        Console.Write("Digite a Porta do servidor (ex: 5000): ");
        if (!int.TryParse(Console.ReadLine(), out int portaServidor))
        {
            portaServidor = 5000;
        }

        TcpClient cliente = null;
        NetworkStream stream = null;

        try
        {
            cliente = new TcpClient(ipServidor, portaServidor);
            stream = cliente.GetStream();
            Console.WriteLine($"✅ Conectado ao servidor em {ipServidor}:{portaServidor}");

            while (true)
            {
                Console.Write("\nDigite TRES números separados por virgula (ex: 10,25,5 ou -1,0,0 para sair): ");
                string mensagem = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(mensagem))
                {
                    continue;
                }

                
                byte[] bytesAEnviar = Encoding.ASCII.GetBytes(mensagem);
                stream.Write(bytesAEnviar, 0, bytesAEnviar.Length);
                Console.WriteLine($"-> Enviado: {mensagem}");

                byte[] bytesRecebidos = new byte[1024];
                int bytesLidos = stream.Read(bytesRecebidos, 0, bytesRecebidos.Length);
                string resposta = Encoding.ASCII.GetString(bytesRecebidos, 0, bytesLidos);

                
                Console.WriteLine($"<- Resposta do Servidor: **{resposta}**");
                
                
                if (resposta.Contains("AVISO: Comando de finalização"))
                {
                    Console.WriteLine("Servidor finalizou. Encerrando cliente.");
                    break;
                }
            }
        }
        catch (SocketException)
        {
            Console.WriteLine("❌ Não foi possivel conectar ao servidor. Verifique o IP/Porta e se o servidor esta ativo.");
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
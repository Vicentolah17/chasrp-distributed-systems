using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

public class Servidor
{
    private static bool _executando = true;

    public static void Main(string[] args)
    {
        
        const int porta = 5000;
        IPAddress ip = IPAddress.Parse("127.0.0.1"); 

        TcpListener listener = null;
        try
        {
            listener = new TcpListener(ip, porta);
            listener.Start();
            Console.WriteLine($"✅ Servidor iniciado em {ip}:{porta}. Aguardando conexões...");

            while (_executando)
            {
                if (listener.Pending())
                {
                    TcpClient cliente = listener.AcceptTcpClient();
                    
                    Thread threadCliente = new Thread(HandleClientComm);
                    threadCliente.Start(cliente);
                }
                else
                {
                    Thread.Sleep(100); 
                }
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Erro de Socket: {e.Message}");
        }
        finally
        {
            listener?.Stop();
            Console.WriteLine("🛑 Servidor finalizado.");
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
                string dados = Encoding.ASCII.GetString(bytesRecebidos, 0, bytesLidos);
                Console.WriteLine($"\n[CLIENTE {cliente.Client.RemoteEndPoint}] Dados Recebidos: {dados}");

                string[] strNumeros = dados.Split(',');
                int[] numeros;

                try
                {
                    numeros = strNumeros.Select(s => int.Parse(s.Trim())).ToArray();
                }
                catch (FormatException)
                {
                    string msgErro = "ERRO: Formato inválido. Envie três números separados por vírgula (ex: 10,20,30).";
                    EnviarResposta(stream, msgErro);
                    break; 
                }

                if (numeros.Length != 3)
                {
                    string msgErro = "ERRO: Por favor, envie exatamente TRÊS números.";
                    EnviarResposta(stream, msgErro);
                    continue; 
                }

                if (numeros[0] < 0)
                {
                    Console.WriteLine($"⚠️ Primeiro número negativo ({numeros[0]}). Finalizando o servidor...");
                    _executando = false; //  thread principal para parar
                    string msgAviso = "AVISO: Comando de finalização do servidor recebido.";
                    EnviarResposta(stream, msgAviso);
                    break;
                }

                int maior = numeros.Max();
                int menor = numeros.Min();

                string resposta = $"Maior: {maior}, Menor: {menor}";
                Console.WriteLine($"(Resposta enviada: {resposta})");
                EnviarResposta(stream, resposta);
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
            Console.WriteLine($"[CLIENTE {cliente.Client.RemoteEndPoint}] Conexão encerrada.");
        }
    }

    private static void EnviarResposta(NetworkStream stream, string mensagem)
    {
        byte[] bufferResposta = Encoding.ASCII.GetBytes(mensagem);
        stream.Write(bufferResposta, 0, bufferResposta.Length);
        stream.Flush();
    }
}
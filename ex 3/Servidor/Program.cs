using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Servidor
{
    private static Dictionary<string, int> _estoque = new Dictionary<string, int>();
    private static readonly object _lockEstoque = new object();
    private static bool _executando = true;

    public static void Main(string[] args)
    {
        const int porta = 5002;
        IPAddress ip = IPAddress.Parse("127.0.0.1");

        TcpListener listener = null;
        try
        {
            listener = new TcpListener(ip, porta);
            listener.Start();
            Console.WriteLine($"Servidor 3 (Estoque) iniciado em {ip}:{porta}. Aguardando conexoes...");

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
            Console.WriteLine("Servidor 3 (Estoque) finalizado.");
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
                string[] partes = dados.Split(',');

                if (partes.Length != 2)
                {
                    EnviarResposta(stream, "ERRO: Formato invalido. Envie 'Produto,Quantidade'.");
                    continue; 
                }

                string nomeProduto = partes[0].Trim();
                int quantidade;

                if (!int.TryParse(partes[1].Trim(), out quantidade))
                {
                    EnviarResposta(stream, "ERRO: Quantidade deve ser um numero inteiro.");
                    continue; 
                }

                Console.WriteLine($"\n[CLIENTE {cliente.Client.RemoteEndPoint}] Dados: {nomeProduto}, {quantidade}");

                
                if (nomeProduto.ToLower() == "terminar")
                {
                    _executando = false;
                    EnviarResposta(stream, "AVISO: Comando 'terminar' recebido. Servidor sera finalizado.");
                    break; 
                }

                string resposta = ProcessaOperacaoEstoque(nomeProduto, quantidade);
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
            Console.WriteLine($"[CLIENTE {cliente.Client.RemoteEndPoint}] Conexao encerrada.");
        }
    }

    private static string ProcessaOperacaoEstoque(string nomeProduto, int quantidade)
    {
        string resposta;
        
        lock (_lockEstoque)
        {
            if (quantidade > 0)
            {
                if (_estoque.ContainsKey(nomeProduto))
                {
                    _estoque[nomeProduto] += quantidade;
                    resposta = $"Estoque atualizado e quantidade de {nomeProduto} eh {_estoque[nomeProduto]}";
                }
                else
                {
                    _estoque.Add(nomeProduto, quantidade);
                    resposta = $"Produto {nomeProduto} cadastrado. Estoque atual: {_estoque[nomeProduto]}";
                }
            }
            else if (quantidade < 0)
            {
                int saida = Math.Abs(quantidade);
                
                if (_estoque.ContainsKey(nomeProduto))
                {
                    int quantidadeAtual = _estoque[nomeProduto];
                    
                    if (quantidadeAtual - saida >= 0)
                    {
                        _estoque[nomeProduto] -= saida;
                        resposta = $"Estoque atualizado e quantidade de {nomeProduto} eh {_estoque[nomeProduto]}";
                    }
                    else
                    {
                        resposta = $"Nao e possivel fazer a saida de estoque - quantidade menor que o valor desejado (Atual: {quantidadeAtual}).";
                    }
                }
                else
                {
                    resposta = "Produto inexistente.";
                }
            }
            else
            {
                resposta = "Operacao invalida. Quantidade deve ser positiva (entrada) ou negativa (saida).";
            }
        }
        
        return resposta;
    }

    private static void EnviarResposta(NetworkStream stream, string mensagem)
    {
        byte[] bufferResposta = Encoding.ASCII.GetBytes(mensagem);
        stream.Write(bufferResposta, 0, bufferResposta.Length);
        stream.Flush();
    }
}
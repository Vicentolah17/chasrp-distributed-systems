using System;
using System.Text.Json.Serialization;

public class Quadrilatero
{
    public double Lado1 { get; set; }
    public double Lado2 { get; set; }
    public double Lado3 { get; set; }
    public double Lado4 { get; set; }
    
    public string TipoQuadrilatero { get; set; } = "Indefinido"; 

    public void LeDados()
    {
        Console.Write("Digite o valor do Lado 1: ");
        double.TryParse(Console.ReadLine(), out double l1);
        Lado1 = l1;

        Console.Write("Digite o valor do Lado 2: ");
        double.TryParse(Console.ReadLine(), out double l2);
        Lado2 = l2;
        
        Console.Write("Digite o valor do Lado 3: ");
        double.TryParse(Console.ReadLine(), out double l3);
        Lado3 = l3;
        
        Console.Write("Digite o valor do Lado 4: ");
        double.TryParse(Console.ReadLine(), out double l4);
        Lado4 = l4;
    }

    public void IndicaTipoQuadrilatero()
    {
        if (Lado1 <= 0 || Lado2 <= 0 || Lado3 <= 0 || Lado4 <= 0)
        {
            TipoQuadrilatero = "Invalido (Lados devem ser positivos)";
        }
        else if (Lado1 == Lado2 && Lado2 == Lado3 && Lado3 == Lado4)
        {
            TipoQuadrilatero = "Quadrado";
        }
        else if (Lado1 == Lado3 && Lado2 == Lado4) 
        {
            TipoQuadrilatero = "Retangulo";
        }
        else
        {
            TipoQuadrilatero = "Quadrilatero (Geral)";
        }
    }

    public void MostraDados()
    {
        Console.WriteLine("--- DADOS DO QUADRILATERO ---");
        Console.WriteLine($"Lado 1: {Lado1}");
        Console.WriteLine($"Lado 2: {Lado2}");
        Console.WriteLine($"Lado 3: {Lado3}");
        Console.WriteLine($"Lado 4: {Lado4}");
        Console.WriteLine($"TIPO: {TipoQuadrilatero}");
        Console.WriteLine("-----------------------------");
    }
}
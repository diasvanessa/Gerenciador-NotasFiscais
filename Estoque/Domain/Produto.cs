using Estoque.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace Estoque.Domain;

public class Produto
{
    public Produto(string codigo, string descricao, int saldo, string? imagemUrl = null)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("Código é obrigatório.");
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.");
        if (saldo < 0)
            throw new ArgumentException("Saldo inicial não pode ser negativo.");

        Id = Guid.NewGuid();
        Codigo = codigo;
        Descricao = descricao;
        Saldo = saldo;
        ImagemUrl = imagemUrl;
        RowVersion = Guid.NewGuid().ToByteArray();
    }
    public Guid Id { get; init; }
    public string Codigo { get; private set; }
    public string Descricao { get; private set; }
    public int Saldo { get; private set; }
    public string? ImagemUrl { get; private set; }
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public void AtualizarImagem(string? imagemUrl)
    {
        ImagemUrl = imagemUrl;
    }

    public void BaixarSaldo(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");
        if (Saldo < quantidade)
            throw new SaldoInsuficienteException(Codigo, Saldo, quantidade);

        Saldo -= quantidade;
    }


    public void AdicionarSaldo(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade para estorno deve ser maior que zero.");

        Saldo += quantidade;
    }


}
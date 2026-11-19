namespace MeuEstudo.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma regra de negócio do domínio é violada.
/// Deve ser traduzida para um erro de requisição (ex.: HTTP 400/409) na camada de apresentação.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

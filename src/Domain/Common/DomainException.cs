namespace PixelRoute.Domain.Common;

/// <summary>Error que indica que se intentó violar una regla o invariante del negocio.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

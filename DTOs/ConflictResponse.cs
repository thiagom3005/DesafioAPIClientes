namespace DesafioAPIClientes.DTOs;

/// <summary>
/// Resposta de conflito (409 Conflict)
/// </summary>
public class ConflictResponse
{
    /// <summary>
    /// Mensagem descrevendo o conflito
    /// </summary>
    /// <example>Email já cadastrado.</example>
    public string Message { get; set; } = string.Empty;
}

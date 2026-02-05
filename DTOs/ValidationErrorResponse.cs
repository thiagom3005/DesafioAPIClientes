namespace DesafioAPIClientes.DTOs;

/// <summary>
/// Resposta de erro de validação (400 Bad Request)
/// </summary>
public class ValidationErrorResponse
{
    /// <summary>
    /// Mensagem geral do erro
    /// </summary>
    /// <example>Validation failed</example>
    public string Message { get; set; } = "Validation failed";

    /// <summary>
    /// Dicionário com erros por campo
    /// </summary>
    public Dictionary<string, List<string>> Errors { get; set; } = new();
}

namespace DesafioAPIClientes.DTOs;

/// <summary>
/// Dados do cliente retornado pela API
/// </summary>
public class ClienteResponse
{
    /// <summary>
    /// ID único do cliente
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Nome do cliente
    /// </summary>
    /// <example>Carlos Silva</example>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Email do cliente (normalizado em lowercase)
    /// </summary>
    /// <example>carlos@email.com</example>
    public string Email { get; set; } = string.Empty;
}

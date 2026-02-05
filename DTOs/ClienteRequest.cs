using System.ComponentModel.DataAnnotations;

namespace DesafioAPIClientes.DTOs;

/// <summary>
/// Dados para cadastro de cliente
/// </summary>
public class ClienteRequest
{
    /// <summary>
    /// Nome do cliente
    /// </summary>
    /// <example>Carlos Silva</example>
    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Email do cliente (deve ser único)
    /// </summary>
    /// <example>carlos@email.com</example>
    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; set; } = string.Empty;
}

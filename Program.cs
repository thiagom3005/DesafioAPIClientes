using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DesafioAPIClientes.Data;
using DesafioAPIClientes.DTOs;
using DesafioAPIClientes.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Configurar DbContext com SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "API de Clientes",
        Description = "API para gerenciamento de clientes com validação de email único"
    });

    // Incluir XML comments
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Aplicar document filter para adicionar metadata
    options.DocumentFilter<ClientesDocumentFilter>();

    // Debug: verificar se está sendo chamado
    Console.WriteLine("SwaggerGen configurado");
});

var app = builder.Build();

// Habilitar Swagger em ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Clientes v1");
    });
}

// POST /clientes - Cadastrar cliente
app.MapPost("/clientes", async (ClienteRequest request, AppDbContext db) =>
{
    // Validação manual dos campos
    var validationErrors = new Dictionary<string, List<string>>();

    if (string.IsNullOrWhiteSpace(request.Nome))
    {
        validationErrors["nome"] = ["Nome é obrigatório."];
    }

    if (string.IsNullOrWhiteSpace(request.Email))
    {
        validationErrors["email"] = ["Email é obrigatório."];
    }
    else if (!new EmailAddressAttribute().IsValid(request.Email))
    {
        validationErrors["email"] = ["Email inválido."];
    }

    if (validationErrors.Count > 0)
    {
        return Results.BadRequest(new ValidationErrorResponse
        {
            Message = "Validation failed",
            Errors = validationErrors
        });
    }

    // Normalizar email: trim + toLowerInvariant
    var emailNormalizado = request.Email.Trim().ToLowerInvariant();

    // Verificar duplicidade antes de salvar
    var emailExiste = await db.Clientes
        .AnyAsync(c => c.Email == emailNormalizado);

    if (emailExiste)
    {
        return Results.Conflict(new ConflictResponse { Message = "Email já cadastrado." });
    }

    // Criar entidade
    var cliente = new Cliente
    {
        Nome = request.Nome.Trim(),
        Email = emailNormalizado
    };

    db.Clientes.Add(cliente);

    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        // Tratar concorrência: se estourar unique constraint
        return Results.Conflict(new ConflictResponse { Message = "Email já cadastrado." });
    }

    // Retornar 201 Created com Location header
    var response = new ClienteResponse
    {
        Id = cliente.Id,
        Nome = cliente.Nome,
        Email = cliente.Email
    };

    return Results.Created($"/clientes/{cliente.Id}", response);
});

// GET /clientes - Listar clientes
app.MapGet("/clientes", async (AppDbContext db) =>
{
    var clientes = await db.Clientes
        .AsNoTracking()
        .OrderBy(c => c.Id)
        .Select(c => new ClienteResponse
        {
            Id = c.Id,
            Nome = c.Nome,
            Email = c.Email
        })
        .ToListAsync();

    return Results.Ok(clientes);
});

app.Run();

/// <summary>
/// Document Filter para configurar documentação completa dos endpoints
/// </summary>
public class ClientesDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        Console.WriteLine(">>> DocumentFilter.Apply() chamado!");

        // Garantir estrutura mínima do documento
        swaggerDoc.Paths ??= new OpenApiPaths();
        swaggerDoc.Components ??= new OpenApiComponents();
        swaggerDoc.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        // Definir schemas
        DefineSchemas(swaggerDoc);

        // Substituir path /clientes com documentação completa
        swaggerDoc.Paths["/clientes"] = CreateClientesPath();
    }

    private static void DefineSchemas(OpenApiDocument doc)
    {
        doc.Components.Schemas["ClienteRequest"] = new OpenApiSchema
        {
            Type = "object",
            Required = new HashSet<string> { "nome", "email" },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["nome"] = new() { Type = "string", Description = "Nome do cliente", Example = new OpenApiString("Carlos Silva") },
                ["email"] = new() { Type = "string", Format = "email", Description = "Email do cliente (deve ser único)", Example = new OpenApiString("carlos@email.com") }
            }
        };

        doc.Components.Schemas["ClienteResponse"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["id"] = new() { Type = "integer", Format = "int32", Description = "ID único do cliente", Example = new OpenApiInteger(1) },
                ["nome"] = new() { Type = "string", Description = "Nome do cliente", Example = new OpenApiString("Carlos Silva") },
                ["email"] = new() { Type = "string", Description = "Email do cliente (normalizado em lowercase)", Example = new OpenApiString("carlos@email.com") }
            }
        };

        doc.Components.Schemas["ValidationErrorResponse"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["message"] = new() { Type = "string", Description = "Mensagem geral do erro", Example = new OpenApiString("Validation failed") },
                ["errors"] = new()
                {
                    Type = "object",
                    Description = "Dicionário com erros por campo",
                    AdditionalProperties = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema { Type = "string" }
                    }
                }
            }
        };

        doc.Components.Schemas["ConflictResponse"] = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["message"] = new() { Type = "string", Description = "Mensagem descrevendo o conflito", Example = new OpenApiString("Email já cadastrado.") }
            }
        };
    }

    private static OpenApiPathItem CreateClientesPath()
    {
        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = CreatePostOperation(),
                [OperationType.Get] = CreateGetOperation()
            }
        };
    }

    private static OpenApiOperation CreatePostOperation()
    {
        return new OpenApiOperation
        {
            Tags = [new OpenApiTag { Name = "Clientes" }],
            Summary = "Cadastra um novo cliente",
            Description = "Cria um novo cliente com nome e email. O email deve ser único e será normalizado (trim + lowercase).",
            OperationId = "CriarCliente",
            RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new()
                    {
                        Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ClienteRequest" } },
                        Examples = new Dictionary<string, OpenApiExample>
                        {
                            ["Cliente válido"] = new()
                            {
                                Summary = "Exemplo de cliente válido",
                                Value = new OpenApiObject
                                {
                                    ["nome"] = new OpenApiString("Carlos Silva"),
                                    ["email"] = new OpenApiString("carlos@email.com")
                                }
                            }
                        }
                    }
                }
            },
            Responses = new OpenApiResponses
            {
                ["201"] = new OpenApiResponse
                {
                    Description = "Cliente criado com sucesso",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ClienteResponse" } },
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["Cliente criado"] = new()
                                {
                                    Summary = "Cliente criado com sucesso",
                                    Value = new OpenApiObject
                                    {
                                        ["id"] = new OpenApiInteger(1),
                                        ["nome"] = new OpenApiString("Carlos Silva"),
                                        ["email"] = new OpenApiString("carlos@email.com")
                                    }
                                }
                            }
                        }
                    }
                },
                ["400"] = new OpenApiResponse
                {
                    Description = "Erro de validação",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ValidationErrorResponse" } },
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["Email inválido"] = new()
                                {
                                    Summary = "Erro de validação - email inválido",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Validation failed"),
                                        ["errors"] = new OpenApiObject
                                        {
                                            ["email"] = new OpenApiArray { new OpenApiString("Email inválido.") }
                                        }
                                    }
                                },
                                ["Campos obrigatórios"] = new()
                                {
                                    Summary = "Erro de validação - campos vazios",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Validation failed"),
                                        ["errors"] = new OpenApiObject
                                        {
                                            ["nome"] = new OpenApiArray { new OpenApiString("Nome é obrigatório.") },
                                            ["email"] = new OpenApiArray { new OpenApiString("Email é obrigatório.") }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["409"] = new OpenApiResponse
                {
                    Description = "Email já cadastrado",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ConflictResponse" } },
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["Email duplicado"] = new()
                                {
                                    Summary = "Email já cadastrado",
                                    Value = new OpenApiObject
                                    {
                                        ["message"] = new OpenApiString("Email já cadastrado.")
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private static OpenApiOperation CreateGetOperation()
    {
        return new OpenApiOperation
        {
            Tags = [new OpenApiTag { Name = "Clientes" }],
            Summary = "Lista todos os clientes",
            Description = "Retorna a lista de todos os clientes cadastrados, ordenados por ID.",
            OperationId = "ListarClientes",
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "Lista de clientes",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ClienteResponse" } }
                            },
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                ["Lista com clientes"] = new()
                                {
                                    Summary = "Lista com clientes cadastrados",
                                    Value = new OpenApiArray
                                    {
                                        new OpenApiObject
                                        {
                                            ["id"] = new OpenApiInteger(1),
                                            ["nome"] = new OpenApiString("Carlos Silva"),
                                            ["email"] = new OpenApiString("carlos@email.com")
                                        },
                                        new OpenApiObject
                                        {
                                            ["id"] = new OpenApiInteger(2),
                                            ["nome"] = new OpenApiString("Maria Santos"),
                                            ["email"] = new OpenApiString("maria@email.com")
                                        }
                                    }
                                },
                                ["Lista vazia"] = new()
                                {
                                    Summary = "Nenhum cliente cadastrado",
                                    Value = new OpenApiArray()
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}

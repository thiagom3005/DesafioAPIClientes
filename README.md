# API de Clientes (.NET 9)

API simples para **cadastrar** e **listar** clientes, com **validação** e **garantia de email único**.  
Implementada em **.NET 9 (Minimal API)** com **EF Core + SQLite** e documentação via **Swagger**.

---

## ✅ Funcionalidades

- **POST /clientes**: cadastra um novo cliente (nome + email)
- **GET /clientes**: lista todos os clientes cadastrados (ordenados por Id)
- Validações:
  - `nome` obrigatório
  - `email` obrigatório e com formato válido
  - `email` único (normalizado com `trim + lowercase`)
- Persistência local com **SQLite** (`clientes.db`)
- Swagger UI disponível para testar a API

---

## 🧰 Tecnologias

- .NET 9 (Minimal API)
- Entity Framework Core 9
- SQLite
- Swashbuckle (Swagger/OpenAPI)

---

## ▶️ Como rodar localmente

### Pré-requisitos
- **.NET SDK 9** instalado
- (Opcional) `dotnet-ef` se você quiser gerenciar migrations:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### Passo a passo

1) Restaurar dependências:
```bash
dotnet restore
```

2) Aplicar migrations / criar banco (se necessário):
```bash
dotnet ef database update
```

> Observação: o arquivo do banco será criado/atualizado como `clientes.db` na raiz do projeto (ou conforme configuração).

3) Rodar a aplicação:
```bash
dotnet run
```

---

## 🔎 Swagger

Com a aplicação rodando, acesse:

- **Swagger UI:** `http://localhost:5221/swagger`
- **OpenAPI JSON:** `http://localhost:5221/swagger/v1/swagger.json`

> A porta pode variar conforme seu `launchSettings.json`. Ajuste se necessário.

---

## 📌 Endpoints

### 1) Cadastrar cliente
**POST** `/clientes`

**Body**
```json
{
  "nome": "Carlos Silva",
  "email": "carlos@email.com"
}
```

**Sucesso (201 Created)**
- Retorna o cliente criado
- Envia header `Location: /clientes/{id}`

**Response**
```json
{
  "id": 1,
  "nome": "Carlos Silva",
  "email": "carlos@email.com"
}
```

**Erros**

- **400 Bad Request (validação)**  
Formato padrão:
```json
{
  "message": "Validation failed",
  "errors": {
    "email": ["Email inválido."]
  }
}
```

Exemplo com campos obrigatórios:
```json
{
  "message": "Validation failed",
  "errors": {
    "nome": ["Nome é obrigatório."],
    "email": ["Email é obrigatório."]
  }
}
```

- **409 Conflict (email duplicado)**
```json
{
  "message": "Email já cadastrado."
}
```

---

### 2) Listar clientes
**GET** `/clientes`

**Sucesso (200 OK)**
```json
[
  { "id": 1, "nome": "Carlos Silva", "email": "carlos@email.com" },
  { "id": 2, "nome": "Maria", "email": "maria@email.com" }
]
```

---

## 🧪 Testes rápidos com curl

### POST (sucesso)
```bash
curl -i -X POST "http://localhost:5221/clientes"   -H "Content-Type: application/json"   -d "{"nome":"Carlos Silva","email":"carlos@email.com"}"
```

### POST (email duplicado)
```bash
curl -i -X POST "http://localhost:5221/clientes"   -H "Content-Type: application/json"   -d "{"nome":"Outro Nome","email":"carlos@email.com"}"
```

### POST (validação - email inválido)
```bash
curl -i -X POST "http://localhost:5221/clientes"   -H "Content-Type: application/json"   -d "{"nome":"Carlos","email":"invalido"}"
```

### GET
```bash
curl -i "http://localhost:5221/clientes"
```

---

## 🧠 Decisões técnicas

- **SQLite**: escolhido por ser leve, local e ideal para um desafio simples (zero dependência externa, fácil de rodar).
- **Email único**: o email é **normalizado** (`trim + lowercase`) antes de salvar e há proteção contra duplicidade retornando **409 Conflict**.
- **Sem overengineering**: estrutura propositalmente simples (Minimal API + EF Core) para atender o escopo sem complexidade desnecessária.

---

## ✅ Checklist de entrega

- [ ] `dotnet restore` ok  
- [ ] `dotnet ef database update` ok  
- [ ] `dotnet run` ok  
- [ ] Swagger abre em `/swagger`  
- [ ] POST /clientes retorna 201/400/409 corretamente  
- [ ] GET /clientes retorna lista ordenada por Id  
- [ ] Email normalizado e unicidade garantida  

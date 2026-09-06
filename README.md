# 🏭 ShelfApi — Backend ERP de Gestão Industrial & Manufatura de Kits

API REST corporativa desenvolvida em **.NET 9 / C# 13** com **Dapper** e **MySQL**, projetada para controle completo de almoxarifado, rastreabilidade e auditoria de estoque, manufatura e montagem atômica de kits, motor de simulação de produção multi-modelos e autenticação baseada em perfis (RBAC).

---

## 🚀 Visão Geral da Arquitetura

A `ShelfApi` adota uma arquitetura em camadas orientada a serviços e repositórios (Service-Repository Pattern), focada em alta performance, consistência transacional e separação estrita de responsabilidades:

- **Controllers (`Controllers/`)**: Exposição de endpoints REST com validações de entrada, captura de exceções de domínio e injeção de identidade JWT.
- **Services (`Services/`)**: Regras de negócio, cálculos de capacidade de montagem (BOM), validações de consistência e orquestração.
- **Repositories (`Repositories/`)**: Camada de persistência otimizada com **Dapper**, utilizando consultas parametrizadas contra SQL Injection e controle de transações ACID (`IDbTransaction`).
- **Models & DTOs (`Models/`)**: Entidades de banco, objetos de transferência tipados para requisições, respostas analíticas e relatórios.
- **Security (`Infrastructure/`, `Exceptions/`)**: Autenticação via JWT Bearer, hashing de senhas com BCrypt e tratamento centralizado de exceções.

---

## 🛠️ Tecnologias Utilizadas

- **Runtime & Linguagem**: .NET 9.0 / C# 13
- **Micro-ORM**: Dapper 2.1+
- **Banco de Dados**: MySQL 8.0+ / MariaDB via `MySql.Data`
- **Segurança & Criptografia**: JWT (`System.IdentityModel.Tokens.Jwt`), BCrypt.Net-Next
- **Documentação Interativa**: Swagger / OpenAPI (Swashbuckle)
- **Servidor Web**: Kestrel com suporte a HTTPS e CORS configurado para o frontend Vite (`ShelfApp`)

---

## 📦 Principais Funcionalidades

### 1. Linha de Montagem Atômica de Kits (Transações ACID)
- **Montagem de Kits (`POST /api/EstoqueKit/Montar`)**:
  - Em uma única transação atômica (`db.BeginTransaction()`):
    1. Bloqueia e valida os saldos de cada insumo no almoxarifado (`estoque.quantidade >= qtdNecessaria * qtdKits`).
    2. Deduz os insumos da tabela `estoque` e grava registro de auditoria na `movimentacao_estoque` (`tipo = 'SAIDA'`).
    3. Incrementa o saldo do kit pronto na tabela `estoque_kit`.
    4. Registra o evento auditável na tabela `movimentacao_kit` (`tipo = 'ENTRADA'`).
- **Desmontagem Atômica de Kits (`POST /api/EstoqueKit/Desmontar`)**:
  - Estorna kits prontos para o almoxarifado:
    1. Valida e debita o saldo físico do kit em `estoque_kit`.
    2. Devolve rigorosamente cada componente associado de volta à tabela `estoque`.
    3. Registra logs reversos em ambas as tabelas de movimentação com justificativa informada.

### 2. Motor de Simulação de Produção Multi-Kits (`POST /api/EstoqueKit/Simular`)
- Recebe uma fila com múltiplos kits e quantidades arbitrárias planejadas (ex: Kit A: 10 un, Kit B: 5 un, Kit C: 2 un).
- Consolida e soma a demanda agregada de cada peça (`BOM agregada`), confrontando com o saldo físico em tempo real.
- Aponta se a produção é 100% viável ou detalha exatamente quais peças estão em déficit e quantas unidades faltam comprar.
- Suporta rastreabilidade de origem (quais kits usam cada peça) e segregação de componentes de **Painel Elétrico** vs **Estrutura/Gerais**.

### 3. Controle e Auditoria Completa de Almoxarifado
- Movimentações manuais de entrada e saída com operador responsável, saldo anterior, saldo posterior, data/hora e justificativa obrigatória.
- Consulta de histórico cronológico paginado para atendimento a normas de auditoria e compliance.

### 4. Gestão Integrada
- Cadastro de Fornecedores homologados.
- Catálogo de Itens e Peças vinculadas a fornecedores.
- Cadastro de Kits e Composição de peças (com indicação de componentes de painel `isPainel`).
- Softwares industriais, controle de licenças e versões para CLPs.
- Gestão de Usuários com permissões por perfil (`Administrador` vs `Operador`).

---

## 🗄️ Esquema do Banco de Dados (MySQL DDL)

Para inicializar a base de dados `stock`, execute o script DDL abaixo no seu servidor MySQL:

```sql
CREATE DATABASE IF NOT EXISTS stock CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE stock;

-- 1. Fornecedores
CREATE TABLE IF NOT EXISTS Fornecedor (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(255) NOT NULL
);

-- 2. Itens / Peças de Estoque
CREATE TABLE IF NOT EXISTS Item (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(255) NOT NULL,
  FornecedorId INT NOT NULL,
  Descricao TEXT,
  FOREIGN KEY (FornecedorId) REFERENCES Fornecedor(Id)
);

-- 3. Saldo de Estoque de Itens
CREATE TABLE IF NOT EXISTS estoque (
  itemId INT PRIMARY KEY,
  quantidade INT NOT NULL DEFAULT 0,
  FOREIGN KEY (itemId) REFERENCES Item(Id) ON DELETE CASCADE
);

-- 4. Histórico de Movimentações de Itens (Auditoria)
CREATE TABLE IF NOT EXISTS movimentacao_estoque (
  id INT AUTO_INCREMENT PRIMARY KEY,
  itemId INT NOT NULL,
  tipo ENUM('Entrada', 'Saida') NOT NULL,
  quantidade INT NOT NULL,
  saldoAnterior INT NOT NULL,
  saldoPosterior INT NOT NULL,
  motivo VARCHAR(255),
  usuarioId INT NULL,
  usuarioNome VARCHAR(100) NOT NULL DEFAULT 'Sistema',
  dataMovimentacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (itemId) REFERENCES Item(Id) ON DELETE CASCADE
);

-- 5. Kits de Produtos
CREATE TABLE IF NOT EXISTS Kit (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(255) NOT NULL
);

-- 6. Composição do Kit (BOM)
CREATE TABLE IF NOT EXISTS KitItem (
  KitId INT NOT NULL,
  ItemId INT NOT NULL,
  Quantidade INT NOT NULL DEFAULT 1,
  IsPainel TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (KitId, ItemId),
  FOREIGN KEY (KitId) REFERENCES Kit(Id) ON DELETE CASCADE,
  FOREIGN KEY (ItemId) REFERENCES Item(Id) ON DELETE CASCADE
);

-- 7. Saldo Físico de Kits Prontos
CREATE TABLE IF NOT EXISTS estoque_kit (
  kitId INT PRIMARY KEY,
  quantidade INT NOT NULL DEFAULT 0,
  FOREIGN KEY (kitId) REFERENCES Kit(Id) ON DELETE CASCADE
);

-- 8. Histórico de Movimentações de Kits (Auditoria de Montagem)
CREATE TABLE IF NOT EXISTS movimentacao_kit (
  id INT AUTO_INCREMENT PRIMARY KEY,
  kitId INT NOT NULL,
  tipo VARCHAR(50) NOT NULL,
  quantidade INT NOT NULL,
  saldoAnterior INT NOT NULL,
  saldoPosterior INT NOT NULL,
  motivo VARCHAR(255),
  usuarioId INT NULL,
  usuarioNome VARCHAR(100) NOT NULL DEFAULT 'Sistema',
  dataMovimentacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (kitId) REFERENCES Kit(Id) ON DELETE CASCADE
);

-- 9. Softwares Industriais & Licenças de CLP
CREATE TABLE IF NOT EXISTS Software (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(255) NOT NULL,
  Versao VARCHAR(50) NOT NULL,
  Fabricante VARCHAR(255),
  Licenca VARCHAR(100),
  DataValidade DATE NULL
);

-- 10. Usuários e Controle de Acesso
CREATE TABLE IF NOT EXISTS Usuario (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Nome VARCHAR(100) NOT NULL,
  Email VARCHAR(150) NOT NULL UNIQUE,
  SenhaHash VARCHAR(255) NOT NULL,
  Role VARCHAR(50) NOT NULL DEFAULT 'Operador',
  CriadoEm DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

---

## 📡 Catálogo de Endpoints da API

### 🔐 Autenticação & Usuários (`api/Auth`)
| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `POST` | `/api/Auth/login` | Autentica usuário e retorna token JWT |
| `POST` | `/api/Auth/register` | Cadastra novo usuário (Restrito para Administrador) |
| `GET` | `/api/Auth/users` | Lista todos os usuários cadastrados |
| `PUT` | `/api/Auth/users/{id}` | Atualiza dados e perfil de acesso |
| `DELETE`| `/api/Auth/users/{id}` | Remove usuário |

### 📦 Estoque & Movimentações (`api/Estoque`)
| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/api/Estoque/GetSaldo?itemId={id}` | Retorna saldo físico do item |
| `POST` | `/api/Estoque/Movimentar` | Registra entrada ou saída manual com justificativa e operador |
| `GET` | `/api/Estoque/Historico` | Consulta histórico de auditoria de movimentações |

### 🔨 Kits Estocáveis & Linha de Montagem (`api/EstoqueKit`)
| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/api/EstoqueKit/Saldos` | Lista todos os kits com saldo físico e capacidade teórica da BOM |
| `GET` | `/api/EstoqueKit/Saldo/{kitId}` | Detalhes do kit com status de cada componente |
| `POST` | `/api/EstoqueKit/Montar` | **Montagem Atômica**: baixa insumos e credita kits prontos |
| `POST` | `/api/EstoqueKit/Desmontar`| **Desmontagem Atômica**: estorna kits e devolve insumos ao estoque |
| `POST` | `/api/EstoqueKit/Movimentar`| Entrada ou saída manual avulsa de kits |
| `GET` | `/api/EstoqueKit/Historico` | Consulta histórico de auditoria de kits |
| `POST` | `/api/EstoqueKit/Simular` | **Simulador de Produção**: calcula demanda agregada de insumos para múltiplos kits |

### ⚙️ Gestão Cadastral
- **Itens (`api/Item`)**: `GetAllItems`, `GetItemById`, `CreateItem`, `UpdateItem`, `DeleteItem`.
- **Kits (`api/Kit`)**: `GetAll`, `GetById`, `Create`, `Update`, `Delete`.
- **Composições (`api/KitItem`)**: `GetAll`, `GetByKit`, `GetById`, `Create`, `Update`, `Delete`.
- **Fornecedores (`api/Fornecedor`)**: `GetAll`, `GetById`, `Create`, `Update`, `Delete`.
- **Softwares (`api/Software`)**: `GetAll`, `GetById`, `Create`, `Update`, `Delete`.

---

## ⚙️ Configuração & Execução

### Pré-requisitos
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) instalado.
- Servidor **MySQL 8.0+** ativo na máquina ou rede.

### 1. Configurar Conexão com o Banco
Abra o arquivo `appsettings.json` e ajuste a string de conexão:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=stock;Uid=seu_usuario;Pwd=sua_senha;Port=3306;"
  },
  "Jwt": {
    "Key": "SuaChaveSecretaSuperSeguraParaAssinarTokensJWT2026!",
    "Issuer": "ShelfApi",
    "Audience": "ShelfApp"
  }
}
```

### 2. Restaurar, Compilar e Executar

Execute os comandos no terminal dentro da pasta `ShelfApi`:

```bash
# Restaurar pacotes NuGet
dotnet restore

# Compilar a solução
dotnet build

# Iniciar a API em modo desenvolvimento
dotnet run
```

A API iniciará por padrão em:
- **HTTPS**: `https://localhost:7024`
- **HTTP**: `http://localhost:5029`
- **Swagger UI**: `https://localhost:7024/swagger`
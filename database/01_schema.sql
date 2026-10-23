/*
  Cria o banco "Biblioteca" com tabelas, regras de integridade, índices e uma view.
  Pode ser executado mais de uma vez: só cria o que ainda não existe.

  Como executar:
    - SQL Server Management Studio (SSMS) ou Azure Data Studio: abra este arquivo e execute (F5), ou
    - Terminal: sqlcmd -S "(localdb)\MSSQLLocalDB" -I -i database/01_schema.sql
*/

IF DB_ID(N'Biblioteca') IS NULL
    CREATE DATABASE Biblioteca;
GO

USE Biblioteca;
GO

-- Índices filtrados e views exigem estas opções ligadas. O SSMS já liga; o sqlcmd não (a menos que use -I).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ---------------------------------------------------------------------------
-- Livros: um título do acervo, com a quantidade total de exemplares.
-- Quantos estão emprestados NÃO é guardado aqui (seria um dado duplicado que pode ficar
-- errado); é calculado a partir de Emprestimos, na view vw_LivrosComDisponibilidade.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Livros', N'U') IS NULL
CREATE TABLE dbo.Livros
(
    Id              INT IDENTITY(1, 1) NOT NULL,
    Titulo          NVARCHAR(150)      NOT NULL,
    Autor           NVARCHAR(100)      NOT NULL,
    Isbn            CHAR(13)           NOT NULL,
    AnoPublicacao   SMALLINT           NOT NULL,
    QuantidadeTotal INT                NOT NULL,

    CONSTRAINT PK_Livros PRIMARY KEY (Id),
    CONSTRAINT UQ_Livros_Isbn UNIQUE (Isbn),
    CONSTRAINT CK_Livros_Isbn CHECK (Isbn NOT LIKE '%[^0-9]%'),            -- só dígitos
    CONSTRAINT CK_Livros_Ano CHECK (AnoPublicacao BETWEEN 1400 AND 2100),
    CONSTRAINT CK_Livros_Quantidade CHECK (QuantidadeTotal BETWEEN 1 AND 1000)
);
GO

-- ---------------------------------------------------------------------------
-- Membros: pessoas cadastradas. O e-mail é único.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Membros', N'U') IS NULL
CREATE TABLE dbo.Membros
(
    Id           INT IDENTITY(1, 1) NOT NULL,
    Nome         NVARCHAR(100)      NOT NULL,
    Email        NVARCHAR(150)      NOT NULL,
    DataCadastro DATE               NOT NULL CONSTRAINT DF_Membros_DataCadastro DEFAULT (CAST(GETDATE() AS DATE)),

    CONSTRAINT PK_Membros PRIMARY KEY (Id),
    CONSTRAINT UQ_Membros_Email UNIQUE (Email)
);
GO

-- ---------------------------------------------------------------------------
-- Emprestimos: liga um livro a um membro. Enquanto DataDevolucao for NULL, está em andamento.
-- Relacionamentos: Livros 1 --- N Emprestimos N --- 1 Membros.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Emprestimos', N'U') IS NULL
CREATE TABLE dbo.Emprestimos
(
    Id                    INT IDENTITY(1, 1) NOT NULL,
    LivroId               INT                NOT NULL,
    MembroId              INT                NOT NULL,
    DataEmprestimo        DATE               NOT NULL,
    DataPrevistaDevolucao DATE               NOT NULL,
    DataDevolucao         DATE               NULL,
    Multa                 DECIMAL(10, 2)     NOT NULL CONSTRAINT DF_Emprestimos_Multa DEFAULT (0),

    CONSTRAINT PK_Emprestimos PRIMARY KEY (Id),
    CONSTRAINT FK_Emprestimos_Livros FOREIGN KEY (LivroId) REFERENCES dbo.Livros (Id),
    CONSTRAINT FK_Emprestimos_Membros FOREIGN KEY (MembroId) REFERENCES dbo.Membros (Id),
    CONSTRAINT CK_Emprestimos_Prazo CHECK (DataPrevistaDevolucao >= DataEmprestimo),
    CONSTRAINT CK_Emprestimos_Devolucao CHECK (DataDevolucao IS NULL OR DataDevolucao >= DataEmprestimo),
    CONSTRAINT CK_Emprestimos_Multa CHECK (Multa >= 0)
);
GO

-- Índices filtrados: só indexam os empréstimos em andamento, que são os consultados o tempo todo
-- (quantos exemplares estão fora, quantos livros um membro tem, quais estão atrasados).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Emprestimos_Livro_EmAndamento' AND object_id = OBJECT_ID(N'dbo.Emprestimos'))
    CREATE INDEX IX_Emprestimos_Livro_EmAndamento ON dbo.Emprestimos (LivroId) WHERE DataDevolucao IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Emprestimos_Membro_EmAndamento' AND object_id = OBJECT_ID(N'dbo.Emprestimos'))
    CREATE INDEX IX_Emprestimos_Membro_EmAndamento ON dbo.Emprestimos (MembroId) INCLUDE (DataPrevistaDevolucao) WHERE DataDevolucao IS NULL;
GO

-- ---------------------------------------------------------------------------
-- View: cada livro com quantos exemplares estão emprestados e quantos estão disponíveis.
-- O programa lê esta view na listagem de livros.
-- ---------------------------------------------------------------------------
CREATE OR ALTER VIEW dbo.vw_LivrosComDisponibilidade
AS
SELECT
    l.Id,
    l.Titulo,
    l.Autor,
    l.Isbn,
    l.AnoPublicacao,
    l.QuantidadeTotal,
    COUNT(e.Id)                      AS Emprestados,
    l.QuantidadeTotal - COUNT(e.Id)  AS Disponiveis
FROM dbo.Livros AS l
LEFT JOIN dbo.Emprestimos AS e
       ON e.LivroId = l.Id
      AND e.DataDevolucao IS NULL
GROUP BY l.Id, l.Titulo, l.Autor, l.Isbn, l.AnoPublicacao, l.QuantidadeTotal;
GO

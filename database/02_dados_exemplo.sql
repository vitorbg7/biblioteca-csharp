/*
  Dados de exemplo para testar o programa. As datas são relativas ao dia em que o script roda,
  então sempre há um empréstimo atrasado e um histórico já devolvido (um deles com multa).
  Só insere se o banco estiver vazio. Execute depois do 01_schema.sql.
*/

USE Biblioteca;
GO

-- Necessário porque Emprestimos tem índices filtrados (o sqlcmd deixa esta opção desligada por padrão).
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM dbo.Livros)
BEGIN
    PRINT N'O banco já tem dados; nada foi inserido.';
    RETURN;
END;

DECLARE @Hoje DATE = CAST(GETDATE() AS DATE);

INSERT INTO dbo.Livros (Titulo, Autor, Isbn, AnoPublicacao, QuantidadeTotal)
VALUES (N'Clean Code',                N'Robert C. Martin', '9780132350884', 2008, 2),
       (N'Design Patterns',           N'Erich Gamma',      '9780201633610', 1994, 1),
       (N'Effective Java',            N'Joshua Bloch',     '9780134685991', 2018, 3),
       (N'The Pragmatic Programmer',  N'David Thomas',     '9780135957059', 2019, 2),
       (N'Refactoring',               N'Martin Fowler',    '9780134757599', 2018, 1),
       (N'Introduction to Algorithms', N'Thomas H. Cormen', '9780262033848', 2009, 2);

INSERT INTO dbo.Membros (Nome, Email, DataCadastro)
VALUES (N'Ana Souza',   'ana.souza@exemplo.com',   DATEADD(DAY, -90, @Hoje)),
       (N'Bruno Lima',  'bruno.lima@exemplo.com',  DATEADD(DAY, -60, @Hoje)),
       (N'Carla Dias',  'carla.dias@exemplo.com',  DATEADD(DAY, -30, @Hoje)),
       (N'Diego Rocha', 'diego.rocha@exemplo.com', DATEADD(DAY, -5,  @Hoje));

-- Os Ids são buscados pelo ISBN e pelo e-mail em vez de assumir que começam em 1.
DECLARE @CleanCode      INT = (SELECT Id FROM dbo.Livros WHERE Isbn = '9780132350884');
DECLARE @DesignPatterns INT = (SELECT Id FROM dbo.Livros WHERE Isbn = '9780201633610');
DECLARE @EffectiveJava  INT = (SELECT Id FROM dbo.Livros WHERE Isbn = '9780134685991');
DECLARE @Pragmatico     INT = (SELECT Id FROM dbo.Livros WHERE Isbn = '9780135957059');

DECLARE @Ana   INT = (SELECT Id FROM dbo.Membros WHERE Email = 'ana.souza@exemplo.com');
DECLARE @Bruno INT = (SELECT Id FROM dbo.Membros WHERE Email = 'bruno.lima@exemplo.com');
DECLARE @Carla INT = (SELECT Id FROM dbo.Membros WHERE Email = 'carla.dias@exemplo.com');

-- Prazo de 14 dias. Os dois primeiros já foram devolvidos (o segundo com 8 dias de atraso: R$ 8,00).
INSERT INTO dbo.Emprestimos (LivroId, MembroId, DataEmprestimo, DataPrevistaDevolucao, DataDevolucao, Multa)
VALUES (@DesignPatterns, @Carla,  DATEADD(DAY, -40, @Hoje), DATEADD(DAY, -26, @Hoje), DATEADD(DAY, -30, @Hoje), 0),
       (@Pragmatico,     @Carla,  DATEADD(DAY, -40, @Hoje), DATEADD(DAY, -26, @Hoje), DATEADD(DAY, -18, @Hoje), 8.00),
       (@CleanCode,      @Ana,    DATEADD(DAY, -20, @Hoje), DATEADD(DAY, -6,  @Hoje), NULL, 0),   -- atrasado
       (@EffectiveJava,  @Bruno,  DATEADD(DAY, -5,  @Hoje), DATEADD(DAY, 9,   @Hoje), NULL, 0),
       (@CleanCode,      @Bruno,  DATEADD(DAY, -3,  @Hoje), DATEADD(DAY, 11,  @Hoje), NULL, 0);   -- os 2 exemplares ficam fora

PRINT N'Dados de exemplo inseridos.';
GO

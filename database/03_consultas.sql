/*
  Consultas de exemplo sobre o banco da biblioteca.
  Cada consulta é independente: selecione uma e execute (F5 no SSMS).
*/

USE Biblioteca;
GO

-- 1) Livros com a quantidade de exemplares disponíveis (usa a view).
SELECT Titulo, Autor, QuantidadeTotal, Emprestados, Disponiveis
FROM dbo.vw_LivrosComDisponibilidade
ORDER BY Titulo;

-- 2) Empréstimos atrasados, com dias de atraso e multa estimada (R$ 1,00 por dia).
--    Junta as três tabelas (INNER JOIN).
SELECT e.Id                                                                AS Emprestimo,
       l.Titulo,
       m.Nome                                                              AS Membro,
       e.DataPrevistaDevolucao,
       DATEDIFF(DAY, e.DataPrevistaDevolucao, CAST(GETDATE() AS DATE))     AS DiasDeAtraso,
       DATEDIFF(DAY, e.DataPrevistaDevolucao, CAST(GETDATE() AS DATE)) * 1.00 AS MultaEstimada
FROM dbo.Emprestimos AS e
INNER JOIN dbo.Livros  AS l ON l.Id = e.LivroId
INNER JOIN dbo.Membros AS m ON m.Id = e.MembroId
WHERE e.DataDevolucao IS NULL
  AND e.DataPrevistaDevolucao < CAST(GETDATE() AS DATE)
ORDER BY DiasDeAtraso DESC;

-- 3) Membros que já fizeram 2 ou mais empréstimos (GROUP BY + HAVING).
SELECT m.Nome, COUNT(*) AS TotalEmprestimos
FROM dbo.Membros AS m
INNER JOIN dbo.Emprestimos AS e ON e.MembroId = m.Id
GROUP BY m.Id, m.Nome
HAVING COUNT(*) >= 2
ORDER BY TotalEmprestimos DESC, m.Nome;

-- 4) Livros que nunca foram emprestados (LEFT JOIN e filtro por NULL).
SELECT l.Titulo, l.Autor
FROM dbo.Livros AS l
LEFT JOIN dbo.Emprestimos AS e ON e.LivroId = l.Id
WHERE e.Id IS NULL
ORDER BY l.Titulo;

-- 5) Ranking dos livros mais emprestados, com posição (função de janela RANK).
--    Livros com o mesmo total ficam na mesma posição.
SELECT RANK() OVER (ORDER BY COUNT(e.Id) DESC) AS Posicao,
       l.Titulo,
       COUNT(e.Id)                              AS TotalEmprestimos
FROM dbo.Livros AS l
LEFT JOIN dbo.Emprestimos AS e ON e.LivroId = l.Id
GROUP BY l.Id, l.Titulo
ORDER BY Posicao, l.Titulo;

-- 6) Total de multas cobradas por membro (só quem já pagou alguma).
SELECT m.Nome, SUM(e.Multa) AS TotalDeMultas
FROM dbo.Membros AS m
INNER JOIN dbo.Emprestimos AS e ON e.MembroId = m.Id
GROUP BY m.Id, m.Nome
HAVING SUM(e.Multa) > 0
ORDER BY TotalDeMultas DESC;

-- 7) Tempo médio, em dias, que os livros ficam emprestados (só os já devolvidos).
SELECT AVG(CAST(DATEDIFF(DAY, DataEmprestimo, DataDevolucao) AS DECIMAL(10, 2))) AS MediaDeDias
FROM dbo.Emprestimos
WHERE DataDevolucao IS NOT NULL;

-- 8) Histórico completo de um membro, do mais recente para o mais antigo.
DECLARE @Email NVARCHAR(150) = N'carla.dias@exemplo.com';

SELECT l.Titulo,
       e.DataEmprestimo,
       e.DataPrevistaDevolucao,
       e.DataDevolucao,
       e.Multa,
       CASE
           WHEN e.DataDevolucao IS NOT NULL THEN N'Devolvido'
           WHEN e.DataPrevistaDevolucao < CAST(GETDATE() AS DATE) THEN N'Atrasado'
           ELSE N'No prazo'
       END AS Situacao
FROM dbo.Emprestimos AS e
INNER JOIN dbo.Livros  AS l ON l.Id = e.LivroId
INNER JOIN dbo.Membros AS m ON m.Id = e.MembroId
WHERE m.Email = @Email
ORDER BY e.DataEmprestimo DESC;

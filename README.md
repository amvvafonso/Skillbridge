# Skillbridge
### Help
#### Migrations
Para atualizar e criar migrações, utilize o seguinte comando no terminal.
```
dotnet ef migrations add *Nome da migracao*
dotnet ef database update
```
##### Reset db
```sql
-- 1. Gerar comandos para apagar todas as Foreign Keys (para não dar erro de restrição)
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' DROP CONSTRAINT ' + QUOTENAME(f.name) + ';'
FROM sys.foreign_keys AS f
INNER JOIN sys.tables AS t ON f.parent_object_id = t.object_id
INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id;

-- 2. Gerar comandos para apagar todas as Tabelas
SELECT @sql += 'DROP TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';'
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
WHERE t.name <> '__EFMigrationsHistory'; -- Opcional: manter o histórico se quiseres

-- 3. Executar o extermínio
EXEC sp_executesql @sql;

drop table __EFMigrationsHistory

```

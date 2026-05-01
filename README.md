# O que é Skillbridge
O **Skillbridge** é uma plataforma focada em **Shadow Work** (trabalho de sombra ou acompanhamento profissional) e colaboração técnica em tempo real. Ele se posiciona como um espaço onde a barreira entre "aprender" e "fazer" é eliminada através da transparência total do fluxo de trabalho.
# TODO
- [ ] Atualização dos campos do InputModel e da base de dados para novos atributos e dados
- [ ] Criação da página das organizações
- [ ] Desenvolvimento da base de dados e da estrutura de modelos
- [ ] Criação da área de cliente onde estará as sessões que pode entrar, e os ficheiros que pode estudar
- [ ] Edição estética do perfil do utilizador (Alteração de password e outro tipos de dados)
- [ ] Desenvolvimento de estrutura de ficheiros e de visualização dos meus com uso de [[#Websocket|Websocket]] 
- [ ] Relacionar ficheiros de código com ficheiros markdown para comentários
# Help
## Migrations
Para atualizar e criar migrações, utilize o seguinte comando no terminal.
```
dotnet ef migrations add *Nome da migracao*
dotnet ef database update
```
## Reset db
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
## Websocket
WebSocket is a computer communications protocol providing full-duplex, bidirectional communication channels over a single, persistent TCP connection. Unlike HTTP's request-response model, WebSockets enable low-latency, real-time data exchange, allowing servers to send data to clients without a prior request. It is finalized under RFC 6455. 

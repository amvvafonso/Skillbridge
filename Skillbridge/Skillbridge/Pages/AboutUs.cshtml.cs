using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Skillbridge.Pages;

public class AboutUs : PageModel
{
    public class Author
    {
        public string Number { get; set; }
        public string Name { get; set; }
    }

    public class CreditEntry
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
    }

    public class TestCredential
    {
        public string Role { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public string CourseName => "Licenciatura em Engenharia Informática";
    public string SubjectName => "Desenvolvimento Web";
    public string AcademicYear => "2º ano, 2º semestre — Ano letivo 2025/2026";

    public List<Author> Authors { get; } = new()
    {
        new Author { Number = "26165", Name = "Igor Afonso" },
        new Author { Number = "27502", Name = "Afonso Viana" }
    };

    public List<CreditEntry> Credits { get; } = new()
    {
        new CreditEntry { Name = "ASP.NET Core (Razor Pages + MVC)", Description = "Framework principal da aplicação — interface server-side (Razor Pages) e API (MVC).", Source = "dotnet.microsoft.com/apps/aspnet" },
        new CreditEntry { Name = "Entity Framework Core", Description = "ORM utilizado para acesso à base de dados.", Source = "learn.microsoft.com/ef/core" },
        new CreditEntry { Name = "SignalR", Description = "Comunicação em tempo real — chat da sessão, notificações e sincronização do editor de código.", Source = "dotnet.microsoft.com/apps/aspnet/signalr" },
        new CreditEntry { Name = "AWS SDK for .NET (S3)", Description = "Armazenamento e acesso ao conteúdo dos ficheiros dos projetos.", Source = "nuget.org/packages/AWSSDK.S3" },
        new CreditEntry { Name = "Monaco Editor", Description = "Editor de código integrado na página de sessões colaborativas.", Source = "microsoft.github.io/monaco-editor" },
        new CreditEntry { Name = "Prism.js", Description = "Realce de sintaxe de código (syntax highlighting).", Source = "prismjs.com" },
        new CreditEntry { Name = "Bootstrap 5", Description = "Framework CSS para layout, componentes e responsividade.", Source = "getbootstrap.com" },
        new CreditEntry { Name = "Bootstrap Icons", Description = "Conjunto de ícones usado em toda a aplicação.", Source = "icons.getbootstrap.com" },
        new CreditEntry { Name = "jQuery", Description = "Biblioteca JavaScript utilitária.", Source = "jquery.com" },
        new CreditEntry { Name = "Google Fonts — Material Symbols Outlined", Description = "Conjunto de ícones tipográficos.", Source = "fonts.google.com/icons" },
    };

    public List<TestCredential> TestCredentials { get; } = new()
    {
        new TestCredential { Role = "Owner — Organização \"Apple\"", Login = "professor@email.pt", Password = "Professor123." },
        new TestCredential { Role = "Apprentice — Organização \"Apple\"", Login = "estudante@email.pt", Password = "Estudante123." }
    };
    public void OnGet()
    {
    }
}
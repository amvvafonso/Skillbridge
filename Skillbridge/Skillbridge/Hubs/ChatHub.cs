using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Skillbridge.Data;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;

namespace Skillbridge.Hubs;
/// <summary>
/// Hub SignalR para o chat em tempo real dentro das sessões do CodeEditor
/// Cada sessão funciona como um grupo separado assim isolando as mensagens
/// </summary>

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public ChatHub(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Junta o utilizador ao grupo da sessão quando abre o CodeEditor
    /// </summary>
    public async Task JoinSession(string sessionId)
    {
        //Vai buscar o user
        var user = await _userManager.GetUserAsync(Context.User);
        
        //Senão houver user retorna
        if (user == null) return;
        
        //Verifica se o utilizador tem acesso à sessão
        var access = await _context.SessionAccesses
            .FirstOrDefaultAsync(sa=> sa.SessionId == sessionId && sa.UserId == user.Id);

        //Senão estiver acesso á sessão retorna
        if (access == null) return;
        
        //Junta ao grupo SignalR da sessão
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }
    /// <summary>
    /// Remove o utilizador do grupo da sessão
    /// </summary>
    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
    
    /// <summary>
    /// Envia uma mensagem para todos os membros da sessão
    /// </summary>
    public async Task SendMessage(string sessionId, string message)
    {
        var user = await _userManager.GetUserAsync(Context.User);
        if (user == null) return;
        
        //Valida que a mensagem não está vazia
        if (String.IsNullOrWhiteSpace(message)) return;
        
        //Verifica se o utilizador tem acesso á sessão
        var access = await _context.SessionAccesses
            .FirstOrDefaultAsync(sa => sa.SessionId == sessionId && sa.UserId == user.Id);
        
        if (access == null) return;
        
        //Verifica se a sessão não está bloqueada
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null || session.Locked) return;
        
        //Guarda a mensagem na BD
        var chatMessage = new ChatMessage
        {
            SessionId = sessionId,
            UserId = user.Id,
            ChatMessageId = Guid.NewGuid().ToString(),
            ChatMessageText = message.Trim(),
            SentAt = DateTime.UtcNow.AddHours(1)
        };
        
        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();
        
        //Envia a mensagem para todos os membros do grupo
        await Clients.Group(sessionId).SendAsync("ReceiveMessage", new
        {
            userName = user.UserName,
            userId = user.Id,
            id = chatMessage.ChatMessageId,
            text = chatMessage.ChatMessageText,
            sentAt = chatMessage.SentAt.ToString("HH:mm")
        });

    }
    
    
}

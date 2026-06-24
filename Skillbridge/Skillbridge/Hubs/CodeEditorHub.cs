using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Skillbridge.Data;
using Skillbridge.Models.Client;
using Skillbridge.Models.Project;


namespace Skillbridge.Hubs;
/// <summary>
/// Hub SignalR para o codigo em tempo real dentro das sessões do CodeEditor
/// Cada sessão funciona como um grupo separado assim isolando o codigo editado
/// </summary>

[Authorize]
public class CodeEditorHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    
    public CodeEditorHub (ApplicationDbContext context, UserManager<User> userManager)
        {
        _context = context;
        _userManager = userManager;
        }
    
    
    public async  Task JoinSession(string sessionId)
    {
        
        var user = await _userManager.GetUserAsync(Context.User);
        if (user == null) return;

        var access = await _context.SessionAccesses
            .FirstOrDefaultAsync(sa => sa.SessionId == sessionId && sa.UserId == user.Id);

        if (access == null) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }
    
    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
    
    //Mentor envia alterações, todos os outros no grupo recebem
    public async Task SendCodeChange(string sessionId, string content)
    {
        var user = await _userManager.GetUserAsync(Context.User);
        if (user == null) return;
        
        var access = await _context.SessionAccesses
            .FirstOrDefaultAsync(sa => sa.SessionId == sessionId && sa.UserId == user.Id);
        
        //Só o mentor pode enviar alterações
        if (access?.Role != Role.Mentor) return;
        
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null || !session.Active || session.Locked) return;
        
        //Envia para todos exceto para o mentor
        await Clients.OthersInGroup(sessionId)
            .SendAsync("ReceiveCodeChange", content);
    }
}
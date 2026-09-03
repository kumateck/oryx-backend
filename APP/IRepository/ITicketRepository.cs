using APP.Utils;
using DOMAIN.Entities.Tickets;
using SHARED;

namespace APP.IRepository;

public interface ITicketRepository
{
    Task<Result<Guid>> CreateTicket(CreateTicketRequest request, Guid reportedById, Guid? departmentId);

    Task<Result<Paginateable<IEnumerable<TicketDto>>>> GetTickets(Guid currentUserId, int page, int pageSize,
        string searchQuery = null, TicketStatus? status = null, TicketPriority? priority = null,
        TicketCategory? category = null, Guid? assignedToId = null, Guid? reportedById = null);

    Task<Result<TicketDto>> GetTicket(Guid id);
    Task<Result> AssignTicket(Guid id, Guid assignedToId, Guid assignedById);
    Task<Result> StartProgress(Guid id, Guid userId);
    Task<Result> MarkDone(Guid id, Guid userId);
    Task<Result> CloseTicket(Guid id, Guid userId);
    Task<Result> ReopenTicket(Guid id, string reason, Guid userId);
    Task<Result<Guid>> AddComment(Guid id, string comment, Guid userId);
    Task<Result<IEnumerable<TicketActivityDto>>> GetTicketActivity(Guid id);
}

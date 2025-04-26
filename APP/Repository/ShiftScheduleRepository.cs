// using APP.IRepository;
// using APP.Utils;
// using AutoMapper;
// using DOMAIN.Entities.ShiftSchedules;
// using INFRASTRUCTURE.Context;
// using Microsoft.EntityFrameworkCore;
// using SHARED;
//
// namespace APP.Repository;
//
// public class ShiftScheduleRepository(ApplicationDbContext context, IMapper mapper) : IShiftScheduleRepository
// {
//     public async Task<Result<Guid>> CreateShiftSchedule(CreateShiftScheduleRequest request, Guid userId)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result<Paginateable<IEnumerable<ShiftScheduleDto>>>> GetShiftSchedules(int page, int pageSize, string searchQuery)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result<ShiftScheduleDto>> GetShiftSchedule(Guid id)
//     {
//         var shiftSchedule = await context.ShiftSchedules.FirstOrDefaultAsync(s => s.Id == id && s.LastDeletedById == null);
//         if (shiftSchedule is null)
//         {
//             return Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found");
//         }
//         
//         return Result.Success(mapper.Map<ShiftScheduleDto>(shiftSchedule));
//
//     }
//
//     public async Task<Result> UpdateShiftSchedule(Guid id, CreateShiftScheduleRequest request, Guid userId)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result> DeleteShiftSchedule(Guid id, Guid userId)
//     {
//         var shiftSchedule = await context.ShiftSchedules.FirstOrDefaultAsync(s => s.Id == id && s.LastDeletedById == null);
//         if (shiftSchedule is null)
//         {
//             return Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found");
//         }
//         
//         context.ShiftSchedules.Update(shiftSchedule);
//         await context.SaveChangesAsync();
//         return Result.Success();
//     }
// }
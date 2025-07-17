// using DOMAIN.Entities.LeaveTypes;
// using INFRASTRUCTURE.Context;
//
// namespace API.Database.Seeds.TableSeeders;
//
// public class LeaveTypeSeeder : ISeeder
// {
//     public void Handle(IServiceScope scope)
//     {
//         var dbContext  = scope.ServiceProvider.GetService<ApplicationDbContext>();
//         
//         if (dbContext != null) SeedLeaveTypes(dbContext);
//     }
//
//     private static void SeedLeaveTypes(ApplicationDbContext dbContext)
//     {
//         var leaveTypes = new List<LeaveType>
//         {
//             new()
//             {
//                 Name = "Sick Leave",
//                 NumberOfDays = 20,
//                 DeductFromBalance = true,
//                 IsPaid = true,
//                 IsActive = true,
//                 CreatedAt = DateTime.UtcNow,
//                 Designations = [..dbContext.Designations.AsQueryable()]
//             },
//             new()
//             {
//                 Name = "Maternity Leave",
//                 NumberOfDays = 20,
//                 DeductFromBalance = true,
//                 IsPaid = true,
//                 IsActive = true,
//                 CreatedAt = DateTime.UtcNow,
//                 Designations = [..dbContext.Designations.AsQueryable()]
//             }
//         };
//
//         foreach (var leaveType in leaveTypes)
//         {
//             dbContext.LeaveTypes.Add(leaveType);
//         }
//         
//         dbContext.SaveChanges();
//     }
// }
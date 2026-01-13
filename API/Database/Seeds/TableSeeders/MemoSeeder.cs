// using DOMAIN.Entities.LeaveTypes;
// using DOMAIN.Entities.Memos;
// using INFRASTRUCTURE.Context;
//
// namespace API.Database.Seeds.TableSeeders;
//
// public class MemoSeeder : ISeeder
// {
//     public void Handle(IServiceScope scope)
//     {
//         var dbContext  = scope.ServiceProvider.GetService<ApplicationDbContext>();
//         
//         if (dbContext != null) SeedMemo(dbContext);
//     }
//
//     private static void SeedMemo(ApplicationDbContext dbContext)
//     {
//         var memo = new Memo
//         {
//             Id = Guid.Empty,
//             Code = "MEMO-00256001",
//             Paid = true,
//             CreatedAt = DateTime.UtcNow
//         };
//         
//         dbContext.Memos.Add(memo);
//         dbContext.SaveChanges();
//     }
// }
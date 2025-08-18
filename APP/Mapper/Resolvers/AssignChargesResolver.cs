using AutoMapper;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class AssignChargesResolver(ApplicationDbContext context)
    : IValueResolver<CreateBillingSheetRequest, BillingSheet, List<Charge>>
{
    public List<Charge> Resolve(CreateBillingSheetRequest source, BillingSheet destination, List<Charge> destMember, ResolutionContext context1)
    {
        var charges = new List<Charge>();
        foreach (var chargeRequest in source.Charges)
        {
            Charge charge;
            if (chargeRequest.Id != null)
            {
                charge = context.Charges.Find(chargeRequest.Id);
                if (charge != null)
                {
                    charge.Amount = chargeRequest.Amount;
                    charge.Description = chargeRequest.Description;
                    charge.CurrencyId = chargeRequest.CurrencyId;
                    charges.Add(charge);
                    continue;
                }
            }
            
            charge = new Charge
            {
                Amount = chargeRequest.Amount,
                Description = chargeRequest.Description,
                CurrencyId = chargeRequest.CurrencyId
            };
            charges.Add(charge);
        }
        return charges;
    }
}
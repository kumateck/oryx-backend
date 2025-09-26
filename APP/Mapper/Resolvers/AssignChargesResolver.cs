/*using AutoMapper;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class AssignBillingSheetChargesResolver(ApplicationDbContext context)
    : IValueResolver<CreateBillingSheetRequest, BillingSheet, List<BillingSheetCharge>>
{
    public List<BillingSheetCharge> Resolve(
        CreateBillingSheetRequest source,
        BillingSheet destination,
        List<BillingSheetCharge> destMember,
        ResolutionContext context1)
    {
        var billingSheetCharges = new List<BillingSheetCharge>();

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

                    billingSheetCharges.Add(new BillingSheetCharge
                    {
                        ChargeId = charge.Id,
                        Charge = charge
                    });

                    continue;
                }
            }

            charge = new Charge
            {
                Amount = chargeRequest.Amount,
                Description = chargeRequest.Description,
                CurrencyId = chargeRequest.CurrencyId
            };

            billingSheetCharges.Add(new BillingSheetCharge
            {
                Charge = charge
            });
        }

        return billingSheetCharges;
    }
}*/
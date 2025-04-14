using AutoMapper;
using DOMAIN.Entities.PurchaseOrders;
using System.Diagnostics;

namespace APP.Mapper.Resolvers;

public class PurchaseOrderRevisionResolver : IValueResolver<PurchaseOrder, PurchaseOrderDto, List<PurchaseOrderRevisionDto>>
{
    public List<PurchaseOrderRevisionDto> Resolve(PurchaseOrder source, PurchaseOrderDto destination, List<PurchaseOrderRevisionDto> destMember, ResolutionContext context)
    {
        var result = new List<PurchaseOrderRevisionDto>();

        var maxRevision = source.RevisedPurchaseOrders.Any() 
            ? source.RevisedPurchaseOrders.Max(r => r.RevisionNumber) 
            : 0;

        for (int rev = 1; rev <= maxRevision; rev++)
        {
            var resolvedItems = ResolvePurchaseOrderAtRevision(source, rev);
            var itemDtos = context.Mapper.Map<List<PurchaseOrderItemDto>>(resolvedItems);

            result.Add(new PurchaseOrderRevisionDto
            {
                RevisionNumber = rev,
                Items = itemDtos
            });
        }

        return result;
    }
    
    private List<PurchaseOrderItemSnapshot> ResolvePurchaseOrderAtRevision(PurchaseOrder order, int revisionNumber)
    {
        var resolvedItems = new List<PurchaseOrderItemSnapshot>();
        var initialItems = new List<PurchaseOrderItemSnapshot>(order.Items.Select(item => new PurchaseOrderItemSnapshot
        {
            Id = item.Id,
            MaterialId = item.MaterialId,
            Material = item.Material,
            UoMId = item.UoMId,
            UoM = item.UoM,
            Quantity = item.Quantity,
            Price = item.Price,
            CurrencyId = item.CurrencyId,
            Currency = item.Currency
        }));

        var revisions = order.RevisedPurchaseOrders
            .Where(r => r.RevisionNumber <= revisionNumber)
            .OrderBy(r => r.RevisionNumber)
            .ThenBy(r => r.Type)
            .ToList();

        foreach (var revision in revisions)
        {
            switch (revision.Type)
            {
                case RevisedPurchaseOrderType.AddItem:
                    // Reverse the AddItem action, so remove the item
                    initialItems.RemoveAll(i => i.Id == revision.PurchaseOrderItemId);
                    break;

                case RevisedPurchaseOrderType.UpdateItem:
                    var updateTarget = initialItems.FirstOrDefault(i => i.Id == revision.PurchaseOrderItemId);
                    if (updateTarget != null)
                    {
                        updateTarget.UoMId = revision.UoMId ?? updateTarget.UoMId;
                        updateTarget.Quantity = revision.Quantity ?? updateTarget.Quantity;
                        updateTarget.Price = revision.Price ?? updateTarget.Price;
                        updateTarget.CurrencyId = revision.CurrencyId ?? updateTarget.CurrencyId;
                    }
                    break;

                case RevisedPurchaseOrderType.RemoveItem:
                    // Reverse the RemoveItem action, so add the item back
                    var itemToAddBack = initialItems.FirstOrDefault(i => i.Id == revision.PurchaseOrderItemId);
                    if (itemToAddBack != null)
                    {
                        resolvedItems.Add(new PurchaseOrderItemSnapshot
                        {
                            Id = itemToAddBack.Id,
                            MaterialId = itemToAddBack.MaterialId,
                            UoMId = itemToAddBack.UoMId,
                            Quantity = itemToAddBack.Quantity,
                            Price = itemToAddBack.Price,
                            CurrencyId = itemToAddBack.CurrencyId
                        });
                    }
                    break;

                case RevisedPurchaseOrderType.ReassignSuppler:
                    var reassignedItemsToAddBack = order.Items.FirstOrDefault(i => i.Id == revision.PurchaseOrderItemId);
                    if (reassignedItemsToAddBack != null)
                    {
                        resolvedItems.Add(new PurchaseOrderItemSnapshot
                        {
                            Id = reassignedItemsToAddBack.Id,
                            MaterialId = reassignedItemsToAddBack.MaterialId,
                            UoMId = reassignedItemsToAddBack.UoMId,
                            Quantity = reassignedItemsToAddBack.Quantity,
                            Price = reassignedItemsToAddBack.Price,
                            CurrencyId = reassignedItemsToAddBack.CurrencyId
                        });
                    }
                    break;
                case RevisedPurchaseOrderType.ChangeSource:
                    var changeSourceItemsToAddBack = order.Items.FirstOrDefault(i => i.Id == revision.PurchaseOrderItemId);
                    if (changeSourceItemsToAddBack != null)
                    {
                        resolvedItems.Add(new PurchaseOrderItemSnapshot
                        {
                            Id = changeSourceItemsToAddBack.Id,
                            MaterialId = changeSourceItemsToAddBack.MaterialId,
                            UoMId = changeSourceItemsToAddBack.UoMId,
                            Quantity = changeSourceItemsToAddBack.Quantity,
                            Price = changeSourceItemsToAddBack.Price,
                            CurrencyId = changeSourceItemsToAddBack.CurrencyId
                        });
                    }
                    break;
            }
        }

        // At the end of the loop, resolvedItems will hold the final state after all the revisions
        // Add back any items that were present before the revisions
        resolvedItems.AddRange(initialItems);

        return resolvedItems;
    }
}

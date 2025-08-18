using AutoMapper;
    using DOMAIN.Entities.Checklists;
    using DOMAIN.Entities.Warehouses;
    using INFRASTRUCTURE.Context;
    using Microsoft.EntityFrameworkCore;
    
    namespace APP.Mapper.Resolvers;
    
    public class ChecklistResolver : IValueResolver<DistributedRequisitionMaterial, DistributedRequisitionMaterialDto, ChecklistDto>
    {
        private readonly ApplicationDbContext _context;
    
        public ChecklistResolver(ApplicationDbContext context)
        {
            _context = context;
        }
    
        public ChecklistDto Resolve(DistributedRequisitionMaterial source, DistributedRequisitionMaterialDto destination, ChecklistDto destMember, ResolutionContext context)
        {
            var checklist = _context.Checklists
                .AsSplitQuery()
                .Include(c => c.MaterialBatches)
                .FirstOrDefaultAsync(c => c.DistributedRequisitionMaterialId == source.Id)
                .Result;
        
            return checklist != null ? context.Mapper.Map<ChecklistDto>(checklist) : null;
        }
    }
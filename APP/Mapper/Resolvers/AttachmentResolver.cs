using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;

namespace APP.Mapper.Resolvers;

public class AttachmentsResolver(ApplicationDbContext context, IHttpContextAccessor request)
    : IValueResolver<BaseEntity, WithAttachment, IEnumerable<AttachmentDto>>
{
    public IEnumerable<AttachmentDto> Resolve(BaseEntity source, WithAttachment destination, IEnumerable<AttachmentDto> destMember, ResolutionContext context1)
    {
        try
        {
            context1.Items.TryGetValue(AppConstants.ModelType, out var modelTypeValue);
            var modelType = (string)modelTypeValue;

            return context.Attachments
                .Where(attachment => attachment.ModelType.ToLower() == modelType.ToLower() && attachment.ModelId == source.Id)
                .Select(attachment => new AttachmentDto
                {
                    Name = attachment.Name,
                    Link = $"https://{request.HttpContext.Request.Host}/api/v1/file/{modelType.ToLower()}/{attachment.ModelId}/{attachment.Reference}",
                    Id = attachment.ModelId,
                    Reference = attachment.Reference
                }).ToList();
        }
        catch (Exception)
        {
            return new List<AttachmentDto>();
        }
    }
}
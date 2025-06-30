using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Forms;

namespace APP.Mapper.Resolvers;

public class FormResponseAttachmentResolver(IMapper mapper) : IValueResolver<Response, ResponseDto, List<FormResponseDto>>
{
    public List<FormResponseDto> Resolve(Response source, ResponseDto destination, List<FormResponseDto> destMember, ResolutionContext context)
    {
        return mapper.Map<List<FormResponseDto>>(source.FormResponses, opt =>
        {
            opt.Items[AppConstants.ModelType] = nameof(FormResponse);
        });
    }
}

public class FormWithResponseAttachmentResolver(IMapper mapper) : IValueResolver<Form, FormDto, List<FormResponseDto>>
{
    public List<FormResponseDto> Resolve(Form source, FormDto destination, List<FormResponseDto> destMember, ResolutionContext context)
    {
        return mapper.Map<List<FormResponseDto>>(source.Responses, opt =>
        {
            opt.Items[AppConstants.ModelType] = nameof(FormResponse);
        });    }
}
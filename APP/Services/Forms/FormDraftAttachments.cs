using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using SHARED;

namespace APP.Services.Forms;

public static class FormDraftAttachments
{
    public static async Task<Result> Save(Response response, FormField field, Guid? revisionId,
        string value, Guid userId, IFileRepository files, ApplicationDbContext context)
    {
        var answer = response.FormResponses.FirstOrDefault(item => item.FormFieldId == field.Id);
        if (answer == null)
        {
            // The blob key and Attachment.ModelId must be captured only after the field has a stable ID.
            answer = new FormResponse {
                Id = Guid.CreateVersion7(), FormFieldId = field.Id,
                FormFieldRevisionId = revisionId, Value = "form response attachment."
            };
            // An assigned generated key on a tracked navigation may be interpreted as
            // an existing row. Mark it Added before FileRepository flushes this context.
            context.FormResponses.Add(answer);
            response.FormResponses.Add(answer);
        }
        foreach (var data in value.Split("|"))
        {
            var result = await files.SaveBlobItem(nameof(FormResponse), answer.Id,
                Guid.NewGuid().ToString(), data.ConvertFromBase64(), userId);
            if (result.IsFailure) return result;
        }
        return Result.Success();
    }
}

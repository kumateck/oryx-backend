using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Forms;
using SHARED;

namespace APP.Services.Forms;

public static class FormDraftAttachments
{
    public static async Task<Result> Save(Response response, FormField field, Guid? revisionId,
        string value, Guid userId, IFileRepository files)
    {
        var answer = response.FormResponses.FirstOrDefault(item => item.FormFieldId == field.Id);
        if (answer == null)
        {
            // The blob key and Attachment.ModelId must be captured only after the field has a stable ID.
            answer = new FormResponse {
                Id = Guid.CreateVersion7(), FormFieldId = field.Id,
                FormFieldRevisionId = revisionId, Value = "form response attachment."
            };
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

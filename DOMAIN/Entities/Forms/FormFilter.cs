using SHARED;

namespace DOMAIN.Entities.Forms;

public class FormFilter : PagedQuery
{
    public string SearchQuery { get; set; }
    public FormType? Type { get; set; }
}
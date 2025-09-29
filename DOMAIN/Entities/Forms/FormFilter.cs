using SHARED;

namespace DOMAIN.Entities.Forms;

public class FormFilter : PagedQuery
{
    public string SearchQuery { get; set; }
    public List<FormType?> Type { get; set; }
}
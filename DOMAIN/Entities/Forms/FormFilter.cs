using SHARED;

namespace DOMAIN.Entities.Forms;

public class FormFilter : PagedQuery
{
    public string SearchQuery { get; set; }
    public FormType? Type { get; set; }
}

public class QuestionFilter : PagedQuery
{
    public string SearchQuery { get; set; }
    public List<QuestionType?> Type { get; set; } = [];
    public FormType? FormType { get; set; }
}
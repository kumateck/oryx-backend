using SHARED;

namespace DOMAIN.Entities.Forms;

public class FormFilter : PagedQuery
{
    public string SearchQuery { get; set; }
    public FormType? Type { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ProductId { get; set; }
    public string MaterialSpecificationNumber { get; set; }
    public string ProductSpecificationNumber { get; set; }
}

public class QuestionFilter : PagedQuery
{
    public string SearchQuery { get; set; }
    public List<QuestionType?> Type { get; set; } = [];
    public FormType? FormType { get; set; }
}

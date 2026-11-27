using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Forms;

public class Question : BaseEntity
{
    [StringLength(1000)]
    public string Label { get; set; }
    public QuestionType Type { get; set; }
    public QuestionValidationType Validation { get; set; }
    public List<QuestionOption> Options { get; set; } = [];
    public bool IsMultiSelect { get; set; }

    [StringLength(100)]
    public string Reference { get; set; }

    [StringLength(10000)]
    public string Description { get; set; }
}

public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public Question Question { get; set; }

    [StringLength(100000000)]
    public string Name { get; set; }
}

public class QuestionDto : BaseDto
{
    public string Label { get; set; }
    public QuestionType Type { get; set; }
    public QuestionValidationType Validation { get; set; }
    public bool IsMultiSelect { get; set; }
    public string Reference { get; set; }
    public string Description { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = [];
}

public class QuestionOptionDto : BaseDto
{
    public CollectionItemDto Question { get; set; }
    public string Name { get; set; }
}

public enum QuestionType
{
    ShortAnswer = 0,
    LongAnswer = 1,
    Paragraph = 2,
    Datepicker = 3,
    SingleChoice = 4,
    Dropdown = 5,
    Checkbox = 6,
    FileUpload = 7,
    Signature = 8,
    Reference = 9,
    Formula = 10,
    Specification = 11,
    Equipment = 12,
    Table = 13,
    Instrument = 14,
    /// <summary>A microorganism from the fixed list in <see cref="MicrobialOrganisms"/>; the answer is its code.</summary>
    Organism = 15,
    /// <summary>Sample preparation dilution; the answer is JSON {sample, sampleUnit, diluent, factor}.</summary>
    Dilution = 16,
    /// <summary>Incubation conditions; the answer is JSON {temperature, hours, startedAt, endedAt}.</summary>
    Incubation = 17,
    /// <summary>Plate counts; the answer is JSON {plates: [n, ...], tooNumerousToCount}.</summary>
    ColonyCount = 18,
    /// <summary>Average count × dilution factor, calculated by the server from the section's ColonyCount and Dilution; its single option is the unit.</summary>
    CfuCalculation = 19,
    /// <summary>Presence or absence of growth of a specified organism; the answer is "Absent" or "Present".</summary>
    GrowthObservation = 20,
}

public enum QuestionValidationType
{
    Number = 0,
    Letter = 1,
    Alphanumeric = 2,
    None = 3,
}

public class Formula
{
    [Required]
    [StringLength(100)]
    public string Marker { get; set; } = "present";

    [StringLength(1000)]
    public string Expression { get; set; }

    [StringLength(100000000)]
    public string Variables { get; set; }
}

public class FormulaDto
{
    [StringLength(1000)]
    public string Expression { get; set; }

    [StringLength(100000000)]
    public string Variables { get; set; }
}

public class Variable
{
    public string Key { get; set; }
    public string Label { get; set; }
}

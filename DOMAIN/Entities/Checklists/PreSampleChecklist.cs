using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Checklists;
public class PreSampleChecklist : BaseEntity
{
    /// <summary>
    /// The date when the pre-sample checklist was created.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The ID of the associated material batch.
    /// </summary>
    public Guid MaterialBatchId { get; set; }

    public MaterialBatch MaterialBatch { get; set; }

    /// <summary>
    /// The ID of the associated GRN (Goods Received Note).
    /// </summary>
    public Guid GrnId { get; set; }

    public Grn Grn { get; set; }

    /// <summary>
    /// The environmental temperature condition in Celsius.
    /// </summary>
    public decimal TemperatureCondition { get; set; }

    /// <summary>
    /// The relative humidity (RH) at the time of sampling.
    /// </summary>
    public decimal EnvironmentalConditionRh { get; set; }

    /// <summary>
    /// The cleanliness status of the packing.
    /// </summary>
    public PackingCleanliness PackingCleanliness { get; set; }

    /// <summary>
    /// The style(s) used for packing.
    /// </summary>
    public List<string> PackingStyles { get; set; } = [];

    /// <summary>
    /// Indicates if a quarantine label was affixed.
    /// </summary>
    public QuarantinedLabel QuarantinedLabel { get; set; }

    /// <summary>
    /// Pharmacopoeia standards applicable.
    /// </summary>
    public List<string> PharmacopoeiaStatus { get; set; } = [];

    /// <summary>
    /// Indicates if the manufacturer's name is mentioned.
    /// </summary>
    public bool ManufacturersNameMentioned { get; set; }

    /// <summary>
    /// Indicates if the batch number is mentioned.
    /// </summary>
    public MentionedStatus BatchNumber { get; set; }

    /// <summary>
    /// Indicates if the quantity is mentioned.
    /// </summary>
    public MentionedStatus Quantity { get; set; }

    /// <summary>
    /// Indicates if the storage condition is mentioned.
    /// </summary>
    public MentionedStatus StorageCondition { get; set; }

    /// <summary>
    /// Indicates if the number of containers is mentioned.
    /// </summary>
    public MentionedStatus NoOfContainer { get; set; }

    /// <summary>
    /// Indicates if any container is damaged.
    /// </summary>
    public bool AnyContainerDamaged { get; set; }

    /// <summary>
    /// Additional remarks or observations.
    /// </summary>
    [StringLength(1000000)] public string AnyOtherRemarks { get; set; }

    /// <summary>
    /// Indicates if the physical appearance conforms to standard.
    /// </summary>
    public ConformityStatus PhysicalAppearance { get; set; }

    /// <summary>
    /// Indicates if the manufacturer's seal is present.
    /// </summary>
    public PresenceStatus ManufacturerSeal { get; set; }

    /// <summary>
    /// Indicates if lumps are present.
    /// </summary>
    public PresenceStatus PresenceOfLumps { get; set; }

    /// <summary>
    /// Indicates if any non-characteristic odour is present.
    /// </summary>
    public PresenceStatus AnyNonCharacteristicOdour { get; set; }

    /// <summary>
    /// Indicates if there is heterogeneity within the same container.
    /// </summary>
    public PresenceStatus HeterogeneityWithinSameContainer { get; set; }

    /// <summary>
    /// Indicates if there is heterogeneity between different containers.
    /// </summary>
    public PresenceStatus HeterogeneityBetweenDifferentContainers { get; set; }

    /// <summary>
    /// The user who performed the check.
    /// </summary>
    public Guid DoneById { get; set; }
    public User DoneBy { get; set; }

    /// <summary>
    /// The user who verified the check.
    /// </summary>
    public Guid CheckedById { get; set; }
    public User CheckedBy { get; set; }

    /// <summary>
    /// The current status of the form.
    /// </summary>
    public FormStatus FormStatus { get; set; }
}

public class CreatePreSampleChecklistRequest
{
    public DateTime Date { get; set; }
    public Guid MaterialBatchId { get; set; }
    public Guid GrnId { get; set; }
    public decimal TemperatureCondition { get; set; }
    public decimal EnvironmentalConditionRh { get; set; }
    public PackingCleanliness PackingCleanliness { get; set; }
    public List<string> PackingStyles { get; set; } = new();
    public QuarantinedLabel QuarantinedLabel { get; set; }
    public List<string> PharmacopoeiaStatus { get; set; } = new();
    public MentionedStatus ManufacturersName { get; set; }
    public MentionedStatus BatchNumber { get; set; }
    public MentionedStatus Quantity { get; set; }
    public MentionedStatus StorageCondition { get; set; }
    public MentionedStatus NoOfContainer { get; set; }
    public bool AnyContainerDamaged { get; set; }
    public string AnyOtherRemarks { get; set; }
    public ConformityStatus PhysicalAppearance { get; set; }
    public PresenceStatus ManufacturerSeal { get; set; }
    public PresenceStatus PresenceOfLumps { get; set; }
    public PresenceStatus AnyNonCharacteristicOdour { get; set; }
    public PresenceStatus HeterogeneityWithinSameContainer { get; set; }
    public PresenceStatus HeterogeneityBetweenDifferentContainers { get; set; }
}

public class PreSampleChecklistDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public MaterialBatchReducedDto MaterialBatch { get; set; }
    public string GrnGraNumber { get; set; }
    public bool AnyContainerDamaged { get; set; }
    public decimal TemperatureCondition { get; set; }
    public decimal EnvironmentalConditionRh { get; set; }
    public string PackingCleanliness { get; set; }
    public List<string> PackingStyles { get; set; }
    public string QuarantinedLabel { get; set; }
    public List<string> PharmacopoeiaStatus { get; set; }
    public bool ManufacturersNameMentioned { get; set; }
    public string Quantity { get; set; }
    public string StorageCondition { get; set; }
    public string AnyOtherRemarks { get; set; }
    public UserDto DoneBy { get; set; }
    public UserDto CheckedBy { get; set; }
    public FormStatus FormStatus { get; set; }
}

public enum PackingCleanliness { Satisfactory, NotSatisfactory }
public enum QuarantinedLabel { Affixed, NotAffixed }
public enum MentionedStatus { Mentioned, NotMentioned }
public enum ConformityStatus { Conforms, DoesNotConform }
public enum PresenceStatus { Present, Absent }
public enum FormStatus { InProgress, Submitted }
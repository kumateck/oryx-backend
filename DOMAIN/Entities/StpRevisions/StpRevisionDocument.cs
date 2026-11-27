using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StpRevisions;

/// <summary>
/// The Word file generated from a revision when it was approved, kept byte for byte with its SHA-256, so what was
/// approved is the exact file people download and print. Written once, in the approval's transaction.
/// </summary>
public class StpRevisionDocument : BaseEntity
{
    public Guid StpRevisionId { get; set; }
    public StpRevision StpRevision { get; set; }
    [StringLength(260)] public string FileName { get; set; }
    /// <summary>Lower-case hex SHA-256 of <see cref="Content"/>.</summary>
    [StringLength(64)] public string Sha256 { get; set; }
    public byte[] Content { get; set; }
}

/// <summary>
/// A specification test cites the STP test it is performed by: an STP number of the same kind and the stable key of one
/// of its tests. A test is a section of the specification's template, which other specifications may share, so the link
/// is scoped by the specification. Kept apart from the specification so the STP side owns it.
/// </summary>
public class StpTestLink : BaseEntity
{
    public Guid FormSectionId { get; set; }
    /// <summary>The material specification the link belongs to; exactly one of the two specification ids is set.</summary>
    public Guid? MaterialSpecificationId { get; set; }
    public Guid? ProductSpecificationId { get; set; }
    public StpRevisionKind Kind { get; set; }
    [StringLength(100)] public string StpNumber { get; set; }
    [StringLength(100)] public string TestKey { get; set; }
}

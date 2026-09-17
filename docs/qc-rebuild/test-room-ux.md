# Test Room UX

A work queue, not a record browser — the analyst shouldn't need to navigate
ARD → Details → Tests → Worksheet. Opening a card opens the `WorksheetInstance`
execution view directly.

```
QC TEST ROOM

My Work

12 Pending      4 In Progress      3 Awaiting Review

------------------------------------

ARD-2609124
Paracetamol Tablets
Batch: PT260901

ASSAY
Chemical

Due: Today

[ Start Test ]
```

## My Work is scoped by assignment, not just role

"My Work" isn't every test a QC Officer's permissions would let them touch — it's
only the `WorksheetInstance`s currently assigned to *them* (see
[lifecycle-and-governance.md](./lifecycle-and-governance.md#assignment-enforcement)).
A test with no assignee yet doesn't appear in anyone's My Work; it sits in a QC
Manager–visible unassigned pool until assigned. Reassigning a test moves it out of
the previous assignee's queue and into the new one's immediately.

## Separate tabs by AnalysisType

Chemical and Microbial work is shown in **separate tabs**, not one unified queue —
Chemical and Microbial analysts are usually different people/rooms with different
equipment in practice, so each only ever sees their own relevant work with no
filtering needed.

## Reviewer queue

**Awaiting My Review**: submitted values shown side-by-side with `Specification`
limits. OOS results surface with a red flag and a link into the `OOSCase`, blocking
Approve until a disposition exists (see
[lifecycle-and-governance.md](./lifecycle-and-governance.md#oos--oot--formal-workflow)).
Actions: Approve / Return for Correction / (for OOS) route to Investigation.

## Referenced fields

`ReferencedResult` fields are clickable inline — opening the source
`WorksheetInstance` in a side panel, not a full navigation away — so traceability
back to the original analyst entry costs one click, not a separate lookup. See
[field-catalog.md](./field-catalog.md#integration) for the field type this renders.

## Rendering the spec inline

The execution view resolves `Specification.Characteristics` against the current
`WorksheetInstance.FieldValues` live and renders them as the "Test | Result |
Specification" summary real paper worksheets already use — see
[specification-model.md](./specification-model.md#rendering-is-invisible-to-the-analyst).
The analyst never needs to know Specification and WorksheetTemplate are separate
records.

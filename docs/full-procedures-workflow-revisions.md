# Full Procedures Workflow template revisions

Date: 2026-09-16. Status: implemented backend definition layer; not executable.

`/api/v1/template-workflows` owns stable, area-scoped Workflow identities and
immutable revisions. A revision is a typed graph of nodes and edges: Start,
End, Activity (an exact Published Activity revision binding), Branch,
Fork/Join, Wait/IPC, Hold/Resume and Rework. Node visual position (React
Flow x/y) is persisted separately from this graph and does not affect the
governed content or its hash. Node labels and node/edge ordering are semantic
content and are included in the immutable hash.

## Graph shape rules

The server validates the declared graph is well-formed before it ever touches
the database:

- Exactly one Start node (no incoming edge, exactly one outgoing edge); at
  least one End node (no outgoing edge).
- An Activity node pins an exact `TemplateActivityId` + `TemplateActivityRevisionId`
  pair and has exactly one outgoing edge.
- A Branch node has two or more outgoing edges, each carrying a unique,
  non-empty branch key; a non-Branch edge never carries one.
- A Fork node has two or more outgoing edges and a `JoinGroupKey`; exactly one
  Join node in the revision must share that same key, and every
  `JoinGroupKey` used must pair Fork-to-Join one-to-one. The Join node
  requires two or more incoming edges.
- A Wait or IPC node declares a `WaitKind` (Duration, ApprovedReceipt,
  ExternalEvent) and has exactly one outgoing edge.
- A Hold node declares a `HoldGroupKey`; at least one Resume node must share
  that key, and every Hold/Resume key used must appear on both sides.
- A Rework node declares a `ReworkTargetKey` (another node in the same
  revision) and a `ReworkMaxAttempts` (1-10). The target must have a strictly
  smaller declared `Order` than the Rework node itself — the only permitted
  cycle in the definition, and it is bounded by the attempt ceiling rather
  than expressed as an ordinary edge.
- Every node except Start needs at least one incoming edge; every node except
  End needs at least one outgoing edge; every node must be reachable from
  Start; the graph formed by ordinary edges (excluding the Rework
  back-reference, which is not an edge) must contain no cycle.

This proves the declared graph is structurally sound — single entry, every
node reachable, fork/join and hold/resume keys pair correctly, no unbounded
loop outside the explicit, capped Rework mechanism. It is not a full soundness
proof of runtime flow semantics (see Evidence limits below).

## Lifecycle and governance

Draft → In Review → reviewed → Published → Retired, identical to the
Question/Section/Form/Activity layers: expected-content-hash and reason fence
every write; author, reviewer and publisher authority are separate
(`regulated-three-person` areas additionally require the publisher to differ
from the reviewer); publication rechecks every pinned Activity revision is
still Published in the same area/purpose/subject context, and transactionally
supersedes the prior Published revision. Every command appends an
actor/correlation-bound JSON audit snapshot; the snapshot and the content hash
both deliberately exclude node layout.

## Layout persistence

`PUT /api/v1/template-workflows/revisions/{id}/layout` updates per-node x/y
position independently of the Draft-only content lifecycle: it works on
Draft, In Review or Published revisions (not Retired), requires only that the
caller's `ExpectedContentHash` matches the revision's current content hash
(so a stale client cannot silently save positions against a graph it never
saw), and is not audited or hash-fenced, since repositioning a node carries no
governed meaning.
Coordinates must be finite; NaN and infinity are rejected before persistence.

## Migration

Migration `20260916141007_AddFullProcedureWorkflowRevisions` adds the
`TemplateWorkflow{s,Revisions,Nodes,Edges,NodeLayouts,RevisionAudits}` tables
and the `AK_TemplateActivityRevisions_Id_TemplateActivityId` alternate key
needed for a Workflow node to pin an exact Activity revision by composite FK
(mirroring the same Form-revision-pinning pattern the Activity layer uses).
`dotnet ef migrations has-pending-model-changes` reports no pending changes
after this migration.

The four node types that reference domain concepts (Activity, Wait/IPC,
Hold/Resume, Rework) describe controlled intent only. Publishing a Workflow
does not execute it. A governed action-contract catalog, release bundle and
idempotent runtime remain required before any of this can drive a real
Procedure run.

## Evidence limits

- Fork/Join validation proves one-to-one key pairing and minimum in/out
  degree; it does not prove every parallel branch launched by a Fork
  terminates at its paired Join before reaching an End node (full flow-graph
  soundness). A Fork whose branches skip the Join and reach End directly will
  still pass this validator.
- Wait/IPC/Hold nodes declare intent (`WaitKind`, `HoldGroupKey`) but no
  runtime adapter resolves a duration, an approved receipt, an external event
  or a manual hold/resume signal. Nothing here executes a wait or a hold.
- Rework's attempt ceiling is a declared, persisted integer; there is no
  runtime counter enforcing it against a live run, because there is no live
  run.
- Legacy Routes, Forms and Activity execution remain unchanged. Full Procedures
  test suite: Workflow tests pass 12/12; complete suite passes 72 with 10
  intentionally gated skips.

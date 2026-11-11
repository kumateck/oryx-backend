# Approved formula previews

An approved formula edit could display correctly in Edit while the Questions
list displayed the original expression. The editor reads governed revisions,
while the list read the original QuestionOption. That option remains the source
for legacy and historical records.

The paginated question library now projects the effective approved revision's
authoring payload into its response. It resolves the page's formula questions
in one query. A later draft, revision in review or retired revision cannot
replace that preview. Questions with no effective approved payload retain their
legacy preview. This read also supplies the question picker for templates.

The companion desktop renders the saved percentage setting, including the
multiplication by 100, in question and template previews. Successful approval
invalidates cached Questions data. Refused approval keeps the existing cache.

The existing shared-read permission gate remains on GET /api/v1/form/question.
Question viewing uses CanViewQuestions. The template picker retains its existing
template keys, and approval uses CanApproveFormulaRevision. No new grant is
introduced.

Deploy this backend and then the companion desktop through the normal release
process. Refresh Questions and search for the question's name. Compare the
configuration with its approved revision in Edit. The list continues to sort by
the original creation date.

Existing approved edits become visible through this read projection. No data
repair or migration is needed. Stored question options, response snapshots and
approval audit rows stay available for historical review. Rolling back the
application returns to its earlier preview behavior.

Regression checks cover approval and supersession with PostgreSQL audit
triggers, newer unapproved revisions, legacy fallback, missing original options,
unchanged stored options and the existing read gate. The desktop checks cover
percentage settings and cache refresh after successful and refused approval.

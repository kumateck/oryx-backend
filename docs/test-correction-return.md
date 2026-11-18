# Return submitted tests for correction before COA generation

QC checking can return the current submitted product, raw material, or packaging worksheet to **Under test**. A nonblank reason (at most 2000 characters) is required. The original submitted response, formula evidence and authors remain immutable. A linked correction draft retains its answers, question/revision identities, repaired formula configuration, sample and field assignments. Governed formula execution and repaired results must be recalculated in the new cycle; previous finalization evidence is never reused.

Only assigned analysts with current testing permission may edit or resubmit a correction. A certificate is blocked until it is resubmitted. Product analysis actor/date are recorded on resubmission. Material retests reopen their own testing progress and leave the batch frozen. Checked, approved, rejected, superseded, and formal QA rework records cannot use this pre-COA action. Existing post-approval rework remains separate.

The history shows every return's actor, time and reason on both the checking and analyst entry/progress pages. Product history is under Quality Control / Analytical Test Request Products; raw/packaging history is under Quality Control / Goods Receipt Note. Administrators explicitly assign the six new permissions; none are automatically granted:

- CanReturnProductTestForCorrection / CanViewProductTestCorrectionHistory
- CanReturnRawMaterialTestForCorrection / CanViewRawMaterialTestCorrectionHistory
- CanReturnPackagingMaterialTestForCorrection / CanViewPackagingMaterialTestCorrectionHistory

Each variant has a permission-named GET and POST at `/api/v1/test-corrections/{responseId}/product`, `/raw-material` or `/packaging-material`. The server checks the stored kind, current grants and record state; variant paths cannot authorize another kind. Correction and legacy certificate writes share subject locks inside transactions. The unique source response prevents duplicate returns. History and returned response/answer evidence have PostgreSQL write guards, with application guards as well. Deploy `AddTestCorrectionReturns` and the backend before the desktop. No existing records or grants are rewritten.

Validation covers variant deny/allow, reason validation, repeated cycles, copied answers, resubmission gates, retest holds, repaired formula pins, named endpoint policies, catalog registration, and PostgreSQL audit protection/concurrent returns. Frontend tests cover reasons, variant endpoints, history-only access, wrong-kind permission and stale certificate errors. Browser visual verification was unavailable in this chat; no live database or deployment was changed.

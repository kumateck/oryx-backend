# Product batch stem for BMR issue

Product Info accepts an optional batch stem. When issuing a BMR for a product
with stem `LJK`, the operator enters `098` and the application submits the full
batch number `LJK098`. The backend checks the current stem and numeric suffix
on both detail save and issue. The existing duplicate batch number checks apply
to the complete value. Previously issued numbers stay recorded as issued.

Product create, edit and BMR issue use their existing permissions. Legacy update
requests can omit the field and preserve the stored stem. An explicit empty
string clears it. Apply `AddProductBatchStem` before deploying the backend and
then activate the companion desktop release.

The PostgreSQL regression run covered persistence, omitted and cleared stems,
leading zeros, invalid prefixes and updates across BMR, BPR and schedule records.

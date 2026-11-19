"""Run against a LOCAL disposable PostgreSQL server, never a customer database.

PGHOST/PGPORT/PGUSER/PGPASSWORD select the server (libpq defaults apply).
Creates and drops its own uniquely named database. Fixture rows are synthetic;
only the repair's required identifiers, batch/AR and quantity are shared.
"""
import json
import os
from pathlib import Path
import subprocess
import unittest
import uuid

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / 'deploy/repairs/correct-rmv004-to-rmv002.sql'
SOURCE = '01996128-3354-7ddc-9ab8-95055660323d'
TARGET = '01996128-3354-7ce1-a813-e1f8a377ef0e'
BATCH = 'ce058b66-8dab-4d4d-86e6-f7655d481d6e'
LINE = '23eaa748-f373-412d-83c0-62ab264ddf84'
MIGRATION = 'c13fe90f-3b43-4d49-826d-8fdccf081e4b'
DEPARTMENT = '01994fb8-2b57-7b23-a5b9-5b5c68aa9abc'
ACTOR = '10000000-0000-0000-0000-000000000001'
ROLE = '10000000-0000-0000-0000-000000000002'
UNIT = '10000000-0000-0000-0000-000000000003'
WAREHOUSE = '10000000-0000-0000-0000-000000000004'
LOCATION = '10000000-0000-0000-0000-000000000005'
RACK = '10000000-0000-0000-0000-000000000006'
SHELF = '10000000-0000-0000-0000-000000000007'
TARGET_BATCH = '10000000-0000-0000-0000-000000000008'
GRANTS = ['CanViewOpeningBalances', 'CanCorrectOpeningBalances', 'CanReassignRawMaterialStock']


class RepairTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if os.environ.get('PGHOST', 'localhost') not in ('localhost', '127.0.0.1', '::1'):
            raise RuntimeError('Tests require local PostgreSQL; refusing remote server')
        cls.database = 'oryx_rmv004_test_' + uuid.uuid4().hex[:12]
        cls.run_sql('CREATE DATABASE "' + cls.database + '"', database='postgres')
        cls.cluster = cls.run_sql('SELECT system_identifier FROM pg_control_system()').stdout.strip()

    @classmethod
    def tearDownClass(cls):
        cls.run_sql('DROP DATABASE "' + cls.database + '" WITH (FORCE)', database='postgres')

    @classmethod
    def run_sql(cls, sql, database=None, check=True):
        return subprocess.run(['psql', '-X', '-w', '-q', '-A', '-t', '-v', 'ON_ERROR_STOP=1',
                               '-d', database or cls.database, '-c', sql],
                              text=True, capture_output=True, check=check)

    def insert(self, table, **values):
        payload = json.dumps(values).replace("'", "''")
        self.run_sql(f'''INSERT INTO "{table}" SELECT * FROM json_populate_record(
                        NULL::"{table}",'{payload}'::json)''')

    def setUp(self):
        self.run_sql('DROP SCHEMA public CASCADE; CREATE SCHEMA public;')
        self.run_sql((Path(__file__).parent / 'rmv004-schema.sql').read_text())
        # Populate required fields from catalog types, then overlay synthetic data.
        self.columns = json.loads(self.run_sql("""SELECT json_object_agg(table_name,cols) FROM
          (SELECT table_name,json_agg(json_build_object('name',column_name,'type',data_type)) AS cols
           FROM information_schema.columns WHERE table_schema='public' AND is_nullable='NO'
           GROUP BY table_name) t""").stdout)

        def seed(table, **values):
            row = {}
            for col in self.columns[table]:
                kind = col['type']
                row[col['name']] = (str(uuid.uuid4()) if kind == 'uuid' else
                                     '2026-01-01T00:00:00Z' if 'timestamp' in kind else
                                     False if kind == 'boolean' else
                                     0 if kind in ('integer', 'bigint', 'numeric') else 'fixture')
            row.update(values)
            self.insert(table, **row)

        for material, code in [(SOURCE, 'RMV004'), (TARGET, 'RMV002')]:
            seed('Materials', Id=material, Code=code, Name='Synthetic ' + code, Kind=0)
        seed('UnitOfMeasures', Id=UNIT, Symbol='mg')
        seed('users', Id=ACTOR, Email='repair-operator@example.invalid', DepartmentId=DEPARTMENT)
        seed('roles', Id=ROLE)
        self.insert('userroles', UserId=ACTOR, RoleId=ROLE)
        for i, grant in enumerate(GRANTS, 1):
            seed('roleclaims', Id=i, RoleId=ROLE, ClaimType='permission', ClaimValue=grant)
            seed('PermissionTypes', Key=grant, RoleClaimId=i, Type='Access')
        seed('StockMigrations', Id=MIGRATION, DepartmentId=DEPARTMENT, Status=1, Mode=0,
             FileName='synthetic-fixture.xlsx', ApprovedAt='2026-01-01T00:00:00Z')
        seed('MaterialDepartments', MaterialId=TARGET, DepartmentId=DEPARTMENT, UoMId=UNIT)
        seed('Warehouses', Id=WAREHOUSE, DepartmentId=DEPARTMENT, Type=0)
        seed('WarehouseLocations', Id=LOCATION, WarehouseId=WAREHOUSE)
        seed('WarehouseLocationRacks', Id=RACK, WarehouseLocationId=LOCATION)
        seed('WarehouseLocationShelves', Id=SHELF, WarehouseLocationRackId=RACK)
        for batch, material, qty, batch_number, ar, created in [
            (BATCH, SOURCE, 41650000, 'YE-390921', 'NQC/RM/22/016', '2026-01-01T00:00:00Z'),
            (TARGET_BATCH, TARGET, 21440000, 'SYNTHETIC-RMV002', 'TEST/AR/2', '2026-01-02T00:00:00Z')]:
            seed('MaterialBatches', Id=batch, MaterialId=material, BatchNumber=batch_number,
                 UoMId=UNIT, Status=3, TotalQuantity=qty, QuantityAssigned=qty,
                 QuantityPerContainer=qty, NumberOfContainers=1,
                 ManufacturingDate='2025-01-01T00:00:00Z', ExpiryDate='2028-12-31T00:00:00Z')
            seed('ShelfMaterialBatches', MaterialBatchId=batch, WarehouseLocationShelfId=SHELF,
                 UoMId=UNIT, Quantity=qty, Note='Synthetic original shelf evidence')
            seed('BinCardInformation', MaterialBatchId=batch, WarehouseId=WAREHOUSE,
                 UoMId=UNIT, QuantityReceived=qty, QuantityIssued=0, BalanceQuantity=qty,
                 ArNumber=ar, CreatedAt=created, Description='Synthetic opening receipt')
        seed('StockMigrationLines', Id=LINE, StockMigrationId=MIGRATION, RowNumber=263,
             ImportedBatchId=BATCH, MaterialCode='RMV004', ResolvedMaterialId=SOURCE,
             SheetMaterialCode='RMV004', SheetMaterialName='Synthetic original sheet text',
             Quantity=41650000, QuantityInSheet=41650000, UoMId=UNIT,
             BatchNumber='YE-390921', ArNumber='NQC/RM/22/016')
        seed('MassMaterialBatchMovements', BatchId=BATCH, ToWarehouseId=WAREHOUSE,
             Quantity=41650000, MovementType=0, MovedById=ACTOR)
        self.run_sql('''ALTER TABLE "ShelfMaterialBatches" ADD FOREIGN KEY ("MaterialBatchId") REFERENCES "MaterialBatches"("Id");
          ALTER TABLE "BinCardInformation" ADD FOREIGN KEY ("MaterialBatchId") REFERENCES "MaterialBatches"("Id");
          ALTER TABLE "StockMigrationLines" ADD FOREIGN KEY ("ImportedBatchId") REFERENCES "MaterialBatches"("Id");''')

    def repair(self, apply=False, token='', cluster=None, check=True, database_name=None):
        args = ['psql', '-X', '-w', '-q', '-A', '-t', '-d', self.database, '-v', 'ON_ERROR_STOP=1']
        for key, value in dict(apply=None if apply is None else str(apply).lower(), expected_token=token,
                               expected_database=database_name or self.database,
                               expected_system_identifier=self.cluster if cluster is None else cluster,
                               actor_email='repair-operator@example.invalid',
                               reason='Correct stock imported under RMV004 to RMV002').items():
            if value is not None:
                args += ['-v', key + '=' + value]
        result = subprocess.run(args + ['-f', str(SCRIPT)], text=True, capture_output=True)
        if check and result.returncode:
            self.fail(result.stderr)
        return result

    def preview(self):
        result = self.repair()
        return json.loads(next(line for line in result.stdout.splitlines() if line.startswith('{')))

    def material(self):
        return self.run_sql(f'''SELECT "MaterialId" FROM "MaterialBatches" WHERE "Id"='{BATCH}' ''').stdout.strip()

    def test_preview_apply_repeat_and_audit(self):
        report = self.preview()
        self.assertEqual([], report['problems'])
        self.assertEqual(SOURCE, self.material())
        self.assertEqual('', self.run_sql('''SELECT to_regclass('public."_MigrationMaterialCorrections"')''').stdout.strip())
        self.repair(True, report['previewToken'])
        self.assertEqual(TARGET, self.material())
        self.assertEqual('41650000\n63090000', self.run_sql('''SELECT "BalanceQuantity" FROM "BinCardInformation" ORDER BY "CreatedAt","Id"''').stdout.strip())
        self.assertEqual('63090000', self.run_sql('''SELECT sum("Quantity") FROM "ShelfMaterialBatches"''').stdout.strip())
        self.assertEqual('RMV004|RMV002', self.run_sql('''SELECT "SheetMaterialCode","MaterialCode" FROM "StockMigrationLines"''').stdout.strip())
        self.assertIn('already-applied', self.repair(True, 'ignored-on-noop').stdout)
        self.assertEqual('1', self.run_sql('''SELECT count(*) FROM "_MigrationMaterialCorrections"''').stdout.strip())
        self.assertNotEqual(0, self.run_sql('''DELETE FROM "_MigrationMaterialCorrections"''', check=False).returncode)

    def test_stale_token_rolls_back(self):
        report = self.preview()
        self.run_sql('''UPDATE "MaterialBatches" SET "RetestNotes"='changed after preview' ''')
        self.assertNotEqual(0, self.repair(True, report['previewToken'], check=False).returncode)
        self.assertEqual(SOURCE, self.material())

    def test_default_is_preview(self):
        result = self.repair(apply=None)
        self.assertEqual('preview', json.loads(next(line for line in result.stdout.splitlines()
                                                   if line.startswith('{')))['mode'])
        self.assertEqual(SOURCE, self.material())

    def test_late_failure_rolls_back_batch_line_and_ledger(self):
        report = self.preview()
        self.run_sql('''CREATE FUNCTION reject_test_write() RETURNS trigger LANGUAGE plpgsql AS
          $$BEGIN RAISE EXCEPTION 'synthetic late failure'; END$$;
          CREATE TRIGGER reject_test_write BEFORE UPDATE ON "BinCardInformation"
          FOR EACH ROW EXECUTE FUNCTION reject_test_write();''')
        result = self.repair(True, report['previewToken'], check=False)
        self.assertNotEqual(0, result.returncode)
        self.assertIn('synthetic late failure', result.stderr)
        self.assertEqual(SOURCE, self.material())
        self.assertEqual('RMV004', self.run_sql('''SELECT "MaterialCode" FROM "StockMigrationLines"''').stdout.strip())
        self.assertEqual('41650000\n21440000', self.run_sql('''SELECT "BalanceQuantity" FROM "BinCardInformation" ORDER BY "CreatedAt"''').stdout.strip())

    def test_unknown_batch_reference_blocks(self):
        self.run_sql(f'''CREATE TABLE "FutureQcUsage"("Id" uuid,"LotId" uuid REFERENCES "MaterialBatches"("Id"));
                        INSERT INTO "FutureQcUsage" VALUES(gen_random_uuid(),'{BATCH}')''')
        report = self.preview()
        self.assertTrue(any('FutureQcUsage' in p for p in report['problems']))
        self.assertNotEqual(0, self.repair(True, report['previewToken'], check=False).returncode)

    def test_shelf_only_usage_blocks(self):
        self.run_sql(f'''CREATE TABLE "FutureShelfUsage"("ShelfRef" uuid REFERENCES "ShelfMaterialBatches"("Id"));
                        INSERT INTO "FutureShelfUsage" SELECT "Id" FROM "ShelfMaterialBatches" WHERE "MaterialBatchId"='{BATCH}' ''')
        self.assertTrue(any('FutureShelfUsage' in p for p in self.preview()['problems']))

    def test_usage_without_foreign_key_blocks(self):
        self.run_sql(f'''CREATE TABLE "LegacyUsage"("MaterialBatchId" uuid);
                        INSERT INTO "LegacyUsage" VALUES('{BATCH}')''')
        self.assertTrue(any('LegacyUsage' in p for p in self.preview()['problems']))

    def test_permission_revocation_and_department(self):
        report = self.preview()
        self.run_sql('''DELETE FROM "PermissionTypes" WHERE "Key"='CanCorrectOpeningBalances' ''')
        self.assertNotEqual(0, self.repair(True, report['previewToken'], check=False).returncode)
        self.assertEqual(SOURCE, self.material())
        self.run_sql('''UPDATE users SET "DepartmentId"=gen_random_uuid()''')
        self.assertNotEqual(0, self.repair(check=False).returncode)

    def test_wrong_database_cluster_and_token(self):
        report = self.preview()
        for kwargs in [dict(database_name='wrong'), dict(cluster='wrong'), dict(token='wrong')]:
            args = dict(apply=True, token=report['previewToken'], check=False)
            args.update(kwargs)
            self.assertNotEqual(0, self.repair(**args).returncode)
        self.assertEqual(SOURCE, self.material())

    def test_wrong_unit_and_used_quantity_block(self):
        self.run_sql('''UPDATE "UnitOfMeasures" SET "Symbol"='ml';
                        UPDATE "MaterialBatches" SET "ConsumedQuantity"=1''')
        problems = self.preview()['problems']
        self.assertTrue(any('mg department' in p for p in problems))
        self.assertTrue(any('unused' in p for p in problems))

    def test_missing_supplier_batch_identity_blocks(self):
        self.run_sql(f'''UPDATE "MaterialBatches" SET "BatchNumber"=NULL WHERE "Id"='{BATCH}' ''')
        self.assertTrue(any('unused' in p for p in self.preview()['problems']))

    def test_existing_bad_ledger_blocks(self):
        self.run_sql('''UPDATE "BinCardInformation" SET "BalanceQuantity"=0''')
        self.assertTrue(any('bin-card chronology' in p for p in self.preview()['problems']))


if __name__ == '__main__':
    unittest.main(verbosity=2)

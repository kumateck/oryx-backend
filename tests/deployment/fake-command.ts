import { appendFileSync, copyFileSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

interface Fixture { directory: string; sha: string; failure?: string; smtp?: boolean; database?: string; apiContainer?: string; dbContainer?: string }
const fixture: Fixture = JSON.parse(readFileSync(process.argv[2] ?? "", "utf8"));
const tool = process.argv[3];
const args = process.argv.slice(4);
const record = join(fixture.directory, "events.jsonl");
appendFileSync(record, `${JSON.stringify({ tool, args })}\n`);
const fail = (stage: string) => { if (fixture.failure === stage) process.exit(1); };
if (tool === "git") {
  const command = args.slice(2);
  if (command[0] === "remote") process.stdout.write("https://github.com/kumateck/oryx-backend.git\n");
  if (command[0] === "rev-parse") process.stdout.write(`${fixture.sha}\n`);
  if (command[0] === "worktree" && command[1] === "add") {
    const migrations = join(command[3] ?? "", "INFRASTRUCTURE/Migrations");
    mkdirSync(migrations, { recursive: true });
    writeFileSync(join(migrations, "Initial.Designer.cs"), '[Migration("20260101000000_Initial")]');
    // The deployed commit carries the repository's current Compose definition.
    copyFileSync(resolve(dirname(fileURLToPath(import.meta.url)), "../../docker-compose.yml"), join(command[3] ?? "", "docker-compose.yml"));
  }
  if (command[0] === "worktree" && command[1] === "remove") rmSync(command[3] ?? "", { recursive: true, force: true });
} else if (tool === "docker") {
  if (args[0] === "build") fail("build");
  if (args[0] === "inspect" && args.includes("--format")) {
    const format = args[args.indexOf("--format") + 1] ?? "";
    if (format === "{{json .}}") {
      const name = args.at(-1);
      const apiName = fixture.apiContainer ?? "oryx_api";
      const dbName = fixture.dbContainer ?? "postgres_db";
      if (name === apiName) {
        if (fixture.failure === "missing-api") { process.stderr.write(`Error: no such object: ${name}\n`); process.exit(1); }
        const host = fixture.failure === "wrong-db" ? "unrelated-db" : dbName;
        const password = fixture.failure === "quoted-password" ? '"fixture;Host=unrelated-db;Database=wrong"' : "fixture";
        process.stdout.write(JSON.stringify({ Name: `/${apiName}`, Config: { Env: [`connectionString=Host=${host};Port=5432;Username=test;Password=${password};Database=${fixture.database ?? "entrancedb"}`] }, State: { Running: true }, NetworkSettings: { Networks: { sail: {} } } }));
      } else if (name === dbName) {
        process.stdout.write(JSON.stringify({ Name: `/${dbName}`, Config: { Hostname: "db-host" }, State: { Running: true }, NetworkSettings: { Networks: { sail: { Aliases: [dbName], IPAddress: "172.18.0.2" } } } }));
      } else { process.stderr.write(`Error: no such object: ${name}\n`); process.exit(1); }
    } else if (format.includes("compose.project")) process.stdout.write("oryx-backend\n");
    else if (format.includes("compose.service")) process.stdout.write("app\n");
    else process.stdout.write(existsSync(join(fixture.directory, "active")) ? "new-image\n" : "old-image\n");
  }
  if (args[0] === "image") process.stdout.write("new-image\n");
  if (args[0] === "compose") {
    if (args.includes("config")) process.stdout.write(JSON.stringify({ services: { app: { environment: {
      connectionString: `Host=postgres_db;Port=5432;Username=test;Password=fixture;Database=${fixture.database ?? "entrancedb"}`,
      ...(fixture.smtp ? { SMTP_HOST: "mail.example.test", SMTP_USERNAME: "erp@example.test", SMTP_PASSWORD: "fixture" } : { SMTP_HOST: "" }),
    } } } }));
    if (args.includes("run")) {
      if (args.includes("has-pending-model-changes")) fail("model");
      else fail("migration");
    }
    if (args.includes("up")) { fail("activate"); writeFileSync(join(fixture.directory, "active"), "new"); }
  }
  if (args[0] === "exec" && args.some(value => value.includes("exec pg_dump"))) { fail("backup"); process.stdout.write("PGDMPfixture"); }
  if (args[0] === "exec" && args.includes("pg_restore")) { fail("backup-verify"); process.stdout.write("Verified archive\n"); }
  if (args[0] === "exec" && args.some(value => value.includes("exec psql"))) {
    const sql = readFileSync(0, "utf8");
    if (!sql.includes("to_regclass")) fail("sql-apply");
    process.stdout.write(fixture.failure === "unknown-migration" ? "20270101000000_Unknown\n" : "20260101000000_Initial\n");
  }
} else if (tool === "flock" && fixture.failure === "lock") process.exit(75);
else if (tool === "curl") process.stdout.write(fixture.failure === "health" ? "500" : "401");

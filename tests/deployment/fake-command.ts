import { appendFileSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { join } from "node:path";

interface Fixture { directory: string; sha: string; failure?: string; database?: string }
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
  }
  if (command[0] === "worktree" && command[1] === "remove") rmSync(command[3] ?? "", { recursive: true, force: true });
} else if (tool === "docker") {
  if (args[0] === "build") fail("build");
  if (args[0] === "inspect" && args.includes("--format")) {
    const format = args[args.indexOf("--format") + 1] ?? "";
    if (format.includes("compose.project")) process.stdout.write("oryx-backend\n");
    else if (format.includes("compose.service")) process.stdout.write("app\n");
    else process.stdout.write(existsSync(join(fixture.directory, "active")) ? "new-image\n" : "old-image\n");
  }
  if (args[0] === "image") process.stdout.write("new-image\n");
  if (args[0] === "compose") {
    if (args.includes("config")) process.stdout.write(JSON.stringify({ services: { app: { environment: {
      connectionString: `Host=postgres_db;Port=5432;Username=test;Password=fixture;Database=${fixture.database ?? "entrancedb"}`,
    } } } }));
    if (args.includes("run")) fail("migration");
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

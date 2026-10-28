import assert from "node:assert/strict";
import { chmodSync, copyFileSync, existsSync, mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import { createHash } from "node:crypto";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
interface Options { failure?: string; database?: string; apiContainer?: string; dbContainer?: string }
interface Event { tool: string; args: string[] }
function fixture(options: Options = {}) {
  const directory = mkdtempSync(join(tmpdir(), "oryx-backend-deploy-"));
  const repo = join(directory, "repo");
  const state = join(directory, "state");
  const bin = join(directory, "bin");
  const sha = "a".repeat(40);
  mkdirSync(join(repo, "deploy"), { recursive: true });
  mkdirSync(bin);
  copyFileSync(join(root, "deploy/customer-backend.sh"), join(repo, "deploy/customer-backend.sh"));
  copyFileSync(join(root, "deploy.sh"), join(repo, "deploy.sh"));
  copyFileSync(join(root, "docker-compose.yml"), join(repo, "docker-compose.yml"));
  writeFileSync(join(repo, ".env"), "DB_DATABASE=entrancedb\n", { mode: 0o600 });
  const settings = join(directory, "fixture.json");
  const configure = (next: Options) => writeFileSync(settings, JSON.stringify({ directory, sha, ...next }));
  configure(options);
  const quote = (value: string) => `'${value.replaceAll("'", "'\\''")}'`;
  for (const tool of ["git", "docker", "curl", "flock", "sleep"]) {
    const path = join(bin, tool);
    writeFileSync(path, `#!/bin/sh\nexec ${quote(process.execPath)} ${quote(join(root, "tests/deployment/fake-command.ts"))} ${quote(settings)} ${quote(tool)} "$@"\n`);
    chmodSync(path, 0o755);
  }
  const run = (...args: string[]) => spawnSync("bash", [join(repo, "deploy.sh"), ...args], {
    cwd: repo, encoding: "utf8", timeout: 15_000, env: { ...process.env, PATH: `${bin}:${process.env.PATH ?? ""}`, ORYX_BACKEND_STATE: state },
  });
  const events = (): Event[] => existsSync(join(directory, "events.jsonl"))
    ? readFileSync(join(directory, "events.jsonl"), "utf8").trim().split("\n").map(line => JSON.parse(line)) : [];
  return { directory, repo, state, sha, run, events, configure, close: () => rmSync(directory, { recursive: true, force: true }) };
}
const contains = (event: Event, word: string) => event.tool === "docker" && event.args.includes(word);

test("customer command builds main, stops the API, verifies backup, migrates, deploys and records the exact SHA", () => {
  const f = fixture();
  try {
    const result = f.run();
    assert.equal(result.status, 0, result.stderr);
    const events = f.events();
    const index = (word: string) => events.findIndex(event => contains(event, word) && (word !== "run" || !event.args.includes("has-pending-model-changes")));
    assert.ok(index("build") < index("stop"));
    assert.ok(index("stop") < index("pg_restore"));
    assert.ok(index("pg_restore") < index("run"));
    assert.ok(index("run") < index("up"));
    const preflight = events.findIndex(event => contains(event, "has-pending-model-changes"));
    assert.ok(index("build") < preflight && preflight < index("stop"));
    const migration = events.find(event => contains(event, "run") && !event.args.includes("has-pending-model-changes"));
    assert.ok(migration?.args.includes("--no-deps"));
    assert.ok(migration?.args.includes("--rm"));
    assert.ok(migration?.args.some(arg => arg.endsWith("migration.json")));
    assert.equal(readFileSync(join(f.state, "current"), "utf8").trim(), f.sha);
    assert.equal(existsSync(join(f.state, "pending")), false);
    assert.ok(events.some(event => event.tool === "git" && event.args.includes("refs/heads/main:refs/remotes/origin/main")));
    assert.equal(f.run("poll").status, 0);
    assert.equal(f.events().filter(event => contains(event, "build")).length, 2, "unchanged main must not rebuild or migrate");
  } finally { f.close(); }
});

for (const failure of ["build", "model", "backup", "backup-verify", "migration", "activate", "health"]) {
  test(`failure during ${failure} cannot mark the release successful or advance past failed migration`, { timeout: 20_000 }, () => {
    const f = fixture({ failure });
    try {
      const result = f.run("deploy");
      assert.notEqual(result.status, 0, `${result.stdout}\n${result.stderr}`);
      assert.equal(existsSync(join(f.state, "current")), false);
      assert.equal(existsSync(join(f.state, "blocked", f.sha)), true);
      if (["build", "model", "backup", "backup-verify", "migration"].includes(failure)) assert.equal(f.events().some(event => contains(event, "up")), false);
      if (failure === "model") {
        assert.equal(f.events().some(event => contains(event, "stop") || contains(event, "start")), false);
        assert.equal(existsSync(join(f.state, "pending")), false);
      }
      if (["backup", "backup-verify"].includes(failure)) assert.ok(f.events().some(event => contains(event, "start")));
      if (["migration", "activate", "health"].includes(failure)) {
        assert.equal(f.events().some(event => contains(event, "start")), false);
        assert.ok(f.events().filter(event => contains(event, "stop")).length >= 2);
      }
      const count = f.events().length;
      assert.notEqual(f.run("poll").status, 0);
      assert.equal(f.events().slice(count).some(event => contains(event, "run")), false);
      f.configure({});
      assert.equal(f.run("deploy", "--retry").status, 0);
    } finally { f.close(); }
  });
}

test("pause suppresses polling and an overlapping deployment exits before fetching or writing", () => {
  const f = fixture();
  try {
    assert.equal(f.run("pause").status, 0);
    assert.equal(f.run("poll").status, 0);
    assert.equal(f.events().some(event => event.tool === "git"), false);
    assert.equal(f.run("resume").status, 0);
    f.configure({ failure: "lock" });
    assert.equal(f.run("deploy").status, 75);
    assert.equal(f.events().some(event => event.tool === "git"), false);
  } finally { f.close(); }
});

test("unrecognized database targets fail before stopping the installed API", () => {
  const f = fixture({ database: "unexpected;Host=elsewhere" });
  try {
    assert.notEqual(f.run("deploy").status, 0);
    assert.equal(f.events().some(event => contains(event, "stop") || contains(event, "run")), false);
  } finally { f.close(); }
});

test("a database ahead of main cannot be migrated or replaced by that older API", () => {
  const f = fixture({ failure: "unknown-migration" });
  try {
    assert.notEqual(f.run("deploy").status, 0);
    assert.equal(f.events().some(event => contains(event, "build") || contains(event, "stop") || contains(event, "run")), false);
  } finally { f.close(); }
});

for (const failure of ["success", "sql-apply", "tampered-sql", "missing-api", "wrong-db", "quoted-password", "custom-containers"]) {
  test(`standalone SQL package: ${failure}`, { timeout: 20_000 }, () => {
    const custom = failure === "custom-containers";
    const f = fixture({ failure, ...(custom ? { apiContainer: "customer-backend", dbContainer: "customer-postgres" } : {}) });
    const packageDir = join(f.directory, "package");
    mkdirSync(packageDir);
    copyFileSync(join(root, "deploy/apply-customer-migrations.sh"), join(packageDir, "apply.sh"));
    const sql = "-- fixture migration";
    writeFileSync(join(packageDir, "migrations.sql"), failure === "tampered-sql" ? "changed" : sql);
    writeFileSync(join(packageDir, "manifest.json"), JSON.stringify({ sourceSha: f.sha,
      migrations: ["20260101000000_Initial"], sqlSha256: createHash("sha256").update(sql).digest("hex") }));
    try {
      const result = spawnSync("bash", [join(packageDir, "apply.sh"), f.repo], {
        cwd: f.repo, encoding: "utf8", timeout: 15_000,
        env: { ...process.env, PATH: `${join(f.directory, "bin")}:${process.env.PATH ?? ""}`, ORYX_BACKEND_STATE: f.state,
          ...(custom ? { ORYX_BACKEND_API_CONTAINER: "customer-backend", ORYX_BACKEND_DB_CONTAINER: "customer-postgres" } : {}) },
      });
      const success = ["success", "quoted-password", "custom-containers"].includes(failure);
      assert.equal(result.status === 0, success, result.stderr);
      assert.equal(f.events().some(event => contains(event, "build") || contains(event, "up") || contains(event, "start")), false);
      assert.equal(existsSync(join(f.state, "current")), false);
      if (["tampered-sql", "missing-api", "wrong-db"].includes(failure)) {
        assert.equal(f.events().some(event => contains(event, "stop") || contains(event, "exec")), false);
        if (failure === "missing-api") {
          assert.equal((result.stderr.match(/Migration preparation failed/g) ?? []).length, 1);
          assert.match(result.stderr, /no migration SQL was executed/);
        }
      }
      else {
        assert.equal(existsSync(join(f.state, "pending")), true);
        assert.ok(f.events().some(event => contains(event, "pg_restore")));
        if (success) assert.match(result.stdout, /All package migrations installed/);
        if (custom) assert.ok(f.events().some(event => contains(event, "stop") && event.args.includes("customer-backend")));
      }
    } finally { f.close(); }
  });
}

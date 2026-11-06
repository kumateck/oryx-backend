import assert from "node:assert/strict";
import { chmodSync, copyFileSync, cpSync, existsSync, mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import { createHash } from "node:crypto";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
interface Options { failure?: string; smtp?: boolean; database?: string; apiContainer?: string; dbContainer?: string }
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
  copyFileSync(join(root, "deploy/customer-backend-notify.sh"), join(repo, "deploy/customer-backend-notify.sh"));
  copyFileSync(join(root, "deploy/customer-formula.sh"), join(repo, "deploy/customer-formula.sh"));
  copyFileSync(join(root, "deploy.sh"), join(repo, "deploy.sh"));
  copyFileSync(join(root, "docker-compose.yml"), join(repo, "docker-compose.yml"));
  writeFileSync(join(repo, ".env"), "DB_DATABASE=entrancedb\n", { mode: 0o600 });
  const bundle = join(directory, "bundle");
  mkdirSync(bundle);
  writeFileSync(join(bundle, "image.tar"), "fixture-image");
  const manifest = { schemaVersion: "oryx-formula-release-v1", sourceRepository: "kumateck/oryx-live-desktop", sourceSha: sha,
    sourceHash: "b".repeat(64), image: `oryx-formula-calculation:${"b".repeat(64)}`, imageId: `sha256:${"c".repeat(64)}`,
    engineBuildHash: "d".repeat(64), platform: "linux/amd64", numericPolicyVersion: "oryx-decimal-v1-half-up",
    archiveSha256: createHash("sha256").update("fixture-image").digest("hex") };
  writeFileSync(join(bundle, "manifest.json"), JSON.stringify(manifest));
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
    cwd: repo, encoding: "utf8", timeout: 15_000, env: { ...process.env, HOME: directory, PATH: `${bin}:${process.env.PATH ?? ""}`, ORYX_BACKEND_STATE: state, ORYX_FORMULA_RELEASE_DIR: bundle },
  });
  const events = (): Event[] => existsSync(join(directory, "events.jsonl"))
    ? readFileSync(join(directory, "events.jsonl"), "utf8").trim().split("\n").map(line => JSON.parse(line)) : [];
  return { directory, repo, state, sha, run, events, configure, close: () => rmSync(directory, { recursive: true, force: true }) };
}
const contains = (event: Event, word: string) => event.tool === "docker" && event.args.includes(word)
  && !(event.args.includes("formula-calculation") || event.args.includes("--network"));

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
    assert.ok(events.find(event => contains(event, "up"))?.args.includes("--force-recreate"));
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

for (const failure of ["formula-image", "formula-secret", "formula-health", "formula-readiness", "build", "model", "backup", "backup-verify", "migration", "activate", "health", "image"]) {
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
      if (["migration", "activate", "health", "image", "formula-readiness"].includes(failure)) {
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

test("deploys the Compose definition of the deployed commit, not the installed checkout's", () => {
  const f = fixture({ smtp: true });
  try {
    writeFileSync(join(f.repo, "docker-compose.yml"), "services: {}\n");
    const result = f.run("deploy");
    assert.equal(result.status, 0, result.stderr);
    const frozen = readFileSync(join(f.state, "releases", f.sha, "compose.yml"), "utf8");
    assert.equal(frozen, readFileSync(join(root, "docker-compose.yml"), "utf8"));
    assert.doesNotMatch(result.stderr, /email will not work/);
  } finally { f.close(); }
});

test("missing email settings are reported but do not block the deployment", () => {
  const f = fixture();
  try {
    const result = f.run("deploy");
    assert.equal(result.status, 0, result.stderr);
    assert.match(result.stderr, /email will not work\. Missing from .*: SMTP_HOST, SMTP_USERNAME, SMTP_PASSWORD/);
    assert.equal(readFileSync(join(f.state, "current"), "utf8").trim(), f.sha);
  } finally { f.close(); }
});

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

test("a same-SHA retry replaces a stale stopped API container instead of trusting its image tag", () => {
  const f = fixture({ failure: "same-tag-stale" });
  try {
    const result = f.run("deploy");
    assert.equal(result.status, 0, result.stderr);
    const activation = f.events().find(event => contains(event, "up"));
    assert.ok(activation?.args.includes("--force-recreate"));
    assert.ok(activation?.args.includes("--no-build"));
    assert.ok(activation?.args.includes("--no-deps"));
    assert.equal(readFileSync(join(f.state, "current"), "utf8").trim(), f.sha);
  } finally { f.close(); }
});


test("formula dependency is healthy before downtime and backend readiness gates success", () => {
  const f = fixture();
  try {
    assert.equal(f.run("deploy").status, 0);
    const events = f.events();
    const formula = events.findIndex(event => event.args.includes("--wait") && event.args.includes("formula-calculation"));
    const stop = events.findIndex(event => contains(event, "stop"));
    const ready = events.findIndex(event => event.args.includes("--formula-readiness"));
    const up = events.findIndex(event => contains(event, "up"));
    assert.ok(formula >= 0 && formula < stop);
    assert.ok(ready > up);
    const app = JSON.parse(readFileSync(join(f.state, "formula-releases", `${f.sha}-${"c".repeat(64)}`, "formula-app.json"), "utf8"));
    assert.equal(app.services.app.environment.FORMULA_RUNTIME_ENABLED, "true");
    assert.match(app.services.app.environment.FORMULA_SERVICE_BASE_URL, new RegExp(f.sha.slice(0, 16)));
    assert.equal(readFileSync(join(f.state, "formula-current"), "utf8").trim(), `${f.sha}-${"c".repeat(64)}`);
  } finally { f.close(); }
});

test("a corrupt formula bundle cannot stop the current API", () => {
  const f = fixture();
  try {
    writeFileSync(join(f.directory, "bundle", "image.tar"), "corrupt");
    assert.notEqual(f.run("deploy").status, 0);
    assert.equal(f.events().some(event => contains(event, "stop") || contains(event, "up")), false);
  } finally { f.close(); }
});


for (const failure of ["success", "formula-health", "formula-readiness", "formula-readiness-once", "interrupted"]) {
  test(`formula-only rollback preserves backend/schema: ${failure}`, { timeout: 20_000 }, () => {
    const f = fixture();
    try {
      assert.equal(f.run("deploy").status, 0);
      const active = readFileSync(join(f.state, "formula-current"), "utf8").trim();
      const selected = `${"b".repeat(40)}-${"c".repeat(64)}`;
      cpSync(join(f.state, "formula-releases", active), join(f.state, "formula-releases", selected), { recursive: true });
      f.configure({ failure });
      if (failure === "interrupted") writeFileSync(join(f.state, "formula-pending"), selected);
      const before = f.events().length;
      const result = spawnSync("bash", [join(f.repo, "deploy/customer-formula.sh"), "rollback", selected], {
        encoding: "utf8", timeout: 15_000,
        env: { ...process.env, HOME: f.directory, PATH: `${join(f.directory, "bin")}:${process.env.PATH ?? ""}`, ORYX_BACKEND_STATE: f.state },
      });
      const successful = ["success", "interrupted"].includes(failure);
      assert.equal(result.status === 0, successful, result.stderr);
      assert.equal(readFileSync(join(f.state, "current"), "utf8").trim(), f.sha);
      assert.equal(readFileSync(join(f.state, "formula-current"), "utf8").trim(), successful ? selected : active);
      const events = f.events().slice(before);
      assert.equal(events.some(event => contains(event, "build") || contains(event, "run") || contains(event, "pg_restore")), false);
      if (failure === "formula-readiness") {
        assert.equal(existsSync(join(f.state, "formula-pending")), true);
        assert.ok(events.some(event => contains(event, "stop") && event.args.includes("oryx_api")));
        assert.notEqual(f.run("poll").status, 0);
      } else assert.equal(existsSync(join(f.state, "formula-pending")), false);
      if (successful) {
        const activation = events.find(event => contains(event, "up"));
        assert.ok(activation?.args.includes(join(f.state, "formula-releases", selected, "formula-app.json")));
        const overlay = activation?.args.find(arg => arg.endsWith(".api-image.json"));
        assert.ok(overlay);
        assert.equal(JSON.parse(readFileSync(overlay, "utf8")).services.app.image, `sha256:${"e".repeat(64)}`);
      }
    } finally { f.close(); }
  });
}

test("a new formula image for unchanged backend main keeps the old rollback snapshot", { timeout: 20_000 }, () => {
  const f = fixture();
  try {
    assert.equal(f.run("deploy").status, 0);
    const old = readFileSync(join(f.state, "formula-current"), "utf8").trim();
    const manifestPath = join(f.directory, "bundle", "manifest.json");
    const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
    manifest.imageId = `sha256:${"f".repeat(64)}`;
    writeFileSync(manifestPath, JSON.stringify(manifest));
    assert.equal(f.run("deploy").status, 0);
    assert.equal(readFileSync(join(f.state, "current"), "utf8").trim(), f.sha);
    assert.notEqual(readFileSync(join(f.state, "formula-current"), "utf8").trim(), old);
    assert.ok(existsSync(join(f.state, "formula-releases", old, "formula.json")));
    const frozen = JSON.parse(readFileSync(join(f.state, "formula-releases", old, "formula-release.json"), "utf8"));
    assert.equal(frozen.imageId, `sha256:${"c".repeat(64)}`);
  } finally { f.close(); }
});

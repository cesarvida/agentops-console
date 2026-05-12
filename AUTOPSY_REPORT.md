# AgentOps Governance — Autopsy Report

> Generated: 2026-05-12 10:36:53 UTC
> Repo root: `C:\Users\cvida\source\framework\app_console`


## 1. Build & Test Status

- ✅ Solution builds without errors — `0 Errores`
- ✅ All tests pass — `Serie de pruebas para C:\Users\cvida\source\framework\app_console\tests\AgentOps…`

## 2. Governance Workflow Integrity

- ✅ `.github/workflows/governance-check.yml` exists
- ✅ Workflow name is `AI Governance Check`
- ✅ Job name is `Governance Enforcement` (Required Status Check key)
- ✅ Triggers on `pull_request`
- ✅ Azure OpenAI endpoint env var wired in workflow
- ✅ Azure OpenAI API key env var wired in workflow
- ✅ API key sourced from GitHub Secret (not hardcoded)
- ❌ No `|| true` masking failures (enforcement is real)

## 3. Exit Code Contract (Phase 10)

- ✅ CLI sets `Environment.ExitCode = 1` on `FinalStatus == BLOCKED`
- ✅ CLI sets `Environment.ExitCode = 1` on unhandled exception
- ✅ Uses `Environment.ExitCode` (not `Environment.Exit`) — allows async cleanup

## 4. Config & Exceptions Integrity

- ✅ `data/governance-config.yaml` exists
- ✅ Config includes `semantic_analysis` section
- ✅ Config includes `allowed_actions`
- ✅ Config includes `forbidden_actions`
- ✅ Config includes `scoring`
- ✅ `GovernanceException.cs` exists (exception model)
- ❌ Handler applies governance exceptions
- ✅ Rule-based BLOCKED is not overridden by semantic result

## 5. Semantic Analysis Integrity (Phase 11)

- ✅ `IAgentSemanticAnalyzer` interface exists
- ✅ `AzureOpenAIGovernanceClient` implementation exists
- ✅ `SemanticAnalysisResult` model exists
- ✅ Client reads endpoint from variable (not hardcoded)
- ❌ API key is not logged
- ✅ Client handles timeout gracefully
- ✅ Client returns Skipped on error (no crash)
- ✅ Options reads `AZURE_OPENAI_ENDPOINT`
- ✅ Options reads `AZURE_OPENAI_API_KEY`
- ✅ Options reads deployment name
- ✅ SemanticAnalyzer is optional (null default — no crash when absent)
- ✅ Semantic analysis is gated by `Enabled` flag in config

## 6. Secret Hygiene

- ✅ `.env.example` exists
- ✅ .env.example contains `AZURE_OPENAI_ENDPOINT`
- ✅ .env.example contains `AZURE_OPENAI_API_KEY`
- ✅ .env.example contains `AZURE_OPENAI_DEPLOYMENT_NAME`
- ✅ .env.example has empty `AZURE_OPENAI_API_KEY` (placeholder only)
- ✅ `.gitignore` contains `.env`
- ✅ `.gitignore` covers local secrets files

**Secret scan** — scanning tracked source files for accidentally committed secrets:

> ⚠️ **Warning:** 5 potential secret location(s) found. Review carefully:

```
  [Azure cognitive services key pattern] CLOSURE_SUMMARY.md:12 → **Commit Hash:** `315d***a346839e02d7` (completo)
  [Hardcoded password assignment] README.md:44 → - Passwords: `var pass***ord"`
  [BEGIN RSA PRIVATE KEY] VALIDATION_GUIDE.md:106 → - ✅ Detecta: `----***----`
  [sk- OpenAI key] docs\SECURITY.md:108 → "Use my API key sk-1***ghij for authentication"
  [Azure cognitive services key pattern] tests\AgentOps.Application.Tests\EvaluateAgentBehaviorHandlerTests.cs:126 → public const string FakeDigest = "aabb***8899aabb***8899";
```

## 7. GitHub Readiness Checklist (manual steps)

The following must be verified in the GitHub repository settings:

- [ ] Branch protection rule enabled for `main`
- [ ] **Require status checks to pass before merging** enabled
- [ ] Required status check: `AI Governance Check / Governance Enforcement` added
- [ ] **Require branches to be up to date** enabled
- [ ] GitHub Secret `AZURE_OPENAI_ENDPOINT` set (optional — semantic degrades gracefully)
- [ ] GitHub Secret `AZURE_OPENAI_API_KEY` set (optional)
- [ ] GitHub Secret `AZURE_OPENAI_DEPLOYMENT_NAME` set (optional, default: `gpt-5.4-nano`)
- [ ] `GITHUB_TOKEN` auto-provided by Actions (no manual secret needed)

## 8. Risks & Recommendations

| Risk | Severity | Recommendation |
|---|---|---|
| Semantic analysis disabled when Azure secrets not set | LOW | Expected fallback — rule-based governance still runs |
| governance-config.yaml only loaded from GitHub API in dashboard mode | MEDIUM | `validate-agent` uses local file — ensure it is committed and up to date |
| Exit code contract depends on FinalStatus string match | LOW | String is set centrally in GovernanceRuleEngine — low risk of drift |
| governance-check.yml path filter removed (all PRs scanned) | INFO | Correct — ensures Required Status Check always appears in PR checks list |

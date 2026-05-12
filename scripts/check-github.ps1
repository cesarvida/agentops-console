#Requires -Version 5.1
<#
.SYNOPSIS
    Verifies GitHub branch protection and required status checks for AgentOps.

.DESCRIPTION
    Uses the 'gh' CLI to check that the 'main' branch has the
    "AI Governance Check / Governance Enforcement" required status check configured.
    If 'gh' is not available, prints manual instructions.

.PARAMETER Repo
    GitHub repository in the form owner/repo.
    Defaults to the origin remote of the current directory.

.EXAMPLE
    .\scripts\check-github.ps1
    .\scripts\check-github.ps1 -Repo "myorg/agentops"
#>

param(
    [string]$Repo = ""
)

$RequiredCheck = "AI Governance Check / Governance Enforcement"
$Branch        = "main"

# ── Resolve repo ──────────────────────────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($Repo)) {
    try {
        $remoteUrl = git remote get-url origin 2>$null
        if ($remoteUrl -match "github\.com[:/](.+?)(?:\.git)?$") {
            $Repo = $Matches[1]
        }
    } catch { }
}

if ([string]::IsNullOrWhiteSpace($Repo)) {
    Write-Host "Usage: .\scripts\check-github.ps1 -Repo 'owner/repo'"
    Write-Host "       Or run from inside a git repository with a GitHub remote."
    exit 1
}

Write-Host ""
Write-Host "Repository    : $Repo"
Write-Host "Branch        : $Branch"
Write-Host "Required check: $RequiredCheck"
Write-Host ""

# ── Check gh availability ─────────────────────────────────────────────────────
$ghPath = Get-Command gh -ErrorAction SilentlyContinue
if (-not $ghPath) {
    Write-Host "⚠️  'gh' CLI not found. Install from https://cli.github.com/"
    Write-Host ""
    Write-Host "Manual steps to verify branch protection:"
    Write-Host "  1. Open https://github.com/$Repo/settings/branches"
    Write-Host "  2. Edit the 'main' branch protection rule"
    Write-Host "  3. Enable 'Require status checks to pass before merging'"
    Write-Host "  4. Add '$RequiredCheck' to the required checks list"
    exit 0
}

# ── Fetch branch protection ───────────────────────────────────────────────────
Write-Host "Fetching branch protection via GitHub API..."
try {
    $protectionJson = gh api "repos/$Repo/branches/$Branch/protection" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw $protectionJson
    }
    $protection = $protectionJson | ConvertFrom-Json
}
catch {
    Write-Host ""
    Write-Host "❌ Could not fetch branch protection."
    Write-Host "   Error: $_"
    Write-Host ""
    Write-Host "Possible reasons:"
    Write-Host "  - Branch protection is not enabled for '$Branch'"
    Write-Host "  - You lack admin access to the repository"
    Write-Host "  - The branch does not exist"
    exit 0
}

# ── Check required status checks ─────────────────────────────────────────────
$contexts = @()
try {
    $contexts = $protection.required_status_checks.contexts
} catch { }

if ($contexts -contains $RequiredCheck) {
    Write-Host "✅ Required status check '$RequiredCheck' is configured."
} else {
    Write-Host "❌ Required status check '$RequiredCheck' NOT found."
    Write-Host ""
    Write-Host "Currently configured checks:"
    if ($contexts -and $contexts.Count -gt 0) {
        foreach ($c in $contexts) { Write-Host "  - $c" }
    } else {
        Write-Host "  (none configured)"
    }
    Write-Host ""
    Write-Host "Action: Add '$RequiredCheck' at:"
    Write-Host "  https://github.com/$Repo/settings/branches"
}

# ── Check enforce_admins ──────────────────────────────────────────────────────
try {
    $enforceAdmins = $protection.enforce_admins.enabled
    if ($enforceAdmins) {
        Write-Host "✅ Branch protection enforced for admins."
    } else {
        Write-Host "⚠️  Branch protection is NOT enforced for admins (enforce_admins=false)."
    }
} catch {
    Write-Host "ℹ️  Could not determine enforce_admins status."
}

# ── Check GitHub Secrets ──────────────────────────────────────────────────────
Write-Host ""
Write-Host "Checking GitHub Secrets..."

function Test-GhSecret {
    param([string]$Name)
    $list = gh secret list --repo $Repo 2>$null
    if ($LASTEXITCODE -eq 0 -and $list -match "\b$Name\b") {
        Write-Host "  ✅ Secret '$Name' is set."
    } else {
        Write-Host "  ⚠️  Secret '$Name' not found (semantic analysis will degrade gracefully)."
    }
}

Test-GhSecret "AZURE_OPENAI_ENDPOINT"
Test-GhSecret "AZURE_OPENAI_API_KEY"
Test-GhSecret "AZURE_OPENAI_DEPLOYMENT_NAME"

Write-Host ""
Write-Host "Done."

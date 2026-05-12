#!/usr/bin/env bash
# scripts/check-github.sh
# Verify GitHub branch protection and required status checks.
# Usage: ./scripts/check-github.sh [owner/repo]
# Requires: gh CLI authenticated with repo scope.

set -euo pipefail

REQUIRED_CHECK="AI Governance Check / Governance Enforcement"
BRANCH="main"

# ── Resolve repo ─────────────────────────────────────────────────────────────
if [[ $# -ge 1 ]]; then
  REPO="$1"
else
  # Try to derive from git remote
  REPO=$(git remote get-url origin 2>/dev/null \
         | sed -E 's|.*github\.com[:/]||' \
         | sed 's|\.git$||') || REPO=""
fi

if [[ -z "$REPO" ]]; then
  echo "Usage: $0 <owner/repo>"
  echo "       Or run from inside a git repository with a GitHub remote."
  exit 1
fi

echo "Repository : $REPO"
echo "Branch     : $BRANCH"
echo "Required check: $REQUIRED_CHECK"
echo ""

# ── Check gh availability ─────────────────────────────────────────────────────
if ! command -v gh &>/dev/null; then
  echo "⚠️  'gh' CLI not found. Install from https://cli.github.com/"
  echo ""
  echo "Manual steps to verify branch protection:"
  echo "  1. Open https://github.com/$REPO/settings/branches"
  echo "  2. Edit the 'main' branch protection rule"
  echo "  3. Ensure 'Require status checks to pass before merging' is enabled"
  echo "  4. Ensure '$REQUIRED_CHECK' is in the required checks list"
  exit 0
fi

# ── Fetch branch protection ───────────────────────────────────────────────────
echo "Fetching branch protection via GitHub API..."
PROTECTION=$(gh api "repos/$REPO/branches/$BRANCH/protection" 2>&1) || {
  echo "❌ Could not fetch branch protection."
  echo "   Error: $PROTECTION"
  echo ""
  echo "Possible reasons:"
  echo "  - Branch protection is not enabled for '$BRANCH'"
  echo "  - You lack admin access to the repository"
  echo "  - The branch does not exist"
  exit 0
}

# ── Check required status checks ─────────────────────────────────────────────
CONTEXTS=$(echo "$PROTECTION" \
  | gh api --jq '.required_status_checks.contexts[]' /dev/stdin 2>/dev/null \
  || echo "$PROTECTION" | python3 -c "
import sys, json
data = json.load(sys.stdin)
checks = data.get('required_status_checks', {})
contexts = checks.get('contexts', [])
for c in contexts:
    print(c)
" 2>/dev/null || true)

if echo "$CONTEXTS" | grep -qF "$REQUIRED_CHECK"; then
  echo "✅ Required status check '$REQUIRED_CHECK' is configured."
else
  echo "❌ Required status check '$REQUIRED_CHECK' NOT found."
  echo ""
  echo "Currently configured checks:"
  if [[ -n "$CONTEXTS" ]]; then
    echo "$CONTEXTS" | while IFS= read -r line; do echo "  - $line"; done
  else
    echo "  (none, or could not parse)"
  fi
  echo ""
  echo "Action: Add '$REQUIRED_CHECK' in:"
  echo "  https://github.com/$REPO/settings/branches"
fi

# ── Check enforce_admins ───────────────────────────────────────────────────────
ENFORCE=$(echo "$PROTECTION" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(data.get('enforce_admins', {}).get('enabled', False))
" 2>/dev/null || echo "unknown")

if [[ "$ENFORCE" == "True" ]]; then
  echo "✅ Branch protection enforced for admins."
else
  echo "⚠️  Branch protection is NOT enforced for admins (enforce_admins=false)."
fi

# ── Check secrets ─────────────────────────────────────────────────────────────
echo ""
echo "Checking GitHub Secrets..."
check_secret() {
  local name="$1"
  if gh secret list --repo "$REPO" 2>/dev/null | grep -qw "$name"; then
    echo "  ✅ Secret '$name' is set."
  else
    echo "  ⚠️  Secret '$name' not found (semantic analysis will be skipped gracefully)."
  fi
}

check_secret "AZURE_OPENAI_ENDPOINT"
check_secret "AZURE_OPENAI_API_KEY"
check_secret "AZURE_OPENAI_DEPLOYMENT_NAME"

echo ""
echo "Done."

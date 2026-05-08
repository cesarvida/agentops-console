$trackedFile = ".git-tracked-files.txt"
if (-Not (Test-Path $trackedFile)) {
    Write-Error "Tracked file list not found: $trackedFile"
    exit 1
}
$pattern = '(bin/|obj/)|\.dll$|\.pdb$|(^|/)\.vscode(/|$)'
Get-Content $trackedFile | ForEach-Object {
    $line = $_.Trim()
    if ($line -match $pattern) {
        Write-Host "Removing from index: $line"
        git rm --cached -- "$line" | Out-Null
    }
}
Write-Host "Staging .gitignore and committing"
git add .gitignore
try {
    git commit -m "chore: add .gitignore and remove build artifacts from repo" | Out-Null
    Write-Host "Commit successful"
} catch {
    Write-Host "No changes to commit or commit failed: $_"
}
Write-Host "Git status:"; git status -sb
Write-Host "Git remotes:"; git remote -v

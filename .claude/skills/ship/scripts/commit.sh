#!/usr/bin/env bash
# commit.sh — commit staged (or all) SurveHive changes with the house identity
# and Claude co-author trailer, so the trailer/author are never forgotten.
#
# Usage:
#   commit.sh "subject line" ["body paragraph" ...]   # commits already-staged files
#   commit.sh -a "subject" ["body" ...]               # git add -A first, then commit
#
# Author is the repo's configured user (Ron Ariel); this only guarantees the
# trailer. Never runs push — pushing is a separate, explicit step (see SKILL.md).
set -euo pipefail
TRAILER="Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"

addall=0
if [[ "${1:-}" == "-a" ]]; then addall=1; shift; fi
[[ $# -ge 1 ]] || { echo "usage: commit.sh [-a] \"subject\" [\"body\" ...]" >&2; exit 2; }

subject="$1"; shift
msg="$subject"
for para in "$@"; do msg+=$'\n\n'"$para"; done
msg+=$'\n\n'"$TRAILER"

[[ $addall -eq 1 ]] && git add -A
git diff --cached --quiet && { echo "commit.sh: nothing staged — aborting" >&2; exit 1; }

git commit -m "$msg"
echo "--- committed ---"; git log -1 --format='%h %an: %s'

---
name: ship
description: Commit (and optionally branch/push/merge) SurveHive work the way Ron does it — correct author + Claude co-author trailer, feature-focused messages, never auto-committing. Use when the user says "commit", "commit and push", "push the commits", or asks to branch/merge work.
---

# Shipping SurveHive changes (commit / push / branch)

## The one hard rule: never auto-commit

Committing requires the user's **explicit** OK — finishing a phase or a fix is not
permission. If they haven't clearly said commit/push, stop and ask. When they have said it,
that invocation IS the go-ahead; proceed without re-asking.

## Commit

Use the helper so the author + trailer are never dropped:

```
scripts/commit.sh "subject line" "body paragraph" ...        # commits staged files
scripts/commit.sh -a "subject" "body" ...                    # git add -A first
```

It appends `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` and commits under the
repo's configured author (Ron Ariel — already set as `user.name`; if a fresh clone lacks it,
`git config user.name "Ron Ariel"`). Review `git status`/`git diff` before staging so Unity
`.meta`/`ProjectSettings` residue is intentional, not accidental.

### Message conventions (match the existing log)

- **Feature / phase commits:** subject = what shipped, framed as capability. For a plan
  sub-phase, lead with its id: `Phase 2B: impactful miniboss kill — guaranteed lucky reward`.
  Body = bulleted **major features/behaviour changes**. When the user asks for a
  "features summary", list *what was added*, not the bugs fixed along the way.
- **Docs-only commits:** `docs:` prefix — e.g. `docs: Phase 2 complete — README + CHANGELOG + PLAN`.
- Keep the subject tight; put detail in the body. No CHANGELOG duplication in the message.

## Push (only when asked)

`git push` is a separate explicit step — never bundle it into a commit request unless the user
said "commit and push" / "push". Then: `git push origin <branch>`.

## Branch / merge (when the user wants a feature branch)

The occasional flow Ron uses:
```
git checkout -b feature/<slug>       # branch off main
scripts/commit.sh -a "subject" ...   # commit on the branch
git push origin feature/<slug>       # if asked to push
git checkout main && git merge --ff-only feature/<slug> && git push origin main   # if asked to merge back
```
Default is committing straight on the current branch — only branch when the user asks for it.
Prefer `--ff-only` merges to keep history linear.

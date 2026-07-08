#!/usr/bin/env bash
# unity.sh — portable, lock-safe headless-Unity runner for SurveHive.
#
# One entry point for every batch Unity task so the long invocations and the
# lock/zombie-safety logic live in exactly one place.
#
# Usage:
#   unity.sh build <BuilderClass>        # -executeMethod SurveHive.BuildTools.<Builder>.Apply  (batch)
#   unity.sh method <Fully.Qualified.Method>   # arbitrary -executeMethod (batch)
#   unity.sh validate                    # BeehiveSceneValidator.Validate + grep verdict (batch)
#   unity.sh test <EditMode|PlayMode> [filter]   # -runTests, parses the results XML (batch);
#                                        # optional filter → -testFilter (also runs [Explicit] tests)
#   unity.sh drive [Method]              # PlayModeVerifyDriver.Run — NO -batchmode (opens GUI ~1min)
#   unity.sh lock                        # report editor/lock state, then exit
#
# Env overrides: UNITY_BIN, PROJECT (defaults: known 6000.5.2f1 path / this repo root).
#
# NOTE: run the batch subcommands with the harness `run_in_background: true` — a
# batch pass takes 1-3 min and can hang; never wrap it in a foreground timeout
# (SIGKILL mid-boot corrupts the lock). This script never kills anything itself.
set -uo pipefail

UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity}"
# Resolve project root: env, else two dirs up from this script (…/.claude/skills/verify/scripts).
if [[ -z "${PROJECT:-}" ]]; then
  PROJECT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
fi
LOGDIR="${LOGDIR:-$PROJECT/VerifyShots/logs}"; mkdir -p "$LOGDIR"

die(){ echo "unity.sh: $*" >&2; exit 3; }
[[ -x "$UNITY_BIN" ]] || die "Unity binary not found at $UNITY_BIN (set UNITY_BIN)"
[[ -d "$PROJECT/Assets" ]] || die "no Assets/ under PROJECT=$PROJECT (set PROJECT)"

# --- lock / editor guard --------------------------------------------------
# The user's GUI editor shows up with -useHub/-hubIPC in its args; a stray
# headless run of mine shows -batchmode -executeMethod / -runTests. Batch
# subcommands must refuse to run while the GUI editor holds the lock.
editor_open(){ pgrep -fl "Unity.app/Contents/MacOS/Unity" 2>/dev/null | grep -q -- "-useHub"; }
batch_running(){ pgrep -fl "Unity.app/Contents/MacOS/Unity" 2>/dev/null | grep -Eq -- "-batchmode|-runTests"; }

report_lock(){
  if editor_open; then echo "LOCK: user GUI editor is OPEN (do not batch, do not kill it)";
  elif batch_running; then echo "LOCK: a headless batch run is in progress";
  elif [[ -f "$PROJECT/Temp/UnityLockfile" ]]; then echo "LOCK: lockfile present, no live process (stale — safe after checking)";
  else echo "LOCK: free"; fi
}
guard_batch(){ editor_open && die "GUI editor is open — close it before headless batch runs (needs the lock)"; return 0; }

run_unity(){ # run_unity <logfile> <args...>
  local log="$1"; shift
  echo "→ $UNITY_BIN $* (log: $log)"
  "$UNITY_BIN" "$@" -logFile "$log"
  return $?
}

cmd="${1:-}"; shift || true
case "$cmd" in
  lock) report_lock ;;

  build)
    b="${1:?usage: unity.sh build <BuilderClass>}"
    case "$b" in *.*) m="$b";; *) m="SurveHive.BuildTools.$b.Apply";; esac
    guard_batch
    log="$LOGDIR/build-${b##*.}.log"
    run_unity "$log" -batchmode -quit -projectPath "$PROJECT" -executeMethod "$m"
    rc=$?; echo "exit=$rc"; tail -n 25 "$log"; exit $rc ;;

  method)
    m="${1:?usage: unity.sh method <Fully.Qualified.Method>}"; guard_batch
    log="$LOGDIR/method-${m##*.}.log"
    run_unity "$log" -batchmode -quit -projectPath "$PROJECT" -executeMethod "$m"
    rc=$?; echo "exit=$rc"; tail -n 25 "$log"; exit $rc ;;

  validate)
    guard_batch
    log="$LOGDIR/validate.log"
    run_unity "$log" -batchmode -quit -projectPath "$PROJECT" \
      -executeMethod SurveHive.BuildTools.BeehiveSceneValidator.Validate
    rc=$?
    echo "--- validator verdict ---"; grep -nE "\[FAIL\]|validation PASSED|validation FAILED" "$log" || echo "(no verdict line — check $log)"
    exit $rc ;;

  test)
    plat="${1:?usage: unity.sh test <EditMode|PlayMode> [filter]}"; filter="${2:-}"; guard_batch
    xml="$LOGDIR/tests-$plat.xml"; log="$LOGDIR/tests-$plat.log"
    extra=(); [[ -n "$filter" ]] && extra=(-testFilter "$filter")
    run_unity "$log" -runTests -batchmode -projectPath "$PROJECT" -testPlatform "$plat" -testResults "$xml" \
      ${extra[@]+"${extra[@]}"}
    rc=$?  # rc==2 means test failures; parse the XML either way
    echo "exit=$rc"
    if [[ -f "$xml" ]]; then
      python3 - "$xml" <<'PY'
import sys,xml.etree.ElementTree as ET
r=ET.parse(sys.argv[1]).getroot()
g=lambda k:r.get(k,'?')
print(f"tests: total={g('total')} passed={g('passed')} failed={g('failed')} skipped={g('skipped')}")
for tc in r.iter('test-case'):
    if tc.get('result')not in('Passed','Skipped','Inconclusive'):
        print("  FAIL:",tc.get('fullname'))
PY
    else echo "(no results XML — build/compile likely failed; see $log)"; fi
    exit $rc ;;

  drive)  # visual play-mode capture — NO -batchmode; game view must render
    m="${1:-SurveHive.BuildTools.PlayModeVerifyDriver.Run}"
    guard_batch
    log="$LOGDIR/drive.log"
    run_unity "$log" -projectPath "$PROJECT" -executeMethod "$m"
    rc=$?; echo "exit=$rc — screenshots in $PROJECT/VerifyShots/"; tail -n 20 "$log"; exit $rc ;;

  ""|-h|--help|help)
    grep -E '^#( |$)' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//' ;;
  *) die "unknown subcommand '$cmd' (try: build validate test drive lock)";;
esac

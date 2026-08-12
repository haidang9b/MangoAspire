#!/usr/bin/env bash
#
# Runs Trivy from its official container image. Every scan in .github/workflows/
# goes through here so the docker invocation, the cache location and the severity
# policy are defined once instead of being copy-pasted into a dozen steps.
#
# Usage:
#   trivy-scan.sh sarif <kind> <target> <output.sarif> [extra trivy flags...]
#   trivy-scan.sh gate  <kind> <target>                [extra trivy flags...]
#
#   trivy-scan.sh sarif fs    src/UI/mango-ui  trivy-npm.sarif
#   trivy-scan.sh gate  fs    src              --skip-dirs src/UI/mango-ui
#   trivy-scan.sh sarif image nginx:alpine     trivy-base-nginx.sarif
#   trivy-scan.sh gate  tar   image.tar
#
# Note the argument count differs by mode: sarif takes an output path where gate
# does not, so anything after it is passed through to Trivy verbatim.
#
# Modes:
#   sarif  Report everything, always exit 0. Feeds the Security > Code scanning
#          tab. Must not fail, or the upload step that follows it never runs and
#          the findings disappear along with the failure that caused them.
#   gate   HIGH/CRITICAL only, exit 1 on a hit. This is what blocks a merge.
#
# Kinds:
#   fs     A directory in the working tree (lockfiles, *.deps.json).
#   image  An image reference; Trivy pulls it from the registry itself.
#   tar    A "docker save" tarball, so locally-built images can be scanned
#          without handing the container the Docker socket.
#
set -euo pipefail

MODE=${1:?expected mode: sarif|gate}
KIND=${2:?expected kind: fs|image|tar}
TARGET=${3:?expected a directory, image reference, or tar path}
shift 3

OUTPUT=""
if [ "$MODE" = "sarif" ]; then
  OUTPUT=${1:?sarif mode needs an output path}
  shift
fi

# Whatever is left goes to Trivy untouched (--skip-dirs, --timeout, ...).
extra=("$@")

# Pinned by digest-equivalent tag rather than :latest — a scanner that silently
# changes version between runs makes "it was green yesterday" unanswerable.
TRIVY_IMAGE=${TRIVY_IMAGE:-ghcr.io/aquasecurity/trivy:0.73.0}
# The default DB host (ghcr.io/aquasecurity/trivy-db) rate-limits anonymous pulls
# and is the usual reason a Trivy job fails with no code change behind it.
DB_REPOSITORY=${TRIVY_DB_REPOSITORY:-public.ecr.aws/aquasecurity/trivy-db:2}
CACHE_DIR=${TRIVY_CACHE_DIR:-.trivycache}

mkdir -p "$CACHE_DIR"

common=(
  --cache-dir "$CACHE_DIR"
  --db-repository "$DB_REPOSITORY"
  # Secrets are gitleaks' job in the sibling workflow; misconfig and licence
  # scanning are separate concerns with separate triage owners.
  --scanners vuln
  --no-progress
)

case "$KIND" in
  fs)    subcommand=(fs);    positional=("$TARGET") ;;
  image) subcommand=(image); positional=("$TARGET") ;;
  tar)   subcommand=(image); positional=(--input "$TARGET") ;;
  *)     echo "::error::unknown kind '$KIND' (expected fs|image|tar)" >&2; exit 2 ;;
esac

case "$MODE" in
  sarif)
    # No --severity on purpose. Unlike the trivy-action wrapper, the CLI *does*
    # apply --severity to SARIF, so adding it here would hide MEDIUM and below
    # from the Security tab. Everything is reported for triage; only the gate
    # below decides what blocks a merge.
    mode_args=(--format sarif --output "$OUTPUT" --exit-code 0)
    ;;
  gate)
    mode_args=(
      --format table
      --severity HIGH,CRITICAL
      --exit-code 1
      # No fixed version published means there is nothing to upgrade to, so
      # blocking an unrelated pull request achieves nothing. Still reported by
      # the sarif pass above.
      --ignore-unfixed
      # Suppressions apply to the gate only, never to the SARIF report, so an
      # accepted risk stops blocking merges without vanishing from the audit.
      --ignorefile .trivyignore
    )
    ;;
  *) echo "::error::unknown mode '$MODE' (expected sarif|gate)" >&2; exit 2 ;;
esac

# --user keeps the SARIF file and the cache owned by the runner account. Left as
# root, actions/cache cannot read Trivy's 0700 cache directory and the DB is
# re-downloaded on every job.
exec docker run --rm \
  --user "$(id -u):$(id -g)" \
  --volume "$PWD:/workspace" \
  --workdir /workspace \
  "$TRIVY_IMAGE" \
  "${subcommand[@]}" "${common[@]}" "${mode_args[@]}" ${extra[@]+"${extra[@]}"} "${positional[@]}"

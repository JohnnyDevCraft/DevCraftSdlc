#!/usr/bin/env sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
TMP_DIR="${TMPDIR:-/tmp}/devcraft-installer-profile-test-$$"
export TMP_DIR
export DEVCRAFT_INSTALLER_TEST_MODE=1

. "$ROOT_DIR/install.sh"

trap 'rm -rf "$TMP_DIR"' EXIT

assert_equal() {
    expected="$1"
    actual="$2"
    description="$3"

    if [ "$expected" != "$actual" ]; then
        printf 'FAIL: %s\nexpected: %s\nactual:   %s\n' "$description" "$expected" "$actual" >&2
        exit 1
    fi
}

source_profile="$TMP_DIR/package/profile"
mkdir -p "$source_profile/features" "$source_profile/templates"
printf 'rules' > "$source_profile/DevCraft.md"
printf 'template' > "$source_profile/templates/spec.template.md"
printf 'package soul' > "$source_profile/soul.md"
printf 'package configuration' > "$source_profile/configure.json"
printf 'package projects' > "$source_profile/features/projects.json"

INSTALL_DIR="$TMP_DIR/existing"
mkdir -p "$INSTALL_DIR/features"
printf 'existing soul' > "$INSTALL_DIR/soul.md"
printf 'existing configuration' > "$INSTALL_DIR/configure.json"
printf 'existing projects' > "$INSTALL_DIR/features/projects.json"
copy_profile "$source_profile"

assert_equal "rules" "$(cat "$INSTALL_DIR/DevCraft.md")" "copies profile files"
assert_equal "template" "$(cat "$INSTALL_DIR/templates/spec.template.md")" "copies nested profile files"
assert_equal "existing soul" "$(cat "$INSTALL_DIR/soul.md")" "preserves soul.md"
assert_equal "existing configuration" "$(cat "$INSTALL_DIR/configure.json")" "preserves an existing configure.json"
assert_equal "existing projects" "$(cat "$INSTALL_DIR/features/projects.json")" "preserves an existing features/projects.json"

INSTALL_DIR="$TMP_DIR/fresh"
copy_profile "$source_profile"

assert_equal "package configuration" "$(cat "$INSTALL_DIR/configure.json")" "seeds configure.json on a fresh install"
assert_equal "package projects" "$(cat "$INSTALL_DIR/features/projects.json")" "seeds features/projects.json on a fresh install"

printf 'installer-profile-copy.sh passed\n'

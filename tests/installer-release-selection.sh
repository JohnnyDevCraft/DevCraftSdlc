#!/usr/bin/env sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
TMP_DIR="${TMPDIR:-/tmp}/devcraft-installer-test-$$"
export TMP_DIR
export DEVCRAFT_INSTALLER_TEST_MODE=1

. "$ROOT_DIR/install.sh"

mkdir -p "$TMP_DIR"
metadata="$TMP_DIR/releases.json"

cat > "$metadata" <<'JSON'
[
  { "tag_name": "v1.0.0-alpha.12", "draft": false },
  { "tag_name": "v1.0.0-alpha.9", "draft": false },
  { "tag_name": "v1.0.0-beta.1", "draft": false },
  { "tag_name": "v1.0.0-beta.2", "draft": false },
  { "tag_name": "v1.0.0-beta.5", "draft": false },
  { "tag_name": "v1.0.0-beta.6", "draft": false },
  { "tag_name": "v1.0.0-beta.7", "draft": false },
  { "tag_name": "v1.0.0-beta.8", "draft": false },
  { "tag_name": "v0.9.0", "draft": false }
]
JSON

assert_equal() {
    expected="$1"
    actual="$2"
    description="$3"

    if [ "$expected" != "$actual" ]; then
        printf 'FAIL: %s\nexpected: %s\nactual:   %s\n' "$description" "$expected" "$actual" >&2
        exit 1
    fi
}

assert_true() {
    description="$1"
    shift

    if ! "$@"; then
        printf 'FAIL: %s\n' "$description" >&2
        exit 1
    fi
}

assert_equal "v1.0.0-beta.8" "$(select_release_tag_from_metadata "$metadata")" "selects newest beta over alpha releases"
assert_true "beta sorts after alpha 12" version_greater "v1.0.0-beta.1" "v1.0.0-alpha.12"
assert_true "beta 2 sorts after beta 1" version_greater "v1.0.0-beta.2" "v1.0.0-beta.1"
assert_true "beta 5 sorts after beta 2" version_greater "v1.0.0-beta.5" "v1.0.0-beta.2"
assert_true "beta 7 sorts after beta 6" version_greater "v1.0.0-beta.7" "v1.0.0-beta.6"
assert_true "beta 8 sorts after beta 7" version_greater "v1.0.0-beta.8" "v1.0.0-beta.7"
assert_true "alpha 12 sorts after alpha 9" version_greater "v1.0.0-alpha.12" "v1.0.0-alpha.9"

DEVCRAFT_VERSION="1.0.0-alpha.12"
export DEVCRAFT_VERSION
assert_equal "v1.0.0-alpha.12" "$(resolve_release_tag)" "honors explicit version override"

install_root="$TMP_DIR/install-root"
package_dir="$TMP_DIR/package"
mkdir -p "$install_root" "$package_dir/profile"
cp "/bin/ls" "$install_root/devcraft"
chmod +x "$install_root/devcraft"
cp "/bin/cat" "$package_dir/devcraft"
chmod +x "$package_dir/devcraft"
old_inode="$(ls -i "$install_root/devcraft" | awk '{print $1}')"

DEVCRAFT_HOME="$install_root"
INSTALL_DIR="$install_root"
export DEVCRAFT_HOME
install_from_directory "$package_dir"

new_inode="$(ls -i "$install_root/devcraft" | awk '{print $1}')"
file "$install_root/devcraft" | grep -E 'Mach-O|ELF' >/dev/null 2>&1 || {
    printf 'FAIL: installs platform executable binary\n' >&2
    exit 1
}

if [ "$old_inode" = "$new_inode" ]; then
    printf 'FAIL: install should replace devcraft with a new inode\n' >&2
    exit 1
fi

invalid_package_dir="$TMP_DIR/invalid-package"
mkdir -p "$invalid_package_dir/profile"
printf 'not a binary\n' > "$invalid_package_dir/devcraft"
chmod +x "$invalid_package_dir/devcraft"

if ( install_from_directory "$invalid_package_dir" ) 2>/dev/null; then
    printf 'FAIL: installer should reject non-platform binary payloads\n' >&2
    exit 1
fi

rm -rf "$TMP_DIR"
printf 'installer-release-selection.sh passed\n'

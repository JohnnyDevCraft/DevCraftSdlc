#!/usr/bin/env sh
set -eu

REPOSITORY="${DEVCRAFT_REPOSITORY:-JohnnyDevCraft/DevCraftSdlc}"
INSTALL_DIR="${DEVCRAFT_HOME:-$HOME/.DevCraft}"
TMP_DIR="${TMPDIR:-/tmp}/devcraft-install-$$"

info() {
    printf '%s\n' "$1"
}

fail() {
    printf 'DevCraft install failed: %s\n' "$1" >&2
    exit 1
}

has_command() {
    command -v "$1" >/dev/null 2>&1
}

detect_rid() {
    os="$(uname -s)"
    arch="$(uname -m)"

    case "$os" in
        Darwin) platform="osx" ;;
        Linux) platform="linux" ;;
        *) fail "unsupported operating system: $os" ;;
    esac

    case "$arch" in
        arm64|aarch64) cpu="arm64" ;;
        x86_64|amd64) cpu="x64" ;;
        *) fail "unsupported CPU architecture: $arch" ;;
    esac

    printf '%s-%s' "$platform" "$cpu"
}

download() {
    url="$1"
    output="$2"

    if has_command curl; then
        curl -fsSL "$url" -o "$output"
        return
    fi

    if has_command wget; then
        wget -q "$url" -O "$output"
        return
    fi

    fail "curl or wget is required"
}

normalize_version_tag() {
    version="$1"

    case "$version" in
        v*) printf '%s' "$version" ;;
        *) printf 'v%s' "$version" ;;
    esac
}

version_core() {
    tag="$(normalize_version_tag "$1")"
    tag="${tag#v}"
    printf '%s' "${tag%%-*}"
}

version_prerelease() {
    tag="$(normalize_version_tag "$1")"
    tag="${tag#v}"

    case "$tag" in
        *-*) printf '%s' "${tag#*-}" ;;
        *) printf '' ;;
    esac
}

prerelease_rank() {
    prerelease="$1"
    label="${prerelease%%.*}"

    case "$label" in
        '') printf '9' ;;
        alpha) printf '1' ;;
        beta) printf '2' ;;
        rc) printf '3' ;;
        *) printf '0' ;;
    esac
}

prerelease_number() {
    prerelease="$1"

    case "$prerelease" in
        *.*) number="${prerelease#*.}" ;;
        *) number="0" ;;
    esac

    case "$number" in
        ''|*[!0-9]*) printf '0' ;;
        *) printf '%s' "$number" ;;
    esac
}

version_greater() {
    left="$(normalize_version_tag "$1")"
    right="$(normalize_version_tag "$2")"
    left_core="$(version_core "$left")"
    right_core="$(version_core "$right")"

    left_major="${left_core%%.*}"
    left_rest="${left_core#*.}"
    left_minor="${left_rest%%.*}"
    left_patch="${left_rest#*.}"

    right_major="${right_core%%.*}"
    right_rest="${right_core#*.}"
    right_minor="${right_rest%%.*}"
    right_patch="${right_rest#*.}"

    for part in major minor patch; do
        eval "left_value=\$left_$part"
        eval "right_value=\$right_$part"

        if [ "$left_value" -gt "$right_value" ]; then
            return 0
        fi

        if [ "$left_value" -lt "$right_value" ]; then
            return 1
        fi
    done

    left_pre="$(version_prerelease "$left")"
    right_pre="$(version_prerelease "$right")"
    left_rank="$(prerelease_rank "$left_pre")"
    right_rank="$(prerelease_rank "$right_pre")"

    if [ "$left_rank" -gt "$right_rank" ]; then
        return 0
    fi

    if [ "$left_rank" -lt "$right_rank" ]; then
        return 1
    fi

    left_number="$(prerelease_number "$left_pre")"
    right_number="$(prerelease_number "$right_pre")"

    [ "$left_number" -gt "$right_number" ]
}

select_release_tag_from_metadata() {
    metadata="$1"
    selected=""

    sed -n 's/.*"tag_name": "\(v[0-9][^"]*\)".*/\1/p' "$metadata" | while IFS= read -r tag; do
        case "$tag" in
            v[0-9]*.[0-9]*.[0-9]*)
                if [ -z "$selected" ] || version_greater "$tag" "$selected"; then
                    selected="$tag"
                fi
                printf '%s\n' "$selected" > "$TMP_DIR/selected-release-tag"
                ;;
        esac
    done

    [ -f "$TMP_DIR/selected-release-tag" ] || return 1
    cat "$TMP_DIR/selected-release-tag"
}

resolve_release_tag() {
    if [ -n "${DEVCRAFT_VERSION:-}" ]; then
        normalize_version_tag "$DEVCRAFT_VERSION"
        return
    fi

    api_url="https://api.github.com/repos/$REPOSITORY/releases?per_page=100"
    metadata="$TMP_DIR/releases.json"
    download "$api_url" "$metadata"
    select_release_tag_from_metadata "$metadata"
}

asset_url_for_release() {
    rid="$1"
    release_tag="$2"

    api_url="https://api.github.com/repos/$REPOSITORY/releases/tags/$release_tag"
    metadata="$TMP_DIR/release-$release_tag.json"
    download "$api_url" "$metadata"

    asset="DevCraft-$rid.tar.gz"
    sed -n 's/.*"browser_download_url": "\(.*'"$asset"'\)".*/\1/p' "$metadata" | head -n 1
}

copy_profile() {
    source_profile="$1"
    mkdir -p "$INSTALL_DIR"

    if [ -d "$source_profile" ]; then
        (
            cd "$source_profile"
            find . -type d | while IFS= read -r directory; do
                mkdir -p "$INSTALL_DIR/$directory"
            done
            find . -type f ! -name 'soul.md' | while IFS= read -r file; do
                target="$INSTALL_DIR/$file"
                if [ "$file" = "./configure.json" ] && [ -f "$target" ]; then
                    continue
                fi

                mkdir -p "$(dirname "$target")"
                cp "$file" "$target"
            done
        )
    fi

    mkdir -p \
        "$INSTALL_DIR/architectures" \
        "$INSTALL_DIR/feature-storage" \
        "$INSTALL_DIR/features" \
        "$INSTALL_DIR/project-types" \
        "$INSTALL_DIR/skills" \
        "$INSTALL_DIR/standards" \
        "$INSTALL_DIR/templates"
}

sign_macos_binary_if_needed() {
    binary="$1"

    [ "$(uname -s)" = "Darwin" ] || return 0
    has_command codesign || return 0

    codesign --force --sign - "$binary" >/dev/null 2>&1 || fail "failed to ad-hoc sign installed macOS binary"
    codesign --verify --deep --strict --verbose=4 "$binary" >/dev/null 2>&1 || fail "installed macOS binary signature verification failed"
}

validate_binary_for_platform() {
    binary="$1"

    has_command file || return 0

    case "$(uname -s)" in
        Darwin)
            file "$binary" | grep 'Mach-O' >/dev/null 2>&1 || fail "package binary is not a macOS Mach-O executable"
            ;;
        Linux)
            file "$binary" | grep 'ELF' >/dev/null 2>&1 || fail "package binary is not a Linux ELF executable"
            ;;
    esac
}

install_binary() {
    source_binary="$1"
    target_binary="$2"
    target_directory="$(dirname "$target_binary")"
    temp_binary="$target_directory/.devcraft-install-$(basename "$target_binary").$$"

    rm -f "$temp_binary"
    cp "$source_binary" "$temp_binary"
    chmod +x "$temp_binary"
    validate_binary_for_platform "$temp_binary"
    sign_macos_binary_if_needed "$temp_binary"
    mv -f "$temp_binary" "$target_binary"
}

install_from_directory() {
    package_dir="$1"

    binary="$package_dir/devcraft"
    [ -f "$binary" ] || fail "package binary not found"

    copy_profile "$package_dir/profile"
    install_binary "$binary" "$INSTALL_DIR/devcraft"
}

install_from_release() {
    rid="$(detect_rid)"
    mkdir -p "$TMP_DIR"
    release_tag="$(resolve_release_tag)"
    asset_url="$(asset_url_for_release "$rid" "$release_tag")"

    [ -n "$asset_url" ] || fail "no release asset found for $rid in $release_tag"

    archive="$TMP_DIR/DevCraft-$rid.tar.gz"
    package_dir="$TMP_DIR/package"
    mkdir -p "$package_dir"

    info "Downloading DevCraft $release_tag for $rid..."
    download "$asset_url" "$archive"
    tar -xzf "$archive" -C "$package_dir"
    install_from_directory "$package_dir"
}

profile_file() {
    shell_name="$(basename "${SHELL:-}")"

    if [ "$shell_name" = "zsh" ]; then
        printf '%s/.zshrc' "$HOME"
        return
    fi

    if [ "$shell_name" = "bash" ]; then
        if [ -f "$HOME/.bash_profile" ]; then
            printf '%s/.bash_profile' "$HOME"
        else
            printf '%s/.bashrc' "$HOME"
        fi
        return
    fi

    if [ -n "${ZSH_VERSION:-}" ]; then
        printf '%s/.zshrc' "$HOME"
        return
    fi

    if [ -n "${BASH_VERSION:-}" ]; then
        if [ -f "$HOME/.bash_profile" ]; then
            printf '%s/.bash_profile' "$HOME"
        else
            printf '%s/.bashrc' "$HOME"
        fi
        return
    fi

    if [ -f "$HOME/.zshrc" ]; then
        printf '%s/.zshrc' "$HOME"
        return
    fi

    printf '%s/.profile' "$HOME"
}

ensure_path() {
    shell_profile="$(profile_file)"
    path_line="export PATH=\"\$HOME/.DevCraft:\$PATH\""

    touch "$shell_profile"

    if ! grep -F '$HOME/.DevCraft' "$shell_profile" >/dev/null 2>&1; then
        {
            printf '\n'
            printf '# DevCraft CLI\n'
            printf '%s\n' "$path_line"
        } >> "$shell_profile"
    fi

    info "DevCraft installed in $INSTALL_DIR"
    info "Run this to refresh your shell:"
    info ". \"$shell_profile\""
}

main() {
    mkdir -p "$TMP_DIR"
    script_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

    if [ -f "$script_dir/devcraft" ] && [ -d "$script_dir/profile" ]; then
        install_from_directory "$script_dir"
    else
        install_from_release
    fi

    ensure_path
}

if [ "${DEVCRAFT_INSTALLER_TEST_MODE:-0}" != "1" ]; then
    main "$@"
fi

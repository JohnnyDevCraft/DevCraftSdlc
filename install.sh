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

latest_asset_url() {
    rid="$1"

    api_url="https://api.github.com/repos/$REPOSITORY/releases/latest"
    metadata="$TMP_DIR/latest-release.json"
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

install_from_directory() {
    package_dir="$1"

    binary="$package_dir/devcraft"
    [ -f "$binary" ] || fail "package binary not found"

    copy_profile "$package_dir/profile"
    cp "$binary" "$INSTALL_DIR/devcraft"
    chmod +x "$INSTALL_DIR/devcraft"
}

install_from_release() {
    rid="$(detect_rid)"
    mkdir -p "$TMP_DIR"
    asset_url="$(latest_asset_url "$rid")"

    [ -n "$asset_url" ] || fail "no release asset found for $rid"

    archive="$TMP_DIR/DevCraft-$rid.tar.gz"
    package_dir="$TMP_DIR/package"
    mkdir -p "$package_dir"

    info "Downloading DevCraft for $rid..."
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

main "$@"

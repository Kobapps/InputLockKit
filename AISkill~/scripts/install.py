#!/usr/bin/env python3
"""
Install com.kobapps.inputlockkit into a Unity project by editing its Packages/manifest.json.

Modes:
  --local <SRC>   Copy the package folder (SRC = path to com.kobapps.inputlockkit) into the
                  project's Packages/ as an embedded package. Embedded packages take precedence,
                  so any manifest dependency entry is removed.
  --git [URL]     Add a Git URL dependency (default: the Kobapps GitHub repo, path-scoped).
  --version STR   Add an explicit version string (registry version or git ref) as the dependency.

If none is given, defaults to --git.

Usage:
  python install.py <UnityProjectRoot> --local ../../Packages/com.kobapps.inputlockkit
  python install.py <UnityProjectRoot> --git
  python install.py <UnityProjectRoot> --version 1.1.0
"""
import argparse
import json
import os
import shutil
import sys

PACKAGE = "com.kobapps.inputlockkit"
DEFAULT_GIT = "https://github.com/Kobapps/InputLockKit.git"


def main() -> int:
    parser = argparse.ArgumentParser(description="Install InputLockKit into a Unity project.")
    parser.add_argument("project", help="Unity project root (the folder containing Packages/ and Assets/).")
    parser.add_argument("--local", metavar="SRC",
                        help="Copy the package from SRC into the project's Packages/ (embedded).")
    parser.add_argument("--git", metavar="URL", nargs="?", const=DEFAULT_GIT,
                        help="Install via a Git URL (default if no mode is given).")
    parser.add_argument("--version", metavar="STR",
                        help="Install a specific version/ref string as the dependency.")
    args = parser.parse_args()

    project = os.path.abspath(args.project)
    manifest_path = os.path.join(project, "Packages", "manifest.json")
    if not os.path.isfile(manifest_path):
        print(f"error: manifest.json not found at {manifest_path}", file=sys.stderr)
        print("Point at the Unity project root (the folder with Packages/ and Assets/).", file=sys.stderr)
        return 1

    with open(manifest_path, encoding="utf-8") as handle:
        manifest = json.load(handle)
    dependencies = manifest.setdefault("dependencies", {})

    if args.local:
        src = os.path.abspath(args.local)
        if not os.path.isdir(src):
            print(f"error: --local source not found: {src}", file=sys.stderr)
            return 1
        if not os.path.isfile(os.path.join(src, "package.json")):
            print(f"error: {src} is not a package folder (no package.json).", file=sys.stderr)
            return 1
        dst = os.path.join(project, "Packages", PACKAGE)
        if os.path.abspath(dst) != src:
            shutil.rmtree(dst, ignore_errors=True)
            shutil.copytree(src, dst, ignore=shutil.ignore_patterns("*.meta"))
            print(f"Copied package into {dst} (embedded).")
        else:
            print(f"Package already at {dst} (embedded).")
        dependencies.pop(PACKAGE, None)  # embedded takes precedence over a manifest entry
    elif args.version:
        dependencies[PACKAGE] = args.version
        print(f"Set dependency {PACKAGE} = {args.version}")
    else:
        url = args.git or DEFAULT_GIT
        dependencies[PACKAGE] = url
        print(f"Set dependency {PACKAGE} = {url}")

    with open(manifest_path, "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=2)
        handle.write("\n")

    print(f"Updated {manifest_path}.")
    print("Focus the Unity Editor (or reopen the project) so UPM resolves the package,")
    print("then verify: menu 'Tools > Input Lock > Debugger' exists and 'using Kobapps.InputLockKit;' compiles.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

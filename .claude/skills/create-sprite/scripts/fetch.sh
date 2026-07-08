#!/usr/bin/env bash
# Download a PixelLab result image (rotation/animation frame URL) to a local path.
# Usage: fetch.sh <url> <output-path.png>
set -euo pipefail

url="$1"
out="$2"

mkdir -p "$(dirname "$out")"
curl -fsSL "$url" -o "$out"
echo "saved: $out ($(wc -c < "$out") bytes)"

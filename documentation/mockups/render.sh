#!/usr/bin/env sh
# Vyrenderuje *.svg -> *.png (2x) cez headless Chrome / Edge.
cd "$(dirname "$0")"
BROWSER="${BROWSER:-/c/Program Files/Google/Chrome/Application/chrome.exe}"
for f in *.svg; do
  "$BROWSER" --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=2 \
    --window-size=1280,800 --screenshot="$PWD/${f%.svg}.png" "file:///$(cygpath -m "$PWD/$f" 2>/dev/null || echo "$PWD/$f")"
done

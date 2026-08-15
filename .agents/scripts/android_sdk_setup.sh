#!/usr/bin/env bash
set -euo pipefail

LOG=/root/omnichannel-monorepo/.agents/logs/android_sdk_setup.log
exec >"$LOG" 2>&1

export ANDROID_HOME=/opt/android-sdk
export ANDROID_SDK_ROOT=/opt/android-sdk

echo "=== [1/4] Download commandline-tools ==="
mkdir -p /opt/android-sdk/cmdline-tools
cd /tmp
if [ ! -f /tmp/cmdline-tools.zip ]; then
  curl -sSL -o /tmp/cmdline-tools.zip https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip
fi
echo "downloaded: $(ls -la /tmp/cmdline-tools.zip | awk '{print $5}') bytes"

echo "=== [2/4] Unzip commandline-tools ==="
cd /opt/android-sdk/cmdline-tools
unzip -qo /tmp/cmdline-tools.zip
mv cmdline-tools latest 2>/dev/null || true
echo "layout:"
ls -la /opt/android-sdk/cmdline-tools/latest/bin 2>&1 | head

echo "=== [3/4] Accept licenses ==="
yes | /opt/android-sdk/cmdline-tools/latest/bin/sdkmanager --sdk_root=/opt/android-sdk --licenses >/dev/null 2>&1 || true

echo "=== [4/4] Install platform-tools, platform 34, build-tools 34.0.0 ==="
/opt/android-sdk/cmdline-tools/latest/bin/sdkmanager --sdk_root=/opt/android-sdk \
  "platform-tools" "platforms;android-34" "build-tools;34.0.0" 2>&1 | tail -20

echo "=== done. SDK contents: ==="
ls -la /opt/android-sdk
ls -la /opt/android-sdk/platforms 2>&1
ls -la /opt/android-sdk/build-tools 2>&1

echo "=== Download Gradle 8.7 ==="
if [ ! -f /tmp/gradle-8.7-bin.zip ]; then
  curl -sSL -o /tmp/gradle-8.7-bin.zip https://services.gradle.org/distributions/gradle-8.7-bin.zip
fi
cd /opt
unzip -qo /tmp/gradle-8.7-bin.zip
echo "gradle installed:"
/opt/gradle-8.7/bin/gradle --version 2>&1 | head -8

echo "ANDROID_SDK_SETUP_COMPLETE"

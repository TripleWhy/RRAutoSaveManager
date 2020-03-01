#!/usr/bin/env bash

set -xe

PAGESDIR="$PWD/public"

if [ -z "$CI_COMMIT_TAG" ]
then
	VERSION=$(git tag -l --points-at HEAD | head -n 1 | cut -d'v' -f 2)
else
	VERSION=$(echo "$CI_COMMIT_TAG" | cut -d'v' -f 2)
fi
if [ -z "$VERSION" ]
then
	VERSION="0.0.0"
fi

RELEASEDATE=$(git show -s --format=%cd --date=short)
if [ -z "$RELEASEDATE" ]
then
	RELEASEDATE=$(date -Idate)
fi

cp -rL ci/installer .
mkdir public

DOTNETVERSION=$(<dotnetversion.txt)
DOTNETDATE=$(date -Idate -r RRAutoSaveManager_sc/System.dll)

QTFULLNAME=$(<qtversion.txt)
QTVERSION=$(echo $QTFULLNAME | cut -d '-' -f 2)

python3 -m aqt install -O ./QtRuntime $QTVERSION windows desktop win64_msvc2017_64
QTDATE=$(date -Idate -r QtRuntime/$QTVERSION/msvc2017_64/bin/Qt5Core.dll)

cp -ar RRAutoSaveManager_sc public/RRAutoSaveManager
QTSRC="QtRuntime/$QTVERSION/msvc2017_64"
QTDSTBASE="public/RRAutoSaveManager/$QTFULLNAME"
QTDST="$QTDSTBASE/qt"
wine "$QTSRC/bin/windeployqt.exe" --dir "$QTDST/qml" --libdir "$QTDST/bin" --plugindir "$QTDST/plugins" --release --qmldir public/RRAutoSaveManager/qml/ public/RRAutoSaveManager/QmlNet.dll
rm -rf "$QTDST/qml/translations"
echo "$QTFULLNAME" > "$QTDSTBASE/version.txt"
cp -ar "$QTDSTBASE" installer/packages/com.gitlab.triplewhy.rrautosavemanager.qt/data/.

pushd public
7z a -t7z -mx=9 -sdel -bd RRAutoSaveManager-w64-full.7z RRAutoSaveManager
popd

cp -ar RRAutoSaveManager_fde public/RRAutoSaveManager
pushd public
7z a -t7z -mx=9 -sdel -bd RRAutoSaveManager-w64-minimal.7z RRAutoSaveManager
popd

mv RRAutoSaveManager_fde/RRAutoSaveManager.*.json installer/packages/com.gitlab.triplewhy.rrautosavemanager.config.fde/data
mv RRAutoSaveManager_sc/RRAutoSaveManager.*.json  installer/packages/com.gitlab.triplewhy.rrautosavemanager.config.sc/data

pushd RRAutoSaveManager_fde
find . -type f > ../fde_files
find . -type d > ../fde_dirs
popd
pushd RRAutoSaveManager_sc
xargs rm < ../fde_files
xargs rmdir < ../fde_dirs || true
rm ../fde_files
rm ../fde_dirs
popd

mv RRAutoSaveManager_fde/* installer/packages/com.gitlab.triplewhy.rrautosavemanager.core/data
mv RRAutoSaveManager_sc/*  installer/packages/com.gitlab.triplewhy.rrautosavemanager.dotnet/data
wget -cnv -P installer/packages/com.microsoft.vcredist.x64/data 'https://aka.ms/vs/16/release/vc_redist.x64.exe'
VCDATE=$(date -Idate -r installer/packages/com.microsoft.vcredist.x64/data/vc_redist.x64.exe)
VCVERSION=$(exiftool -ProductVersion -b installer/packages/com.microsoft.vcredist.x64/data/vc_redist.x64.exe)

wget -cnv 'http://download.qt.io/online/qtsdkrepository/windows_x86/desktop/tools_ifw/qt.tools.ifw.32/3.2.0ifw-win-x86.7z'
wget -cnv 'http://download.qt.io/online/qtsdkrepository/linux_x64/desktop/tools_ifw/qt.tools.ifw.32/3.2.0ifw-linux-x64.7z'
7z x -aos 3.2.0ifw-linux-x64.7z
7z x -aos 3.2.0ifw-win-x86.7z Tools/QtInstallerFramework/3.2/bin/installerbase.exe
IFW="$PWD/Tools/QtInstallerFramework/3.2/bin"

wget -cnv 'http://download.qt.io/online/qtsdkrepository/windows_x86/desktop/licenses/qt.license.lgpl/1.0.2meta.7z'
7z x -aos 1.0.2meta.7z

mv 'qt.license.lgpl/LICENSE' 'installer/packages/com.gitlab.triplewhy.rrautosavemanager.core/meta/license-qt'
wget -cnv -O 'installer/packages/com.gitlab.triplewhy.rrautosavemanager.core/meta/license-qmlnet' 'https://raw.githubusercontent.com/qmlnet/qmlnet/v0.10.1/LICENSE'
wget -cnv -O 'installer/packages/com.gitlab.triplewhy.rrautosavemanager.core/meta/license-protobuf' 'https://raw.githubusercontent.com/protocolbuffers/protobuf/v3.11.4/LICENSE'
wget -cnv -O 'installer/packages/com.gitlab.triplewhy.rrautosavemanager.dotnet/meta/license-dotnet' 'https://raw.githubusercontent.com/dotnet/core/v3.1.1/LICENSE.TXT'

pushd installer
find . -type f -name .gitkeep -delete
find . -type f -name '*.in' -print0 | while read -d $'\0' file
do
	newfile="${file%.in}"
	mv "$file" "$newfile"
	sed -i -e "s/@VERSION@/$VERSION/g" "$newfile"
	sed -i -e "s/@RELEASEDATE@/$RELEASEDATE/g" "$newfile"
	sed -i -e "s/@QTVERSION@/$QTVERSION/g" "$newfile"
	sed -i -e "s/@QTDATE@/$QTDATE/g" "$newfile"
	sed -i -e "s/@DOTNETVERSION@/$DOTNETVERSION/g" "$newfile"
	sed -i -e "s/@DOTNETDATE@/$DOTNETDATE/g" "$newfile"
	sed -i -e "s/@VCVERSION@/$VCVERSION/g" "$newfile"
	sed -i -e "s/@VCDATE@/$VCDATE/g" "$newfile"
done

for f in packages/*/data; do
	name=$(dirname "$f")
	name=$(basename "$name")
	name=${name##*.}
	pushd "$f"
	7z a -t7z -mx=9 -sdel -bd "$name.7z" .
	popd
done

"$IFW"/repogen -p packages "$PAGESDIR"/packages
"$IFW/"binarycreator --online-only  -t "$IFW/installerbase.exe" -c config/config.xml -p packages "$PAGESDIR/RRAutosaveManagerSetup.exe"
"$IFW/"binarycreator --offline-only -t "$IFW/installerbase.exe" -c config/config.xml -p packages "$PAGESDIR/RRAutosaveManagerSetupOffline.exe"

popd

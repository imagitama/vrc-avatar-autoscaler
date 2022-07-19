# place the unity editor dir in your PATH

& 'Unity.exe' `
    -batchmode `
    -nographics `
    -quit `
    -projectPath ../../../ `
    -exportPackage "Assets/PeanutTools" `
    "Assets/PeanutTools/VRC_Avatar_AutoScaler/peanuttools_vrcavatarautoscaler_VERSION.unitypackage"
Copy-Item Patch/FontInfo.json Fonts/FontInfo.json -Force
& "Bin/AITSFChsPatchCreate.exe"
python Bin\convert_rgba32_bc7_dxt1.py
Copy-Item Fonts/FontInfo.json Patch/FontInfo.json -Force

Compress-Archive -Path "Patch\resources.assets", "Patch\BuildPlayer-*", "Patch\CAB-*" -Destination "patch.zip" -Force
Move-Item -Path "patch.zip" -Destination "patch.xzp" -Force

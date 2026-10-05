from pathlib import Path
import hashlib
import zipfile

root = Path(__file__).resolve().parents[1]
dist = root/'dist'; dist.mkdir(exist_ok=True)
archive = dist/'EyeglassLensSystem-1.0.0.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as output:
    for path in sorted((root/'Assets/PrescriptionLens').rglob('*')):
        if path.is_file(): output.write(path,path.relative_to(root).as_posix())
    for name in ['Assets/PrescriptionLens.meta','README.md','LICENSE','RELEASE_NOTES.md']:
        output.write(root/name,name)
files = sorted(dist.glob('*.unitypackage')) + [archive]
assert len(files)==2, 'Export the unitypackage in Unity before building the ZIP'
(dist/'SHA256SUMS.txt').write_text(''.join(f'{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.name}\n' for p in files),encoding='utf-8')
for path in files: print(f'{path.name}: {path.stat().st_size:,} bytes')

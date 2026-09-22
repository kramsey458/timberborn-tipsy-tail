"""Check that every copy of the mod version agrees with Source/Runtime/TipsyTail.Runtime.csproj.
Usage: python Source/validate_version.py
The csproj <Version> is the only place the version is chosen. build_mod.py derives the manifest
version from it and the runtime DLL is compiled with it. The README, the website fallback, the
DEVELOPMENT.md title and RELEASE_NOTES.md are written by hand for each release, so every copy is
listed and the check fails if any disagrees or can no longer be found. Game-free: it reads only
files in this repository, so CI runs it.
"""
import json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
CSPROJ=ROOT/'Source/Runtime/TipsyTail.Runtime.csproj'
V=r'(\d+\.\d+\.\d+)'

def mod_version():
    found=re.findall(r'<Version>([^<]*)</Version>',CSPROJ.read_text(encoding='utf-8'))
    if len(found)!=1 or not re.fullmatch(V,found[0]):
        raise SystemExit(f'{CSPROJ.relative_to(ROOT).as_posix()}: expected one <Version>MAJOR.MINOR.PATCH</Version>, found {found}')
    return found[0]

def manifest_version():
    # Timberborn manifests carry four parts.
    return mod_version()+'.0'

def dll_versions(path):
    # FileVersion and InformationalVersion are stored as custom attribute blobs: prolog 01 00,
    # a one-byte length, the UTF-8 text, then 00 00 for "no named arguments".
    return [m[2].decode() for m in re.finditer(rb'\x01\x00([\x01-\x7f])(\d+(?:\.\d+){2,3}(?:\+[0-9a-f]+)?)\x00\x00',path.read_bytes()) if m[1][0]==len(m[2])]

def copies(version):
    """(where, found, expected) for every copy. found is None where the expected text is missing."""
    text=lambda p:(ROOT/p).read_text(encoding='utf-8')
    rows=[]
    def every(path,label,pattern):
        found=re.findall(pattern,text(path),re.M)
        rows.extend((f'{path} {label}',f,version) for f in found or [None])
    def first(path,label,pattern):
        m=re.search(pattern,text(path),re.M)
        rows.append((f'{path} {label}',m and m[1],version))
    literal=re.search(r"""['"]Version['"]\s*:\s*['"]([^'"]*)['"]""",text('Source/build_mod.py'))
    rows.append(('Source/build_mod.py manifest Version',f"'{literal[1]}' (hard-coded)" if literal else
                 'manifest_version()' if 'manifest_version()' in text('Source/build_mod.py') else None,'manifest_version()'))
    rows.append(('Mod/manifest.json Version',json.loads(text('Mod/manifest.json')).get('Version'),manifest_version()))
    dll=dll_versions(ROOT/'Mod/Scripts/TipsyTail.Runtime.dll')
    rows.extend(('Mod/Scripts/TipsyTail.Runtime.dll '+('FileVersion' if s.count('.')==3 else 'InformationalVersion'),s.split('+')[0],
                 manifest_version() if s.count('.')==3 else version) for s in dll)
    if not any(s.count('.')==2 for s in dll): rows.append(('Mod/Scripts/TipsyTail.Runtime.dll InformationalVersion',None,version))
    every('docs/index.html','data-release-pinned',r'data-release-pinned="v?'+V+'"')
    every('docs/index.html','data-release="tag" fallback',r'data-release="tag">v'+V+'<')
    every('README.md','release link',r'/releases/(?:download|tag)/v'+V+r'\b')
    every('README.md','asset name',r'TipsyTail-v'+V+'-')
    first('README.md',"first What's new heading",r"^## What's new in v"+V)
    first('README.md','release status',r'^\*\*v'+V+' is ')
    first('DEVELOPMENT.md','title',r'\A# The Tipsy Tail\b.*?'+V+r'\s*$')
    first('DEVELOPMENT.md',"first What's new heading",r"^## What's new in v?"+V)
    first('RELEASE_NOTES.md','title',r'\A# The Tipsy Tail v'+V)
    every('RELEASE_NOTES.md','asset name',r'TipsyTail-v'+V+'-')
    return rows

def validate_version():
    version=mod_version()
    rows=copies(version)
    bad=[r for r in rows if r[1]!=r[2]]
    print(f'{"ok":10}{CSPROJ.relative_to(ROOT).as_posix()} <Version>: {version} (the source)')
    for where,found,expected in rows:
        print(f'{"ok":10}{where}: {found}' if found==expected else f'{"MISMATCH":10}{where}: {found or "missing"}, expected {expected}')
    print(f'{len(rows)-len(bad)} of {len(rows)} copies agree with {version}.')
    return not bad

if __name__=='__main__':
    sys.exit(0 if validate_version() else 1)

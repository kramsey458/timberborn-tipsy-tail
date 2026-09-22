"""Check that the committed Mod/Scripts/TipsyTail.Runtime.dll is exactly what the source compiles to.
Usage: python Source/validate_runtime_dll.py
Needs the .NET 8 SDK and the installed game's Managed assemblies (found through TIMBERBORN_PATH, like
the other validators), so CI does not run it. The runtime is built twice: in place, and from a copy of
Source/Runtime outside any git checkout. The two builds must be byte-identical, which shows the DLL
depends only on the source files, the referenced game assemblies and the compiler, not on the git
commit or the folder. The in-place build must then be byte-identical to the committed DLL.
Nothing in the repository is changed apart from the usual Source/Runtime/bin and obj output.
"""
import os,re,shutil,subprocess,sys,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
GAME=Path(os.environ.get('TIMBERBORN_PATH',r'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'))
MANAGED=GAME/'Timberborn_Data/Managed'
RUNTIME=ROOT/'Source/Runtime'
DLL='TipsyTail.Runtime.dll'
COMMITTED=ROOT/'Mod/Scripts'/DLL

def build(project_dir,*props,out=None):
    cmd=['dotnet','build',str(project_dir/'TipsyTail.Runtime.csproj'),'-c','Release','-nologo','-v','q',f'-p:TimberbornManagedDir={MANAGED}',*props]
    if out: cmd+=['-o',str(out)]
    r=subprocess.run(cmd,capture_output=True,text=True)
    if r.returncode: raise SystemExit(f'Build failed in {project_dir}:\n{r.stdout}{r.stderr}')
    return (Path(out) if out else project_dir/'bin/Release/netstandard2.1')/DLL

def informational_version(b):
    # A custom attribute blob: prolog 01 00, a one-byte length, the UTF-8 text, then 00 00.
    found=[m[2].decode() for m in re.finditer(rb'\x01\x00([\x01-\x7f])(\d+\.\d+\.\d+(?:\+[0-9a-f]+)?)\x00\x00',b) if m[1][0]==len(m[2])]
    return found[0] if found else None

def describe(name,a,b):
    first=next((i for i,(x,y) in enumerate(zip(a,b)) if x!=y),min(len(a),len(b)))
    return f'{name}: {len(a)} vs {len(b)} bytes, first difference at offset {first}; InformationalVersion {informational_version(a)!r} vs {informational_version(b)!r}'

def validate_runtime_dll():
    if not MANAGED.is_dir(): raise SystemExit(f'Needs the game assemblies in {MANAGED}; set TIMBERBORN_PATH.')
    sdk=subprocess.run(['dotnet','--version'],capture_output=True,text=True).stdout.strip()
    committed=COMMITTED.read_bytes()
    with tempfile.TemporaryDirectory(prefix='tipsytail-') as tmp:
        copy=Path(tmp)/'Runtime'; copy.mkdir()
        for f in RUNTIME.iterdir():
            if f.is_file(): shutil.copy2(f,copy/f.name)
        here=build(RUNTIME).read_bytes()
        elsewhere=build(copy).read_bytes()
        print(f'.NET SDK {sdk}; game assemblies from {MANAGED}')
        if here!=elsewhere:
            print('FAIL '+describe('in-place build vs a copy outside git',here,elsewhere))
            print('     The DLL depends on the git commit or the checkout folder, so no commit can hold the DLL it builds.')
            return False
        print(f'ok   the in-place build and a copy outside git are byte-identical ({len(here)} bytes)')
        if here==committed:
            print(f'ok   Mod/Scripts/{DLL} is byte-identical to the build')
            return True
        stamp=re.fullmatch(r'\d+\.\d+\.\d+\+([0-9a-f]{40})',informational_version(committed) or '')
        if stamp:
            # Built before the csproj stopped stamping the git commit into InformationalVersion: rebuild with
            # the same stamp to show the rest of the file is the current source. Rebuilding the DLL drops it.
            legacy=build(copy,'-p:IncludeSourceRevisionInInformationalVersion=true',f'-p:SourceRevisionId={stamp[1]}',out=Path(tmp)/'stamped').read_bytes()
            if legacy==committed:
                print(f'ok   Mod/Scripts/{DLL} is byte-identical to the build once its old commit stamp +{stamp[1][:7]} is restored;')
                print('     it predates reproducible builds, and the stamp goes away when the DLL is next rebuilt and copied in')
                return True
            here=legacy
        print('FAIL '+describe(f'build vs Mod/Scripts/{DLL}',here,committed))
        print(f'     Copy Source/Runtime/bin/Release/netstandard2.1/{DLL} into Mod/Scripts, or build with the .NET SDK and game version that made it.')
        return False

if __name__=='__main__':
    sys.exit(0 if validate_runtime_dll() else 1)

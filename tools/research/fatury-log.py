# Fatury 틀(창작마당 2814703549)이 자산을 싣는 모드 목록과 적재 결과를 로그에서 읽는다. 읽기만 한다(05 문서 52절).
#   py -3 -I -X utf8 tools/research/fatury-log.py [MelonLoader\Latest.log 경로] [Player.log 경로]
# 기본은 게임의 Latest.log(CP949)와 %USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\Player.log(UTF-8).
# 적는 것: (1) Fatury가 훑은 모드 폴더(…\ModAssets\Fatury 줄)와 게임이 읽은 모드(Player.log의 加载模组 줄)의 수·차이,
#         (2) Fatury의 ERROR·WARNING 종류와 수, (3) 입화 층(Body·Hair…)마다 어느 모드가 몇 개를 실었는지(Load: 줄), 두 모드가 같은 번호를 실은 것.
import os, re, sys, collections

latest = sys.argv[1] if len(sys.argv) > 1 else r'E:\SteamLibrary\steamapps\common\鬼谷八荒\MelonLoader\Latest.log'
player = sys.argv[2] if len(sys.argv) > 2 else os.path.expandvars(r'%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\Player.log')
BS = chr(92)


def read(path, encoding):
    with open(path, 'rb') as f:
        return f.read().decode(encoding, 'replace').splitlines()


SEP = '[' + BS + BS + '/]'   # 경로 구분자(역슬래시 또는 슬래시). 정규식의 역슬래시는 두 개로 적어야 한다
lines = read(latest, 'cp949') if not latest.endswith('-utf8.log') else read(latest, 'utf-8')
fatury = re.findall(r'ModExportData' + SEP + r'([0-9A-Za-z_]+)' + SEP + r'ModAssets' + SEP + r'Fatury\s*$', '\n'.join(lines), re.M)
print('== 로그', latest, '줄', len(lines), '시작', (lines[0][:14] if lines else ''))
print('Fatury가 훑은 모드 폴더(%d): %s' % (len(fatury), ' '.join(fatury)))
if os.path.exists(player):
    text = '\n'.join(read(player, 'utf-8'))
    loaded = re.findall(r'ModExportData' + SEP + r'([0-9A-Za-z_]+)' + SEP + r'ModExportData' + BS + r'.cache', text)
    print('게임이 읽은 모드(Player.log 加载模组 %d): %s' % (len(loaded), ' '.join(loaded)))
    missing = [m for m in loaded if m not in fatury]
    print('Fatury가 건너뛴 것(%d): %s' % (len(missing), ' '.join(missing) if missing else '없음'))
else:
    print('Player.log 없음:', player)

kinds = collections.Counter()
for ln in lines:
    m = re.search(r'\[Fatury\] \[(ERROR|WARNING)\] ([A-Za-z ,]+?)(?:[:=,]|$)', ln)
    if m:
        kinds[m.group(1) + ' ' + m.group(2).strip()] += 1
print('== Fatury ERROR·WARNING')
for k, v in sorted(kinds.items()):
    print(' ', v, k)
if not kinds:
    print('  없음')
for i, ln in enumerate(lines):
    if 'Could not create sprite' in ln:
        prev = next((lines[j] for j in range(i - 1, max(i - 6, -1), -1) if 'Prefetch:' in lines[j]), '')
        print('  sprite 오류 직전 처리 중이던 자산:', prev[prev.find('Prefetch:'):] if prev else '(모름)')

pat = re.compile(r'Load:.*[\\/](\d+)[\\/]ModAssets.Fatury.(Game/Portrait(?:Dynamic)?/(?:Woman|Man|Hat)/?[A-Za-z]*/[0-9A-Za-z-]+)$')
loads = collections.defaultdict(list)
for ln in lines:
    m = pat.search(ln)
    if m:
        loads[m.group(2)].append(m.group(1))
bycat = collections.defaultdict(collections.Counter)
dups = collections.defaultdict(list)
for path, mods in loads.items():
    cat, name = path.rsplit('/', 1)
    for md in mods:
        bycat[cat][md] += 1
    if len(set(mods)) > 1:
        dups[cat].append((name, sorted(set(mods))))
print('== 입화 층마다 실은 모드와 수(Load: 줄)')
for cat in sorted(bycat):
    print(' ', cat, dict(bycat[cat]))
if not bycat:
    print('  없음')
print('== 두 모드 이상이 같은 번호를 실은 것')
for cat in sorted(dups):
    items = sorted(dups[cat], key=lambda t: (len(t[0]), t[0]))
    print(' ', cat, len(items), '개:', ' '.join(f"{n}({'+'.join(m)})" for n, m in items)[:1200])
if not dups:
    print('  없음')

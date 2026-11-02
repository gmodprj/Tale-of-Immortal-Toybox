# 설치된 창작마당 모드의 Fatury 자산(ModAssets/Fatury)을 훑어, sprite asset.yml이 가리키는 그림 파일이 있는지·PNG가 맞는지·
# rect 크기가 그림보다 큰지 검사한다. 읽기만 한다(05 문서 52절).
#   py -3 -I -X utf8 tools/research/fatury-scan.py [ModExportData 경로]
# 깨진 그림(PNG 아님)이나 그림보다 큰 rect는 Fatury의 Sprite.Create에서 예외가 나고, 같은 틀(template)을 쓰는 뒤의 자산까지 깨뜨릴 수 있다.
import os, re, struct, sys, collections
root = sys.argv[1] if len(sys.argv) > 1 else r'E:\SteamLibrary\steamapps\common\鬼谷八荒\ModExportData'
mods = [d for d in os.listdir(root) if d.isdigit()]
tot = collections.Counter()
problems = collections.defaultdict(list)
counts = collections.Counter()
for m in sorted(mods):
    fat = os.path.join(root, m, 'ModAssets', 'Fatury')
    if not os.path.isdir(fat):
        continue
    for dp, dn, fn in os.walk(fat):
        if 'asset.yml' not in fn:
            continue
        rel = os.path.relpath(dp, fat).replace('\\', '/')
        txt = open(os.path.join(dp, 'asset.yml'), encoding='utf-8', errors='replace').read()
        files = re.findall(r'^\s*file:\s*(\S+)', txt, re.M)
        if not files:
            tmpl = re.search(r'^\s*template:\s*(\S+)', txt, re.M)
            counts[m, 'node'] += 1
            continue
        counts[m, 'sprite'] += 1
        sizes = re.findall(r'size:\s*\n\s*x:\s*(\d+)\s*\n\s*y:\s*(\d+)', txt)
        for f in files:
            # name_${index}_${delay} 꼴은 연속 프레임
            if '${' in f:
                pat = re.escape(f).replace(r'\$\{index\}', r'\d+').replace(r'\$\{delay\}', r'\d+')
                hits = [x for x in fn if re.fullmatch(pat, x)]
                if not hits:
                    problems[m].append((rel, f, '프레임 파일 없음'))
                continue
            p = os.path.join(dp, f)
            if not os.path.exists(p):
                problems[m].append((rel, f, '파일 없음'))
                continue
            b = open(p, 'rb').read(32)
            if b[:8] != b'\x89PNG\r\n\x1a\n':
                problems[m].append((rel, f, 'PNG 아님 ' + b[:4].hex()))
                continue
            w, h = struct.unpack('>II', b[16:24])
            for sx, sy in sizes:
                if int(sx) > w or int(sy) > h:
                    problems[m].append((rel, f, f'rect {sx}x{sy} > 그림 {w}x{h}'))
                    break
print('== 모드별 Fatury 자산 수(sprite / 노드)')
for m in sorted(mods):
    s, n = counts[m, 'sprite'], counts[m, 'node']
    if s or n:
        print(f'  {m}: sprite {s}, node {n}, 문제 {len(problems[m])}')
print('== 문제 목록')
for m in sorted(problems):
    for rel, f, why in problems[m]:
        print(f'  {m} {rel} {f}: {why}')
if not problems:
    print('  (없음)')

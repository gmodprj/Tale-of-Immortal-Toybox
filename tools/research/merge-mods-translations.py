# 번역 묶음을 Translations/mods.txt에 합친다(05 문서 43절의 절차를 스크립트로).
# 입력 폴더(tr)에 uniq.tsv(번호<TAB>대표키<TAB>중국어), keys.tsv(번호<TAB>키,키,…), skipped.tsv(키<TAB>중국어 없는 글),
# t-*.tsv·fix-*.tsv(번호<TAB>한국어, fix가 t를 덮어쓴다)가 있어야 한다.
#   py -3 -I -X utf8 tools/research/merge-mods-translations.py <tr 폴더> [--identity] [--apply] [--head "머리 주석"]
# 검사: 자리 표시($…$ &…& {…} <…> [P0] \n |)가 원문과 같은지, 빠진 번호, 겹친 번호. 결과는 <tr>/merged.tsv·report.txt.
# --identity: skipped.tsv의 행을 원문 그대로 넣는다(0lkK4v 제외). --apply: mods.txt 끝에 덧붙인다.
import io, os, re, sys, glob, collections

args = [a for a in sys.argv[1:] if not a.startswith('--')]
if not args:
    print(__doc__ or 'usage: merge-mods-translations.py <tr dir> [--identity] [--apply] [--head text]'); sys.exit(2)
D = args[0]
MODS = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), 'src', 'ModCode', 'ModMain', 'Translations', 'mods.txt')
APPLY = '--apply' in sys.argv
IDENTITY = '--identity' in sys.argv
HEAD = sys.argv[sys.argv.index('--head') + 1] if '--head' in sys.argv else '# ---- 추가 ----'

def lines(path):
    for l in io.open(path, encoding='utf-8-sig').read().split('\n'):
        l = l.rstrip('\r')
        if l.strip():
            yield l

uniq = {}
for l in lines(os.path.join(D, 'uniq.tsv')):
    p = l.split('\t')
    uniq[int(p[0])] = (p[1], '\t'.join(p[2:]))
keys = {}
for l in lines(os.path.join(D, 'keys.tsv')):
    p = l.split('\t')
    keys[int(p[0])] = p[1].split(',')
skipped = []
sk = os.path.join(D, 'skipped.tsv')
if os.path.exists(sk):
    for l in lines(sk):
        p = l.split('\t')
        skipped.append((p[0], '\t'.join(p[1:])))

ko = {}
bad = []
for f in sorted(glob.glob(os.path.join(D, 't-*.tsv'))) + sorted(glob.glob(os.path.join(D, 'fix-*.tsv'))):
    name = os.path.basename(f)
    for l in lines(f):
        p = l.split('\t')
        if len(p) != 2 or not p[0].isdigit():
            bad.append((name, l[:80])); continue
        i = int(p[0])
        if name.startswith('t-') and i in ko:
            bad.append((name, 'DUP idx %d' % i))
        if i not in uniq:
            bad.append((name, 'UNKNOWN idx %d' % i)); continue
        ko[i] = p[1]

missing = [i for i in sorted(uniq) if i not in ko]

pat = {'$': r'\$[^$\s]*\$', '&': r'&[^&\s]*&', '{': r'\{[^{}]*\}', '<': r'<[^<>]*>', '[': r'\[[A-Z]\d+\]'}
def toks(s):
    c = collections.Counter()
    for n, rx in pat.items():
        for m in re.findall(rx, s):
            if n == '<' and m in ('<b>', '</b>'):
                continue
            c[(n, m)] += 1
    c[('\\n',)] = s.count('\\n')
    c[('|',)] = s.count('|')
    return c

warn = []
han = re.compile(r'[一-鿿]')
han_rows = 0
for i, k in sorted(ko.items()):
    ch = uniq[i][1]
    if '\t' in k or '\r' in k or not k.strip():
        warn.append((i, 'EMPTY/TAB', '', ''))
    a, b = toks(ch), toks(k)
    if a != b:
        warn.append((i, 'TOKENS', dict(a - b), dict(b - a)))
    if han.search(k):
        han_rows += 1

# 절 머리(번호 경계)는 hdr.txt가 있으면 그것을 쓴다: 번호<TAB># 머리글
hdr = []
hp = os.path.join(D, 'hdr.txt')
if os.path.exists(hp):
    for l in lines(hp):
        p = l.split('\t', 1)
        hdr.append((int(p[0]), p[1]))
out = []
total = 0
hi = 0
for i in sorted(ko):
    while hi < len(hdr) and hdr[hi][0] <= i:
        out.append(hdr[hi][1]); hi += 1
    for key in keys[i]:
        out.append(key + '\t' + ko[i]); total += 1
ident = 0
if IDENTITY:
    out.append('# 중국어가 없는 행(기호·말줄임표·숫자·빈 글) — 원문 그대로 적어 fallback과 rloc dump에 잡히지 않게 한다. 영어 플래그 0lkK4v는 검사용으로 그대로 둔다')
    for key, ch in skipped:
        if key != '0lkK4v' and '\t' not in ch:
            out.append(key + '\t' + ch); ident += 1

with io.open(os.path.join(D, 'merged.tsv'), 'w', encoding='utf-8', newline='\n') as w:
    w.write('\n'.join(out) + '\n')
with io.open(os.path.join(D, 'report.txt'), 'w', encoding='utf-8', newline='\n') as w:
    w.write('translated idx %d of %d; missing %s\n' % (len(ko), len(uniq), missing))
    w.write('rows %d (+identity %d); han rows %d; bad %d; warn %d\n' % (total, ident, han_rows, len(bad), len(warn)))
    for b in bad:
        w.write('BAD %s %s\n' % b)
    for wn in warn:
        w.write('WARN %d %s ch-only=%s ko-only=%s\n' % wn)
print('translated', len(ko), 'of', len(uniq), 'missing', missing)
print('rows', total, 'identity', ident, 'han_rows', han_rows, 'bad', len(bad), 'warn', len(warn))
for b in bad[:20]:
    print('BAD', b)
for wn in warn[:80]:
    print('WARN', wn[0], wn[1], 'ch-only=', wn[2], 'ko-only=', wn[3])

if APPLY:
    text = io.open(MODS, encoding='utf-8-sig').read()
    if not text.endswith('\n'):
        text += '\n'
    text += HEAD + '\n' + '\n'.join(out) + '\n'
    io.open(MODS, 'w', encoding='utf-8', newline='\n').write(text)
    n = sum(1 for l in text.split('\n') if l.strip() and not l.lstrip().startswith('#'))
    print('applied; mods.txt data rows now', n)

# 표 칸 번역 묶음(05 문서 74절)을 합쳐 Translations/tables.txt를 만든다.
#   py -3 -I -X utf8 tools/research/merge-table-texts.py <묶음 폴더> <이전 번역으로 맞춘 파일> <출력 tables.txt> [--apply]
# 묶음 폴더: chunk-NN.tsv(번호<TAB>원문)와 t-NN.tsv(번호<TAB>한국어), 선택 fix-NN.tsv(번호<TAB>한국어, 뒤의 것이 앞선다).
# 이전 번역으로 맞춘 파일: 원문<TAB>한국어(reuse 단계 결과).
# 검사: 번호가 모두 있는지, 줄마다 두 글자 \n과 < > { } & 개수가 같은지, 한국어에 한자가 남았는지(원문 그대로 둔 줄은 따로 센다).
# --apply 없이는 보고만 한다. 원문·번역의 줄바꿈은 두 글자 \n 그대로 적는다(TranslationTable.Parse가 줄바꿈으로 읽는다).
import os, re, sys, glob

BS = chr(92)
folder, reuse_path, out_path = sys.argv[1], sys.argv[2], sys.argv[3]
apply = '--apply' in sys.argv
han = re.compile('[一-鿿㐀-䶿]')


def read_tsv(path):
    rows = {}
    for line in open(path, encoding='utf-8'):
        line = line.rstrip('\n').rstrip('\r')
        if not line or line.startswith('#'):
            continue
        n, _, text = line.partition('\t')
        rows[n] = text
    return rows


src, ko = {}, {}
for p in sorted(glob.glob(os.path.join(folder, 'chunk-*.tsv'))):
    src.update(read_tsv(p))
for p in sorted(glob.glob(os.path.join(folder, 't-*.tsv'))):
    ko.update(read_tsv(p))
for p in sorted(glob.glob(os.path.join(folder, 'fix-*.tsv'))):
    ko.update(read_tsv(p))

missing = [n for n in src if n not in ko]
bad, han_left, kept = [], [], []
for n, text in src.items():
    k = ko.get(n)
    if k is None:
        continue
    for tok in (BS + 'n', '<', '>', '{', '}', '&'):
        if text.count(tok) != k.count(tok):
            bad.append((n, tok, text.count(tok), k.count(tok)))
            break
    if k == text:
        kept.append(n)
    elif han.search(k):
        han_left.append((n, ''.join(han.findall(k))[:20]))

pairs = []
for n in sorted(src, key=int):
    if n in ko and ko[n] != src[n]:
        pairs.append((src[n], ko[n]))
for line in open(reuse_path, encoding='utf-8'):
    a, _, b = line.rstrip('\n').partition('\t')
    if a and b and a != b:
        pairs.append((a, b))

print('source', len(src), 'translated', len(ko), 'missing', len(missing), missing[:10])
print('token mismatch', len(bad), bad[:10])
print('kept original', len(kept), kept[:20])
print('han left', len(han_left), han_left[:20])
print('pairs', len(pairs))
if apply:
    if missing or bad:
        sys.exit('not written: fix missing/mismatch first')
    with open(out_path, 'w', encoding='utf-8', newline='\n') as o:
        o.write('# 표 칸 전용 번역표(원문<TAB>한국어). 글 전체가 같을 때만 쓰고 화면 글의 부분 치환에는 쓰지 않는다. 05 문서 74절.\n')
        o.write('# 만들기: tools/research/merge-table-texts.py. "\\n"은 원문·번역 모두 줄바꿈 두 글자. 같은 원문은 뒤의 것.\n')
        for a, b in pairs:
            o.write(a + '\t' + b + '\n')
    print('written', out_path)

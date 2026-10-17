# 번역표 texts.txt(원문<TAB>한국어. 스펙 docs/superpowers/specs/2026-10-17-zh-text-korean-design.md 3.1절) 검사.
#   py -3 -I -X utf8 tools/research/check-texts.py src/ModCode/ModMain/Translations/texts.txt
# 검사: 줄 형식(탭이 있는지), 중복 원문, 한 글자 원문(경고: 글 전체가 같을 때만 쓰임), 자리 표시의 수가 원문과 번역에서 같은지
# ({…}, <…>, &…&, $…$, "\n"), 번역에 한자가 남았는지(바로 앞 줄이 "#병기"면 허용). 문제 0이면 "OK <줄 수>"로 끝나고 종료 코드 0, 아니면 1.
import re
import sys

ZH = re.compile(r'[一-鿿㐀-䶿]')
PLACEHOLDERS = [re.compile(r'\{[^{}]*\}'), re.compile(r'<[^<>]*>'), re.compile(r'&[^&\s]*&'), re.compile(r'\$[^$\s]*\$'), re.compile(r'\\n')]


def main(path):
    problems = 0
    warnings = 0
    rows = 0
    seen = {}
    allow_hanja = False
    with open(path, encoding='utf-8-sig') as f:
        lines = f.read().split('\n')
    for number, raw in enumerate(lines, 1):
        line = raw.rstrip('\r')
        stripped = line.strip()
        if stripped == '':
            continue
        if stripped.startswith('#'):
            allow_hanja = stripped.startswith('#병기')
            continue
        if '\t' not in line:
            print('%d: 탭이 없는 줄: %s' % (number, line[:60]))
            problems += 1
            allow_hanja = False
            continue
        source, korean = line.split('\t', 1)
        source = source.strip()
        korean = korean.strip()
        if source == '':
            print('%d: 원문이 빈 줄' % number)
            problems += 1
        elif source in seen:
            print('%d: 중복 원문(먼저 %d줄): %s' % (number, seen[source], source[:40]))
            problems += 1
        else:
            seen[source] = number
        if korean == '':
            print('%d: 번역이 빈 줄: %s' % (number, source[:40]))
            problems += 1
        if len(source) == 1:
            print('%d: 경고: 한 글자 원문(글 전체가 같을 때만 쓰임): %s' % (number, source))
            warnings += 1
        for pattern in PLACEHOLDERS:
            a = sorted(pattern.findall(source))
            b = sorted(pattern.findall(korean))
            if a != b:
                print('%d: 자리 표시가 다름 %s: 원문 %s / 번역 %s' % (number, pattern.pattern, a, b))
                problems += 1
        if ZH.search(korean) and not allow_hanja:
            print('%d: 번역에 한자가 남음: %s' % (number, korean[:60]))
            problems += 1
        allow_hanja = False
        rows += 1
    if problems == 0:
        print('OK %d줄 (경고 %d)' % (rows, warnings))
        return 0
    print('문제 %d개, 경고 %d개, %d줄' % (problems, warnings, rows))
    return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1]))

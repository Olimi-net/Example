#! python3
buf = {}
row = ''
tab = 4
last = {}
def is_num(t):
    return len(t) > 1 and t[1].isdigit()

def to_tab(k, t):
    r = []
    for e in list(t):
        if e == '~':
            r.append(' ' * k)
        else:
            r.append(e)
    return ''.join(r)

def put_row(k, t, n=0):
    return to_tab(k, ' '.join([t[i] for i in range(n, len(t))]))

def rem_space(t, k=4):
    r = []
    cnt = k
    for i in range(len(t)):
        if cnt > 0:
            if t[i] == ' ':
                cnt -= 1
                continue
            else:
                cnt = 0
        r.append(t[i])
    return ''.join(r)

def paste_row(n, buf, txt):
    if not n in buf:
        buf[len(buf)] = txt
        return buf
    d1 = {}
    for e in buf:
        if e == n:
            d1[len(d1)] = txt
        d1[len(d1)] = buf[e]
    return d1

def clear_or_del_row(n, buf):
    if not n in buf:
        return buf
    if buf[n] == '':
        d = {}
        for e in buf:
            if e != n:
                d[len(d)] = buf[e]
        return d
    buf[n] = ''
    return buf

hlp = {
    '~':'= replace ~ to tab (4 spase see -k)',
    '-a':'[liNe[TEXT]] = add line', 
    '-b':'Start [End[Count=4]] = rem offset', 
    '-c':'liNe = copy row',
    '-d':'liNe = clear or del row', 
    '-e':'[Start[End]] = show simbols',
#    '-f':'couNt_row serch_TEXT = search text',
# g
    '-h':'[-e] = help about', 
    '-i':'[Start[End]] = show simbols number',
    '-j':'liNe numbErs = replace row with chars',
    '-k':'[liNe] = show [set] space count in tab',
    '-l':'FILE_PATH = load file', 
    '-m':'liNe Element = comment row as E',
    '-n':'liNe Element = remove comment as E',
    # o
    '-p':'[Start[End]] = show rows',
    '-q':'close editor',
    '-r':'liNe [TEXT] = replace row',
    '-s':'FILE_PATH = save file',
    '-t':'Start [End[Count=4]] = add offset',
    # u
    '-v':'liNe = paste row',
    # w
    '-x':'liNe = cut row',
    # y
#    '-z':'= undo'
    }
hide_simbols = {' ':'s', '\t':'t', '\n':'n', '\r':'r'}
simbols_hide = {'s':' ', 't':'\t', 'n':'\n', 'r':'\r'}

while True:
    t = input().strip('\n')
    if len(t) == 0:
        continue
    txt = t.split()
    if txt[0] == '-q':
        break
    if txt[0] == '-a':
        if is_num(txt):
            buf = paste_row(int(txt[1]), buf, put_row(tab, txt, 1))
        else:
            buf[len(buf)] = put_row(tab, txt, 1)
        continue
    if txt[0] == '-b':
        if is_num(txt):
            n = int(txt[1])
            if len(txt) > 2 and txt[2].isdigit():
                en = int(txt[2])
                k = tab
                if len(txt) > 3 and txt[3].isdigit():
                    k = int(txt[3])
                for e in buf:
                    if n <= e <= en:
                        buf[e] = rem_space(buf[e], k)
            else:
                buf[n] = rem_space(buf[n], k)
        continue
    if txt[0] == '-c':
        if is_num(txt):
            row = buf[int(txt[1])]
        continue
    if txt[0] == '-d':
        if is_num(txt):
            buf = clear_or_del_row(int(txt[1]), buf)
        continue
    if txt[0] == '-e':
        bn = 0
        if is_num(txt):
            bn = max(bn, int(txt[1]))
        en = len(buf)
        if len(txt) > 2 and txt[2].isdigit():
            en = min(en, int(txt[2]))
        for e in buf:
            if bn <= e < en:
                print(str(e).rjust(5), end=' ')
                for i in range(len(buf[e])):
                    c = buf[e][i]
                    if c.isalnum():
                        print(c, end='')
                    elif c in hide_simbols:
                        print(f'[{hide_simbols[c]}]', end='')
                    else:
                        print(f'[{ord(c)}]', end='')
                print()
        continue
    if txt[0] == '-h':
        if len(txt) > 1 and txt[1] in hlp:
            print(hlp[txt[1]])
        else:
            for e in hlp:
                print(e, hlp[e])
        continue
    if txt[0] == '-i':
        bn = 0
        if is_num(txt):
            bn = max(bn, int(txt[1]))
        en = len(buf)
        if len(txt) > 2 and txt[2].isdigit():
            en = min(en, int(txt[2]))
        for e in buf:
            if bn <= e < en:
                print(str(e).rjust(5), end=' ')
                for i in range(len(buf[e])):
                    print(f'[{ord(buf[e][i])}]', end='')
                print()
        continue
    if txt[0] == '-j':
        if is_num(txt):
            r = []
            for i in range(2, len(txt)):
                if txt[i] in simbols_hide:
                    r.append(simbols_hide[txt[i]])
                    continue
                if not txt[i].isdigit():
                    break
                r.append(chr(int(txt[i])));
            buf[int(txt[1])] = ''.join(r)
        continue
    if txt[0] == '-k':
        if is_num(txt):
            tab = max(1, int(txt[1]))
        else:
            print(f'tab = {tab} space')
        continue
    if txt[0] == '-l':
        buf = {}
        f = open(txt[1], 'r')
        for r in f:
            buf[len(buf)] = r.strip('\n')
        f.close()
        continue
    if txt[0] == '-m':
        if is_num(txt) and len(txt) > 2:
            buf[int(txt[1])] = txt[2] + buf[int(txt[1])]
        continue
    if txt[0] == '-n':
        if is_num(txt) and len(txt) > 2:
            buf[int(txt[1])] = buf[int(txt[1])].lstrip(txt[2])
        continue
    if txt[0] == '-p':
        bn = 0
        if is_num(txt):
            bn = max(bn, int(txt[1]))
        en = len(buf)
        if len(txt) > 2 and txt[2].isdigit():
            en = min(en, int(txt[2]))
        for e in buf:
            if bn <= e < en:
                print(str(e).rjust(5), buf[e])
        continue
    if txt[0] == '-r':
        if is_num(txt):
            buf[int(txt[1])] = put_row(tab, txt, 2)
        continue
    if txt[0] == '-s':
        f = open(txt[1], 'w')
        for e in buf:
            f.write(buf[e] + '\n')
        f.close()
        continue
    if txt[0] == '-t':
        if is_num(txt):
            n = int(txt[1])
            if len(txt) > 2 and txt[2].isdigit():
                en = int(txt[2])
                k = tab
                if len(txt) > 3 and txt[3].isdigit():
                    k = int(txt[3])
                for e in buf:
                    if n <= e <= en:
                        buf[e] = ' ' * k + buf[e]
            else:
                buf[n] = ' ' * tab + buf[n]
        continue
    if txt[0] == '-v':
        if is_num(txt):
            buf = paste_row(int(txt[1]), buf, row)
        continue
    if txt[0] == '-x':
        if is_num(txt):
            row = buf[int(txt[1])]
            buf[int(txt[1])] = ''
        continue
    paste_row(-1, buf, t)

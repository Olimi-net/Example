#! python3
import random

# by olimi for stepik.org 2024-12-03
# по идее нужно разбить на модули и писать это в несколько классов, 
# но сделаем вид что мы пока ничего об этом не знаем :))

input_state = True
input_text = ''
output_text = ''

def get_input(*txt):
	if len(txt) == 0:
		if input_state:
			return input()
		return input_text
	if input_state:
		return input(txt[0] + ': ')
	global output_text
	output_text = txt[0]
	return input_text

def calc_attempts(n):
	total = 0
	while n > 1:
		n = n // 2
		total += 1
	return total + 1

def set_lang(txt):
	lang = txt.lower()
	if lang == 'en':
		return 1
	if lang == 'ру':
		return 2
	return 0

def get_difficult(n):
	if n > 3:
		return random.randint(1, 1000)
	return 10 * n

def exit_dialog(lang):
	txt = ['Error','Do you want to exit? Yes|No', 'Уже уходишь? Да|Нет']
	cmd = get_input(txt[lang]).lower()
	if cmd == 'no' or cmd == 'нет':
		return False
	return True

def valid_cmd(txt, num, lang):
	cmd = get_input(txt)
	if cmd.isdigit() and 1 <= int(cmd) <= num:
		return int(cmd)
	while True:
		t = ['Error','Please enter a number from 1 to {} or 0 for exit','Пожалуйста введите число от 1 до {} или 0 для выхода']
		print(t[lang].format(num))
		cmd = get_input(txt)
		if cmd.isdigit() and 1 <= int(cmd) <= num:
			return int(cmd)
		if exit_dialog(lang):
			return 0

def get_lang_game(lang):
	language = ['Have number from 1 to {}', 'You have {} attempts',"Unfortunately you didn't succeed",'You did it in {} tries']
	ru_language = ['Загадано число от 1 до {}', 'Нужно решить задачу за {} попытки','К сожалению тебе не удалось','Ты сделал это за {} попыток']
	lang_high = ['The number is greater','Come on bigger']
	ru_high = ['Число больше','Покрупнее давай']
	lang_less = ['The number is less','Hold your horses']
	ru_less = ['Число меньше','Придержи коней']
	lang_mot = ['Enter a number','Enter more',"Don't give up",'Almost succeeded','I believe in you']
	ru_mot = ['Введи число','Ещё вводи','Не сдавайся','Почти удалось','Я в тебя верю']
	lang_win =['You win','Congratulations']
	ru_win = ['Победа','Мои поздравления']
	if lang == 2:
		language = ru_language
		lang_high = ru_high
		lang_less = ru_less
		lang_mot = ru_mot
		lang_win = ru_win
	return language, lang_high, lang_less, lang_mot, lang_win

def game(lang, num):
	difficult = get_difficult(num)
	attempts = calc_attempts(difficult)
	guess_number = random.randint(1, difficult)
	language, lang_high, lang_less, lang_mot, lang_win = get_lang_game(lang)
	print((language[0] + ' ' + language[1]).format(difficult, attempts))
	txt = lang_mot[random.randint(1, len(lang_mot)) - 1]
	for i in range(1, attempts + 1):
		cmd = valid_cmd(txt, difficult, lang)
		if cmd == 0:
			return False
		elif cmd == guess_number:
			print(lang_win[random.randint(1, len(lang_win)) - 1] + '!', language[3].format(i))
			return True
		elif cmd < guess_number:
			txt =lang_high[random.randint(1, len(lang_high)) - 1]
		else:
			txt =lang_less[random.randint(1, len(lang_less)) - 1]
	else:
		print(language[2])
	return True

def menu(lang):
	en = ['Welcom to game', 'Guess the number','Please select difficulty','easy','normal','difficult','random','other for exit']
	ru = ['Приветствую в игре','Угадай число','Выбери сложность','легко','нормально','сложно','случайно','любое для выхода']
	language = en
	if lang == 2:
		language = ru
	print(language[0], language[1])
	while True:
		print(language[2])
		for i in range(1, 5):
			print(f'> {i} - {language[i + 2]}')
		print(f'> {language[7]}')
		cmd = get_input()
		if cmd.isdigit() and 1 <= int(cmd) <= 4:
			if not game(lang, int(cmd)):
				return
		elif exit_dialog(lang):
			return

def start():
	lang = set_lang(get_input('Select language en / Выберете язык ру'))
	if lang == 0:
		lang = set_lang(get_input('en | ру'))
	if lang == 0:
		print('Bye / Пока')
		return
	menu(lang)

# ========8<---------------
# test here
def test_calc_attempts():
	put = [1, 3, 4, 6, 8, 10, 20, 30, 40]
	out = [1, 2, 3, 3, 4, 4, 5, 5, 6]
	n = random.randint(0, sum(put))
	total = 0
	for i in range(len(put)):
		res = calc_attempts(put[i])
		if out[i] != res:
			print(f'invalid test_calc_attempts #{i} for: {put[i]}  result: {res} expected: {out[i]}')
		else:
			total += 1
	return len(put) == total

def test_set_lang():
	total = 0
	put = ['ru', 'ру','En','eN','рУ','py']
	out = [0, 2, 1, 1, 2, 0]
	for i in range(0, len(put)):
		res = set_lang(put[i])
		if out[i] != res:
			print(f'invalid test_set_lang #{i} for: {put[i]}  result: {res} expected: {out[i]}')
		else:
			total += 1
	return len(put) == total

def test_get_lang(txt):
	en_a, en_last = ord('a'), ord('z')
	ru_a, ru_last = ord('а'), ord('я')
	en_count = 0
	ru_count = 0
	for c in txt.lower():
		if en_a <= ord(c) <= en_last:
			en_count += 1
		elif ru_a <= ord(c) <= ru_last:
			ru_count += 1
	total = 0
	if en_count > 0:
		total += 1
	if ru_count > 0:
		total += 2
	return total

def test_exit_dialog():
	total = 0
	put = ['ru', 'ру','nO','eN','нЕт','py']
	out = [True, True, False, True, False, True]
	global input_text
	for i in range(0, len(put)):
		input_text = put[i]
		en_ru = (i % 2) + 1
		res = exit_dialog(en_ru)
		lang_num = test_get_lang(output_text) 
		if lang_num == en_ru:
			total += 1
		else:
			print(output_text)
			print(f'invalid test_set_lang #{i} for lang: {en_ru}  result: {lang_num} expected: {en_ru}')
		if out[i] != res:
			print(f'invalid test_set_lang #{i} for: {put[i]}  result: {res} expected: {out[i]}')
		else:
			total += 1
	return len(put) * 2 == total

def test_all():
	total = 0
	if test_calc_attempts():
		total += 1
	if test_set_lang():
		total += 1
	if test_exit_dialog():
		total += 1
	return total == 3

input_state = False
if test_all():
	input_state = True
	start()
else:
	print('Test fail')

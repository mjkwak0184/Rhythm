import os

while True:
	location = input("파일 위치 입력: ")
	if os.name == "nt":
		location = location.replace("\\", "/", -1).lstrip().rstrip()
	elif os.name == "posix":
		location = location.replace("\\", "", -1).lstrip().rstrip()
	with open(location, "r") as t:
		txt = t.read()
		lines = str(txt).replace(" ", "", -1).replace("\t", "").split("\n")
		note_start_time = 0
		for num in range(len(lines)):
			line = lines[num]
			if(len(line) == 0): continue
			if(line[0] == "#"): continue
			if line[0] == "s":
				if len(line.split(";")) != 2:
					print("%s줄 세미콜론 확인해주세요." % (num+1))
					continue
			elif line[0] == "l":
				if len(line.split(";")) <= 2:
					print("%s줄 세미콜론 확인해주세요." % (num+1))
					continue
			else:
				print("%s줄 첫 단어가 s 또는 l 로 시작하지 않습니다." % (num + 1))
				continue
			line = line[1:]
			if line[0] != ";":
				print("%s줄 s 또는 l 이후 따라오는 세미콜론을 확인해주세요." % (num+1))
				continue
			line = line[1:]
			semisplit = line.split(";")
			note_time = 0
			for num_semi in range(len(semisplit)):
				semi = semisplit[num_semi]
				if len(semi) == 0:
					print("%s줄 세미콜론 중복 확인해주세요." % (num+1))
					continue
				if semi.count(".") > 1 or semi.count(",") != 1:
					print("%s줄 . 또는 , 갯수 확인해주세요." % (num+1))
					continue
				commasplit = semi.split(",")
				if not commasplit[0].isdigit():
					print("%s줄 레인 정보 확인해주세요." % (num+1))
					continue
				if int(commasplit[0]) > 12 or int(commasplit[0]) < 0:
					print("%s줄 레인 정보 0~12 사이인지 확인해주세요." % (num+1))
					continue
				if not commasplit[1].replace(".", "", -1).isdigit():
					print("%s줄 문법 확인해주세요." % (num+1))
					continue
				if note_time >= float(commasplit[1]):
					print("%s줄 롱노트 시간 역행 확인해주세요." % (num+1))
					continue
				if num_semi == 0:
					if note_start_time > float(commasplit[1]):
						print("%s줄 노트가 앞 노트보다 시간이 빠릅니다." % (num+1))
						continue
					note_start_time = float(commasplit[1])
				note_time = float(commasplit[1])
				
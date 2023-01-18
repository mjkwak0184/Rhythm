location = input("파일 위치 입력: ")
location = location.replace("\\", "", -1).lstrip().rstrip()

offset = 0.04

out = "notes.txt"
w = open(out, "w")


with open(location, "r") as t:
	txt = t.read()
	lines = str(txt).split("\n")
	for num in range(len(lines)):
		line = lines[num]
		print(num)
		if(line.replace(" ", "", -1).replace("\t", "", -1)[0] == "#"):
			w.write(line + "\n")
			continue
		commasplit = line.split(",")
		w.write(commasplit[0])
		for s in commasplit[1:]:
			w.write(",")
			semisplit = s.split(";")
			time = float(semisplit[0])
			time += offset
			w.write("%.3f" % time)
			if len(semisplit) > 1:
				w.write(";" + semisplit[1])
		if num + 1 < len(lines):  w.write("\n")
w.close()
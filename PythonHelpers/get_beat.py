# 아래 값은 '피에스타' 영상에 최적화되어 있습니다. 참고만 해주세요.
# bpm = 123
# start_time = 1.463
# video_diff = 17.552
# 자세한 정보는 아래 링크 확인
# https://ssizone.notion.site/ebdb49364335416f9e45d7948c38fd73


# 최소/최댓값 = 0.25박자에 맞춘값, 사잇값 = 0.125박자에 맞춘 값
# 밑에 노래에 맞는 값을 입력해주세요
bpm = 115					# bpm
start_time = 0.39				# 박자 기준시간
video_diff = 14.011			# 영상에서 음악이 시작하기 전 시간을 입력

beat_4 = 60/bpm
beat_2 = 30/bpm
beat_1 = 15/bpm
beat_05 = 7.5/bpm

while True:
	num = input("영상 시간 입력: ")
	if num.count(".") > 1 or not num.replace(".", "", -1).isdigit():
		print("잘못 입력하셨습니다. 다시 입력해주세요.")
		continue
	num = float(num)
	num -= video_diff
	i = start_time
	while True:
		if i + beat_1 > num:
			break
		i += beat_1
	print()
	print("원본값: %.3f" % num)
	print("¼박자\t\t%s\t\t¼박자" % ("원본\t\t⅛박자" if num < i+beat_05 else "⅛박자\t\t원본"))
	print("%.3f\t%.3f\t%.3f\t%.3f" % (i, num if num < i+beat_05 else i+beat_05, i+beat_05 if num < i+beat_05 else num, i+beat_1))
	print()
	
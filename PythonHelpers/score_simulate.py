import math
import random
import statistics
import csv
multiple_run_enabled = False
# 0 : ScoreUp, 1 : RaveBonus
# [스킬 종류, 발동 간격, 발동 확률, 효과 시간, 스탯1, 스탯2, 파워]
# 스킬 샘플 = [0, 8, 28, 8, 35, 0, 6000]

card_list = {
	"birthday": [0, 10, 33, 10, 0, 0, 7500],
	"party1": [0, 5, 50, 10, 0, 0, 7500],
	"party2": [1, 10, 12, 12, 82, 45, 6077],
	"room1": [1, 8, 20, 8, 65, 30, 6026],
	"room2": [0, 8, 28, 8, 0, 0, 7500],
	"room3": [0, 8, 20, 8, 35, 0, 6113],
	"sport1": [0, 8, 20, 8, 35, 0, 6113],
	"sport2": [1, 8, 20, 8, 65, 30, 6026],
	"sport3": [0, 8, 28, 8, 0, 0, 7500],
	"winter1": [0, 8, 28, 8, 0, 0, 7500],
	"winter2": [0, 8, 20, 8, 35, 0, 6113],
	"winter3": [1, 8, 20, 8, 65, 30, 6026],
	"live1": [0, 9, 17, 10, 35, 35, 6013],
	"live2": [0, 10, 14, 10, 40, 40, 6041],
	"live3": [0, 8, 12, 8, 40, 40, 6159],
	"sukito1": [0, 10, 8, 12, 45, 45, 6048],
	"sukito2": [1, 5, 5, 10, 110, 0, 5985],
	"bueno1": [1, 10, 9, 12, 90, 50, 6051],
	"bueno2": [1, 10, 8, 12, 94, 50, 6056],
	"bueno3": [1, 10, 7, 12, 98, 50, 6062],
	"eyes1": [0, 10, 25, 9, 26, 42, 6125],
	"eyes2": [0, 12, 16, 12, 32, 48, 6119],
	"vamp1": [1, 10, 25, 10, 62, 15, 6070],
	"vamp2": [1, 10, 30, 10, 65, 0, 6063],
	"cute1": [0, 10, 25, 10, 30, 30, 6047],
	"cute2": [1, 10, 25, 10, 60, 25, 6012],
	"red1": [0, 10, 9, 12, 44, 44, 6071],
	"red2": [0, 10, 7, 12, 46, 46, 6195],
	"classy1": [1, 10, 33, 10, 53, 20, 6055],
	"classy2": [0, 9, 25, 10, 27, 27, 6073],
	"marine1": [1, 12, 20, 12, 65, 30, 6012],
	"marine2": [1, 10, 18, 11, 66, 35, 6021],
	"casual1": [0, 10, 30, 9, 23, 23, 6350],
	"casual2": [0, 10, 22, 9, 28, 50, 6155],
	"resort1": [1, 10, 25, 9, 60, 25, 6078],
	"resort2": [0, 10, 25, 9, 30, 30, 6101],
	"hallo1": [0, 8, 25, 7, 32, 32, 6096],
	"hallo2": [1, 8, 25, 8, 72, 0, 6103],
	"celeb1": [0, 8, 10, 10, 45, 45, 5923],
	"celeb2": [1, 8, 12, 10, 80, 40, 6000],
	"autumn": [0, 5, 50, 5, 20, 20, 5986],
	"christ1": [1, 7, 25, 7, 60, 25, 6087],
	"christ2": [0, 7, 25, 7, 33, 33, 5934],
	"black": [0, 5, 5, 10, 50, 50, 5893],
	"date": [1, 8, 33, 8, 53, 20, 6054],	
	"valentine": [0, 8, 33, 8, 30, 30, 5750],
	"twelve": [0, 12, 24, 12, 36, 36, 5647],
	"whiteday": [1, 8, 50, 8, 42, 15, 6030],
	"oneiric_t1": [1, 5, 8, 8, 100, 0, 5984],
	"oneiric_t2": [0, 5, 3, 10, 55, 55, 6156],
	"color": [1, 1, 1, 5, 200, 0, 5991],
	"rose1": [0, 8, 15, 12, 37, 37, 5702],
	"rose2": [1, 8, 15, 12, 75, 40, 5693],
	"violeta1": [0, 2, 20, 5, 30, 30, 5491],
	"violeta2": [1, 10, 30, 10, 38, 100, 6285],
	"sapphire1": [0, 10, 22, 14, 31, 31, 5649],
	"sapphire2": [1, 10, 22, 14, 65, 25, 5654],
	"bloom1": [1, 10, 30, 9, 55, 25, 6101],
	"bloom2": [0, 15, 18, 18, 34, 34, 5856],
	"bloom3": [1, 15, 18, 18, 66, 35, 5910],
	"bloom4": [0, 1, 1, 4, 100, 100, 6042],
	"diary1": [0, 7, 25, 8, 28, 28, 6048],
	"diary2": [1, 5, 50, 5, 45, 10, 6007],
	"oneiric1": [0, 25, 25, 25, 25, 25, 6030],
	"oneiric2": [1, 8, 50, 8, 35, 60, 6065],
	"onereeler1": [1, 5, 15, 10, 65, 30, 5701],
	"onereeler2": [1, 6, 24, 8, 75, 20, 5642],
	"onereeler3": [0, 8, 30, 12, 27, 27, 5659]
}


#card_list = {
#	"color": [1, 1, 1, 5, 200, 0, 5991],
#	"rose1": [0, 8, 15, 12, 37, 37, 5702],
#	"rose2": [1, 8, 15, 12, 75, 40, 5693],
#	"violeta1": [0, 2, 20, 5, 30, 30, 5491],
#	"violeta2": [1, 10, 30, 10, 38, 80, 6285],
#	"sapphire1": [0, 10, 22, 14, 31, 31, 5649],
#	"sapphire2": [1, 10, 22, 14, 65, 25, 5654],
#	"bloom1": [1, 10, 30, 9, 55, 25, 6101],
#	"bloom2": [0, 15, 18, 18, 34, 34, 5856],
#	"bloom3": [1, 15, 18, 18, 66, 35, 5910],
#	"bloom4": [0, 1, 1, 4, 100, 100, 5927],
#	"diary1": [0, 8, 25, 8, 28, 28, 6048],
#	"diary2": [1, 5, 50, 5, 45, 10, 6007],
#	"oneiric1": [0, 25, 25, 25, 25, 25, 6030],
#	"oneiric2": [1, 8, 50, 8, 35, 60, 6065],
#	"onereeler1": [1, 5, 15, 10, 65, 30, 5701],
#	"onereeler2": [1, 6, 24, 8, 75, 20, 5642],
#	"onereeler3": [0, 8, 30, 12, 27, 27, 5659]
#}


# 덱 목록입니다. 위 카드 리스트에서 프리셋을 가져와도 되며, 수동으로 값을 입력해도 됩니다.
skills = [
	card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["color"]
]

# 같은 카드 5장 사용시 아래 # 지우고 사용해도 됩니다
#skills = [card_list["celeb2"] for i in range(5)]


# 총 파워 - 0 으로 둘 경우 카드 기준으로 자동 계산되며, 0 이외의 값으로 두면 지정값으로 고정됩니다
total_power = 0
song_length = 109			# 곡 길이 (초)
note_per_second = 4			# 초당 노트 갯수
sperfect_chance = 96# S-PERFECT 판정 확률 (%) - 예를들어 90이면 S-PERFECT 90%, PERFECT 10%

# 시행 횟수
run_number = 250
# csv 파일 출력
write_output = False

# 파노라마 1:50초 / 436노트 => 109초 * 초당노트 4 = 436


# 한번에 덱 여러 종류를 비교할 수 있습니다. [] 사이에 skills (=덱) 목록을 , 로 구분하여 넣어주세요
# 사용하려면 multiple_run_enabled 을 True 로 바꿔주세요. 사용시 위 skills 목록은 무시됩니다.

multiple_run_enabled = True
#multiple_run = [
#	[card_list["twelve"] for i in range(5)],
#	[card_list["diary1"] for i in range(5)],
#	[card_list["diary2"] for i in range(5)],
#	[card_list["oneiric_1"] for i in range(5)],
#	[card_list["diary2"], card_list["diary2"], card_list["diary2"], card_list["diary2"], card_list["oneiric_diary"]]
#]
multiple_run = [[card_list[k] for _ in range(5)] for k in card_list.keys()]
multiple_run = [
	[card_list["violeta1"] for i in range(5)],
	[card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["color"]],
	[card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["party2"]],
	[card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["bueno1"]],
	[card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["onereeler2"]],
	[card_list["violeta1"], card_list["violeta1"], card_list["violeta1"], card_list["black"], card_list["onereeler2"]],
	[card_list["celeb1"], card_list["celeb1"], card_list["celeb1"], card_list["onereeler2"], card_list["onereeler2"]],
	[card_list["black"] for i in range(5)],
	[card_list["twelve"] for i in range(5)],
	[card_list["color"] for i in range(5)]
	
]
# multiple_run 예제

# 예제 1: 덱#1은 모든 카드를 트웰브로 구성, 덱#2는 모든 카드를 오나이릭2로 구성
# multiple_run = [[card_list["twelve"], card_list["twelve"], card_list["twelve"], card_list["twelve"], card_list["twelve"]], [card_list["oneiric_t2"], card_list["oneiric_t2"], card_list["oneiric_t2"], card_list["oneiric_t2"], card_list["oneiric_t2"]]]

# 예제 2: 덱#1은 트웰브2, 화이트데이3으로 구성, 덱 #2는 트웰브4, 화이트데이1로 구성
#multiple_run = [[card_list["twelve"], card_list["twelve"], card_list["whiteday"], card_list["whiteday"], card_list["whiteday"]], [card_list["twelve"], card_list["twelve"], card_list["twelve"], card_list["twelve"], card_list["whiteday"]]]

# 예제 3: 카드 목록에 있는 모든 카드 종류마다 순서대로 덱마다 같은카드 5장 장착한 후 테스트
# multiple_run = [[card_list[k] for _ in range(5)] for k in card_list.keys()]

# ==================



if write_output:
	outputFile = open("score_simulate_ssiz.csv", "w")
	outputFile.write("")
	csv_write = csv.writer(outputFile)
total_power_duplicate = total_power
if not multiple_run_enabled:
	multiple_run = [skills]
elif len(multiple_run) <= 0:
	print("multiple_run_enabled 가 활성화 되어 있으나 multiple_run 에 덱 목록이 구성되어 있지 않습니다. 덱 구성 확인 후 다시 시도하세요.")
	exit()
print(f"{run_number}번 시행 결과:")
for multirun in range(len(multiple_run)):
	skills = multiple_run[multirun]
	if total_power_duplicate == 0:
		total_power = 0
		for i in range(len(skills)):
			total_power += skills[i][6]
	scores = []
	
	for z in range(run_number):
		score = 0
		skill_timers = [0 for i in range(len(skills))]
		skill_active = [False for i in range(len(skills))]
		time = 0
		rave_gauge = 0
		totalNotes = song_length * note_per_second
		
		# for every second
		for i in range(song_length):
			# calculate scores
			# check skill status
			scoreup1 = 0
			scoreup2 = 0
			ravebonus1 = 0
			ravebonus2 = 0
			
			# check skill status
			for skill in range(len(skills)):
				if skill_active[skill]:
					if skills[skill][0] == 0: #scoreup
						scoreup1 += skills[skill][4]
						scoreup2 += skills[skill][5]
					elif skills[skill][0] == 1: #ravebonus
						ravebonus1 += skills[skill][4]
						ravebonus2 += skills[skill][5]
					
			# 3 notes per second
			for note in range(note_per_second):
				is_sperfect = sperfect_chance > random.randint(0, 99)
				if is_sperfect:
					# sperfect
					judgeMultiplier = 1 + (scoreup1 / 100)
				else:
					judgeMultiplier = (5/6) * (1 + (scoreup2 / 100))
				# calculate rave
				raveMultiplier = 0
				if rave_gauge >= 0.52:
					raveMultiplier = 0.9 # ultra rave
				elif rave_gauge >= 0.36:
					raveMultiplier = 0.7 # hyper rave
				elif rave_gauge >= 0.22:
					raveMultiplier = 0.5 # super rave
				elif rave_gauge >= 0.1:
					raveMultiplier = 0.3 # rave
				raveMultiplier *= ( 1 + ravebonus1 / 100 )
				score += round(0.06 * total_power * judgeMultiplier * (1 + raveMultiplier))
				
				# add rave gauge
				rave_gauge += ((1 if is_sperfect else 5/6) * (1 + (ravebonus2 / 100))/totalNotes)
				
			# for every skill
			for j in range(len(skills)):
				skill_timers[j] += 1
				
				if skill_active[j]:
					# if skill is active
					if skill_timers[j] >= skills[j][3]:
						# deactivate skill
						skill_timers[j] = 0
						skill_active[j] = False
				else:
					if skill_timers[j] >= skills[j][1]:
						skill_timers[j] = 0
						if skills[j][2] > random.randint(0, 99):
							skill_active[j] = True
		
		scores.append(score)
	if run_number > 1:
		scores.sort()
		top10perc = scores[int(len(scores) * 0.9)]
		output = f"최소 점수: {min(scores):,}\n평균 점수: {round(sum(scores)/run_number):,}\n상위 10%: {top10perc:,}\n최고 점수: {max(scores):,}\n표준 편차: {round(statistics.stdev(scores)):,}"
	else:
		output = f"평균 점수: {round(sum(scores)/run_number):,}\n최고 점수: {max(scores):,}\n최소 점수: {min(scores):,}"
	firstCard = skills[0]
	for i in range(1, len(skills)):
		if skills[i] != firstCard:
			break
	else:
		output += "\t\t"
		for key, value in card_list.items():
			if firstCard == value:
				output += f"({key})"
	print((f"덱 #{multirun}:\t" + output.replace("\n", "\t\t")) if multiple_run_enabled else output)
	if write_output:
		for scoreIndex in range(len(scores)):
			scores[scoreIndex] = round(scores[scoreIndex], -3)
		csv_write.writerow(scores)

if write_output: outputFile.close()
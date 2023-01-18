from flask import Flask, request
import sys, os
if hasattr(sys.modules["__main__"], "__file__"):
	main_name = os.path.basename(sys.modules["__main__"].__file__)
	if main_name == "run.py":
		from __main__ import app, db, db_raw
	else:
		# running from Docker
		from server import app, db, db_raw
from main import Helper, logger, STRINGS

import math



# new scoreboard implementation
class ScoreBoard():
	@staticmethod
	def getUserName(userid):
		'''
		Returns username of the user with given userid
		'''
	@staticmethod
	def getMemberNameFromZSet(zset_key, userid, score):
		range = db.zrange(zset_key, score, score, byscore=True)
		if len(range) == 0: return None
		for entry in range:
			if entry.find(userid) != -1:
				return entry
		return None

	@staticmethod
	def setScore(key, userid, score):
		'''
		Sets score of SCORE to user USERID in a Sorted Set named KEY
		'''
		pass

	def getScores(key, start = 0, end = 100):
		userid = request.form.get("userid", None)
		check = db.type(key)
		if check == "none":
			return {}
		elif check != "zset":
			# return None if type of key is not sorted set
			return None

		returnArr = {}
		scores = db.zrange(key, start, end, desc=True, withscores=True)
		
		rank = start + 1
		# iterate scores
		for score in scores:
			score_userid = score[0].split(",")[1]
			user_info = db.hmget(f"user:{score_userid}", "username", "privateid")
			if request.form.get("version", "2023.01.1") >= "2023":
				returnArr[rank] = {
					"privateid": user_info[1],
					"username": user_info[0],
					"score": int(score[1]),
					"rank": rank
				}
				if userid == score_userid: returnArr[rank]["my_score"] = True
			else:
				# respond with old format
				returnArr[rank] = [user_info[1], user_info[0], int(score[1])]
			# increment rank afterwards
			rank += 1
		return returnArr

@app.route("/getscoreboard", methods=["POST"])
def getscoreboard():
	GET_RANGE = 100		# Number of entries to fetch if start/end is not specified
	songid = request.form.get("songid")
	startrank = request.form.get("startrank", None)
	userid = request.form.get("userid", None)
	lang = request.form.get("lang", "ko")
	if songid == None or userid == None: return STRINGS["invalid"][lang]
	
	# validate startrank value
	if startrank == None: pass
	elif not startrank.isdigit(): return STRINGS["invalid"][lang]
	else: startrank = int(startrank)
	
	returnDict = {}
	# fetch user info and rank
	user_score = Helper.bitfieldHexToInt((db_raw.get(f"user-scores:{userid}") or b"").hex(), 8, songid)
	if user_score != 0:
		key = Helper.getMemberNameFromZSet(f"scoreboard:{songid}", userid, user_score)
		if key != None:
			rank = db.zrevrank(f"scoreboard:{songid}", key)
			if rank == None: return STRINGS["error"][lang]
			returnDict["myrank"] = rank + 1
		
	# if the user hasn't played the song yet, display from top
	if user_score == 0 and startrank == None: startrank = 0
	
	# if the user has not specified start index, set it to middle
	elif startrank == None: startrank = rank - int(GET_RANGE / 2)
	
	# calculate end position if it is not provided
	endrank = request.form.get("endrank", str(startrank + GET_RANGE - 1))	# inclusive, so remove 1 by default
	if not endrank.isdigit(): return STRINGS["invalid"][lang]
	endrank = int(endrank)
	if startrank < 0:
		endrank += -startrank
		startrank = 0
	
	if endrank < startrank: return STRINGS["invalid"][lang]
	
	returnDict["start"] = startrank
	# call zrange to fetch
	scores = ScoreBoard.getScores(f"scoreboard:{songid}", startrank, endrank)
	scores_endedAt = startrank + len(scores) - 1
	print(startrank, endrank, len(scores))
	if len(scores) == 0:
		# no more scores to load, bottom reached
		returnDict["reachedBottom"] = True
	elif endrank != scores_endedAt:
		# actual fetched number of scores is less than end - start
		returnDict["reachedBottom"] = True
	
	returnDict["rank"] = scores
	returnDict["end"] = scores_endedAt
	return returnDict


@app.route("/getweeklyscoreboard", methods=["POST"])
def getweeklyscoreboard(api = False):
	GET_RANGE = 100		# Number of entries to fetch if start/end is not specified
	startrank = request.form.get("startrank", None)
	userid = request.form.get("userid", None)
	lang = request.form.get("lang", "ko")
	if userid == None: return STRINGS["invalid"][lang]
	
	# check week number
	weekNumber = int(request.form.get("weeknumber", str(Helper.getWeekNumber())))
	weekoffset = int(request.form.get("weekoffset", 0))
	weekNumber = weekNumber + weekoffset

	# validate startrank value
	if startrank == None: pass
	elif not startrank.isdigit(): return STRINGS["invalid"][lang]
	else: startrank = int(startrank)
	
	returnDict = {}
	
	# fetch user info and rank
	allScore = db.hgetall(f"weekly-challenge-{weekNumber}-user:{userid}")
	user_score = 0
	for scores in allScore.values():
		user_score += int(scores)
	
	if user_score != 0:
		key = Helper.getMemberNameFromZSet(f"weekly-challenge-scores:{weekNumber}", userid, user_score)
		if key != None:
			rank = db.zrevrank(f"weekly-challenge-scores:{weekNumber}", key)
			if rank == None:
				# treat as if the user has no score
				user_score = 0
			else:
				returnDict["myrank"] = rank + 1
		
	# if the user hasn't played the song yet, display from top
	if user_score == 0 and startrank == None: startrank = 0
	
	# if the user has not specified start index, set it to middle
	elif startrank == None: startrank = rank - int(GET_RANGE / 2)
	
	# calculate end position if it is not provided
	endrank = request.form.get("endrank", str(startrank + GET_RANGE - 1))	# inclusive, so remove 1 by default
	if not endrank.isdigit(): return STRINGS["invalid"][lang]
	endrank = int(endrank)
	if startrank < 0:
		endrank += -startrank
		startrank = 0
		
	if endrank < startrank: return STRINGS["invalid"][lang]
	
	returnDict["start"] = startrank
	# call zrange to fetch
	scores = ScoreBoard.getScores(f"weekly-challenge-scores:{weekNumber}", startrank, endrank)
	scores_endedAt = startrank + len(scores) - 1
	print(startrank, endrank, len(scores))
	if len(scores) == 0:
		# no more scores to load, bottom reached
		returnDict["reachedBottom"] = True
	elif endrank != scores_endedAt:
		# actual fetched number of scores is less than end - start
		returnDict["reachedBottom"] = True
	
	returnDict["rank"] = scores
	returnDict["end"] = scores_endedAt

	# set challenge songs
	returnDict["songs"] = {}

	# fetch weekly challenge data
	weeklySongList = list(db.smembers(f"weekly-challenge-songs:{weekNumber}"))
	record_exists = db.exists(f"weekly-challenge-{weekNumber}-user:{userid}")
	for song in weeklySongList:
		if record_exists:
			returnDict["songs"][song] = db.hget(f"weekly-challenge-{weekNumber}-user:{userid}", song) or "0"
		else:
			# record does not exist
			if weekNumber <= 2761:
				returnDict["songs"][song] = "No Data" if lang == "en" else "데이터 없음"
			else:
				returnDict["songs"][song] = 0
	returnDict["currentWeek"] = Helper.getWeekNumber()
	
	return returnDict

def addScoreToWeeklyChallenge(userid, songid, score):
	weekNumber = Helper.getWeekNumber()
	if db.sismember(f"weekly-challenge-songs:{weekNumber}", songid):
		# song is in weekly challenge
		previousScore = db.hget(f"weekly-challenge-{weekNumber}-user:{userid}", songid)
		if previousScore == None: previousScore = 0
		if score > int(previousScore):
			# new record, update
			db.hset(f"weekly-challenge-{weekNumber}-user:{userid}", songid, score)
			allScore = db.hgetall(f"weekly-challenge-{weekNumber}-user:{userid}")
			sum = 0
			for scores in allScore.values():
				sum += int(scores)
			# update master scoreboard
			entry = Helper.getMemberNameFromZSet(f"weekly-challenge-scores:{weekNumber}", userid, sum - (score - int(previousScore)))
			if entry != None:
				# remove existing entry
				db.zrem(f"weekly-challenge-scores:{weekNumber}", entry)
			# insert
			t = Helper.reverseTimeString()
			db.zadd(f"weekly-challenge-scores:{weekNumber}", {"%s,%s" % (t, userid): sum})
			return sum
		else:
			return -1
	else:
		return -1


	
@app.route("/getweeklyrank_reward", methods=["GET"])
def getweeklyrank_reward():
	userid = request.args.get("userid")
	lang = request.args.get("lang", "ko")
	if userid != None:
		username = db.hget(f"user:{userid}", "username") or ""
	else:
		username = ""
		
	weekNumber = Helper.getWeekNumber()
	reward_multiplier = 1
	if db.exists(f"weekly-challenge-options:{weekNumber}"):
		reward_multiplier = db.hget(f"weekly-challenge-options:{weekNumber}", "reward-multiplier") or "1"
		if not reward_multiplier.isdigit():
			reward_multiplier = 1
		else:
			reward_multiplier = int(reward_multiplier)
			
	# fetch user info and rank
	allScore = db.hgetall(f"weekly-challenge-{weekNumber}-user:{userid}")
	user_score = 0
	
	for scores in allScore.values():
		user_score += int(scores)
		
	rank = -1
	totalrank = db.zcount(f"weekly-challenge-scores:{weekNumber}", "-inf", "inf")
	rankPercentage = 101
	if user_score != 0:
		key = Helper.getMemberNameFromZSet(f"weekly-challenge-scores:{weekNumber}", userid, user_score)
		if key != None:
			rank = db.zrevrank(f"weekly-challenge-scores:{weekNumber}", key)
			if rank == None:
				# treat as if the user has no score
				rank = -1
				user_score = 0
			else:
				rankPercentage = math.ceil(100 * rank / totalrank)
				rank += 1
	reward_collection = ""
	if weekNumber % 5 == 0:
		reward_collection = "Violeta Collection Part 1"
	elif weekNumber % 5 == 1:
		reward_collection = "Blooming Style Collection Part 4"
	elif weekNumber % 5 == 2:
		reward_collection = "Oneiric Collection Part 1"
	elif weekNumber % 5 == 3:
		reward_collection = "One-reeler / Act IV Collection Part 2"
	elif weekNumber % 5 == 4:
		reward_collection = "Colorful Style Collection Part 1"
	page = '''
	<!DOCTYPE html>
	<html>
	<head>
		<style>
			html{
				font-family: "SourceHanSans", "Noto Sans", Roboto, sans-serif;
				padding-left:10px;
				padding-right:10px;
				background-color:#F7F7F7;
				width:100%;
				height:100%;
			}

			table{
				border-collapse: collapse;
			}
			th, td{
				border: 1px solid #CCC;
				padding:3px;
			}
			.myrank{
				background-color:#F294BE;
			}
		</style>
	</head>
	<body>
		<div id="main">'''
	if username != "":
		if rank > 0:
			page += f'''
			<h5>{'Challenge Ranking - Resets every Monday 00:00 (KST)' if lang == 'en' else '챌린지 랭킹 (매주 월요일 00:00 초기화)'}</h5>
			<span style="font-weight:500;">{username} | {user_score:,}점 | {rank}위 | 상위 {rankPercentage}%</span><br>
			'''
		else:
			page += f'''<span style="font-weight:500;">{username} | {'No record' if lang == 'en' else '챌린지 랭킹 미참여'}</span><br>'''
		
		# event
		if weekNumber == 2745:
			playnum = db.bitfield(f"user-songclearcount:{userid}").get("u32", "#103").execute()[0]
			page += f'''<span>SMARTPHONE 플레이 횟수 : {playnum}</span>'''

	page += f'''<h4>{'Reward card collection' if lang == 'en' else '랭킹 보상 카드 컬렉션'}: {reward_collection}</h4>'''
	if reward_multiplier > 1:
		page += f"<br><span>[EVENT] {'Reward' if lang == 'en' else '보상'} ×{reward_multiplier}{'' if lang == 'en' else '배 이벤트 적용중'}</span>"
	page += f'''<br>
		<table>
			<tr> <th style="width:90px;text-align:center;">{'rank' if lang == 'en' else '순위'}</th>	<th style="width:270px">보상</th> </tr>
			<tr{" class='myrank'" if rank == 1 else ""}>
				<td>{'1st' if lang == 'en' else '1위'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(3600 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(150000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (R) × {(9 * reward_multiplier)}</td>
			</tr>
			<tr{" class='myrank'" if rank == 2 else ""}>
				<td>{'2nd' if lang == 'en' else '2위'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(3200 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(150000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (R) × {(8 * reward_multiplier)}</td>
			</tr>
			<tr{" class='myrank'" if rank == 3 else ""}>
				<td>{'3rd' if lang == 'en' else '3위'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(2800 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(150000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (R) × {(7 * reward_multiplier)}</td>
			</tr>
			<tr{" class='myrank'" if rank <= 12 and rank >= 4 else ""}>
				<td>{'4~12th' if lang == 'en' else '4~12위'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(2500 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(150000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (S~R) × {(6 * reward_multiplier)}</td>
			</tr>
			<tr{" class='myrank'" if rank <= 100 and rank >= 13 else ""}>
				<td>{'13~100th' if lang == 'en' else '13~100위'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(2000 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(150000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (S~R) × {(5 * reward_multiplier)}</td>
			</tr>
			<tr{" class='myrank'" if rank > 100 and rankPercentage <= 25 else ""}>
				<td>{'Top 25%' if lang == 'en' else '상위 25%'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(1500 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(120000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (A~R) × {(4 * reward_multiplier)}</td>
			</tr>
			<tr{" class='myrank'" if rank > 100 and rankPercentage > 25 and rankPercentage <= 50 else ""}>
				<td>{'Top 50%' if lang == 'en' else '상위 50%'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(1200 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(100000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (A~S) × {(3 * reward_multiplier)}</td>
			</tr>
			<tr{" class='myrank'" if rank > 100 and rankPercentage > 50 and rankPercentage <= 75 else ""}>
				<td>{'Top 75%' if lang == 'en' else '상위 75%'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(1000 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(70000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (A) × {(3 * reward_multiplier)}</td>
			</tr>
			<tr{" class='myrank'" if rank > 100 and rankPercentage > 75 and rankPercentage <= 100 else ""}>
				<td>{'Top 100%' if lang == 'en' else '참여 보상'}</td>
				<td>{'Diamond' if lang == 'en' else '다이아몬드'} × {(800 * reward_multiplier)}<br>{'SS coin' if lang == 'en' else 'SS코인'} × {(50000*reward_multiplier)}<br>{'Challenge Reward Card' if lang == 'en' else '랭킹 보상 카드'} (A) × {(2 * reward_multiplier)}</td>
			</tr>
		</table>
	</div>
	</body>
	</html>
	'''
	return page
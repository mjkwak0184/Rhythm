import sys, os
if hasattr(sys.modules["__main__"], "__file__"):
	main_name = os.path.basename(sys.modules["__main__"].__file__)
	if main_name == "run.py":
		from __main__ import app, db, db_raw
	else:
		from server import app, db, db_raw
from main import logger, Helper, STRINGS
from flask import Flask, request
import redis


# ==========


import random
import math
import time
import json


collectionNameById = {
	0: "Birthday Collection Part 1",
	1: "Party Dress Collection Part 1",
	2: "Party Dress Collection Part 2",
	3: "Room Wear Collection Part 1",
	4: "Room Wear Collection Part 2",
	5: "Room Wear Collection Part 3",
	6: "Sporty Style Collection Part 1",
	7: "Sporty Style Collection Part 2",
	8: "Sporty Style Collection Part 3",
	9: "Winter Style Collection Part 1",
	10: "Winter Style Collection Part 2",
	11: "Winter Style Collection Part 3",
	12: "Live Photo Collection Part 1",
	13: "Live Photo Collection Part 2",
	14: "Live Photo Collection Part 3",
	15: "SUKI TO IWASETAI Collection Part 1",
	16: "SUKI TO IWASETAI Collection Part 2",
	17: "Buenos Aires Collection Part 1",
	18: "Buenos Aires Collection Part 2",
	19: "Buenos Aires Collection Part 3",
	20: "EYES ON ME Collection Part 1",
	21: "EYES ON ME Collection Part 2",
	22: "Vampire Style Collection Part 1",
	23: "Vampire Style Collection Part 2",
	24: "Cute Style Collection Part 1",
	25: "Cute Style Collection Part 2",
	26: "Red Dress Collection Part 1",
	27: "Red Dress Collection Part 2",
	28: "Classy Style Collection Part 1",
	29: "Classy Style Collection Part 2",
	30: "Marine Style Collection Part 1",
	31: "Marine Style Collection Part 2",
	32: "Casual Style Collection Part 1",
	33: "Casual Style Collection Part 2",
	34: "Resort Style Collection Part 1",
	35: "Resort Style Collection Part 2",
	36: "Halloween Style Collection Part 1",
	37: "Halloween Style Collection Part 2",
	38: "Celebration Dress Collection Part 1",
	39: "Celebration Dress Collection Part 2",
	40: "Autumn Style Collection Part 1",
	41: "Christmas Style Collection Part 1",
	42: "Christmas Style Collection Part 2",
	43: "Black Dress Collection Part 1",
	44: "Winter Date Collection Part 1",
	45: "Valentine's Day Collection Part 1",
	46: "Twelve Collection Part 1",
	47: "White Day Collection Part 1",
	48: "Oneiric Theater Collection Part 1",
	49: "Oneiric Theater Collection Part 2",
	50: "Colorful Style Collection Part 1",
	51: "Rose Collection Part 1",
	52: "Rose Collection Part 2",
	53: "Violeta Collection Part 1",
	54: "Violeta Collection Part 2",
	55: "Sapphire Collection Part 1",
	56: "Sapphire Collection Part 2",
	57: "Blooming Collection Part 1",
	58: "Blooming Collection Part 2",
	59: "Blooming Collection Part 3",
	60: "Blooming Collection Part 4",
	61: "Diary Collection Part 1",
	62: "Diary Collection Part 2",
	63: "Oneiric Collection Part 1",
	64: "Oneiric Collection Part 2",
	65: "One-reeler / Act IV Collection Part I",
	66: "One-reeler / Act IV Collection Part II",
	67: "One-reeler / Act IV Collection Part III"
}

gachaLevelList = [1, 11, 31, 61, 121]
MAX_LEVEL = 240

levelTableNames = ["C", "B", "A", "S", "R"]
memberNames_ko = ["권은비", "미야와키 사쿠라", "강혜원", "최예나", "이채연", "김채원", "김민주", "야부키 나코", "혼다 히토미", "조유리", "안유진", "장원영"]
memberNames_en = ["Eunbi", "Sakura", "Hyewon", "Yena", "Chaeyeon", "Chaewon", "Minju", "Nako", "Hitomi", "Yuri", "Yujin", "Wonyoung"]

@app.route("/gacha_list", methods=["GET"])
def gacha_list():
	lang = request.args.get("lang", "ko")
	template = '''
	<!DOCTYPE html>
	<html>
		<head>
			<style>
			html{
				font-family: "SourceHanSans", "Noto Sans", Roboto, sans-serif;
			}

			table{
				border-collapse: collapse;
			}
			table, th, td{
				border: 1px solid #CCC;
				padding:3px;
			}
			.tab {
				border: 1px solid #F294BE;
				background-color: #F0F0F0;
			}
			.tab div{	
				border:none;
				outline:none;
				padding: 20px;
				display:inline-block;
			}

			.tabcontent {
				display:none;
			}
			input, textarea, button {
				appearance: none;
				-moz-appearance: none;
				-webkit-appearance: none;
				border-radius: 0;
				-webkit-border-radius: 0;
				-moz-border-radius: 0;
			}
			</style>
			<script>
				function opentab(name){
					let tabcontent = document.getElementsByClassName("tabcontent");
					for(var i = 0; i < tabcontent.length; i++){
						tabcontent[i].style.display = "none";
					}
					let tabs = document.getElementsByClassName("tablinks");
					for(var i = 0; i < tabs.length; i++){
						tabs[i].style.backgroundColor = "";
					}
					document.getElementById(name).style.display = "block";
					document.getElementById("tab" + name).style.backgroundColor = "#FFF";
				}
			</script>
		</head>
		<body>
	'''
	
	tabs = '''
	<div class="tab">
	'''
	collections = '''<div class="collections">'''
	gachaList = list(db.smembers("gacha-active"))
	firstGacha = -1
	gachaList.sort(reverse=True)
	for gacha in gachaList:
		if not db.exists(f"gacha-item:{gacha}"): continue
		gachaInfo = db.hmget(f"gacha-item:{gacha}", "start-time", "end-time", "name", "gacha-collection", "gacha-member", "gacha-level-table")
		
		if gachaInfo[1] != "0":
			# check start, end time
			if not gachaInfo[0].isdigit() or not gachaInfo[1].isdigit():
				continue
			if time.time() < int(gachaInfo[0]) or time.time() > int(gachaInfo[1]):
				continue
	
		# within sale time
		if firstGacha == -1: firstGacha = gacha
		
		gachaCollectionList = json.loads(gachaInfo[3])
		gachaMemberList = json.loads(gachaInfo[4])
		gachaLevelTable = json.loads(gachaInfo[5])
		
		# add tab
		tabs += f'''
		<div class="tablinks" id="tabitem{gacha}" onclick="opentab('item{gacha}')">{gachaInfo[2]}</div>
		'''
		
		collections += f'''
		<div class="tabcontent" id="item{gacha}">
		<h1>{gachaInfo[2]}</h1>
		<h4>{'Chance for each card rank' if lang == 'en' else '등급별 출현 확률'}</h4>
		<table>
			<tr>		<th style="width:100px">{'Rank' if lang == 'en' else '등급'}</th>		<th style="width:100px">{'Chance' if lang == 'en' else '확률'}</th> </tr>
			<tr>		<td style="text-align:center;">R</td>		<td>{gachaLevelTable[4]*100:.2f}%</td>	</tr>
			<tr>		<td style="text-align:center;">S</td>		<td>{gachaLevelTable[3]*100:.2f}%</td>	</tr>
			<tr>		<td style="text-align:center;">A</td>		<td>{gachaLevelTable[2]*100:.2f}%</td>	</tr>
			<tr>		<td style="text-align:center;">B</td>		<td>{gachaLevelTable[1]*100:.2f}%</td>	</tr>
			<tr>		<td style="text-align:center;">C</td>		<td>{gachaLevelTable[0]*100:.2f}%</td>	</tr>
		</table>
		<h4>{'List of collection & members' if lang == 'en' else '출현 컬렉션 및 멤버'}</h4>
		<table>
			<tr>		<th>{'Collection' if lang == 'en' else '컬렉션'}</th>	<th>{'Chance' if lang == 'en' else '컬렉션 출현 확률'}</th>	<th style="">{'Obtainable member list' if lang == 'en' else '멤버 목록'}</th> <tr>
		'''
		
		collectionAdded = set()
		for i in range(len(gachaCollectionList)):
			if gachaCollectionList[i] in collectionAdded:
				continue
			collectionAdded.add(gachaCollectionList[i])
			
			# member list
			if lang == "en":
				memberList = [memberNames_en[i] for i in gachaMemberList]
			else:
				memberList = [memberNames_ko[i] for i in gachaMemberList]
			collections += f'''
			<tr>
				<td style="font-size:11pt">{collectionNameById[gachaCollectionList[i]]}</td>
				<td style="font-size:11pt;text-align:center">{100 * gachaCollectionList.count(gachaCollectionList[i]) / len(gachaCollectionList):.2f}%</td>
				<td style="font-size:10pt !important;">{", ".join(memberList)}</td>
			</tr>
			'''
		collections += "</table></div>"
	template += tabs + "</div>"
	template += collections + "</div>"
	print(firstGacha)
	if firstGacha != -1: template += f'''<script>opentab("item{firstGacha}")</script>'''
	template += f'''
		</body>
	</html>
	'''
	return template

def gachaCard(userid, collectionList, memberList, levelChanceTable, amount):
	# draw
	pipe = db_raw.bitfield(f"user-cards:{userid}")
	changes = []		# <collectionId>:<memberId>:<level>:<existing>:<final_result>
	cardlistRaw = db_raw.get(f"user-cards:{userid}")
	if cardlistRaw == None:
		db_raw.set(f"user-cards:{userid}", "")
		cardlistRaw = b""
	cardlistHex = cardlistRaw.hex()
	cardlist = [cardlistHex[i:i+2] for i in range(0, len(cardlistHex), 2)]
	
	for i in range(amount):
		collection = random.choice(collectionList)
		level = random.choices(gachaLevelList, levelChanceTable)[0]
		memberId = random.choice(memberList)
		
		position = getCardPosition(collection, memberId)
		# add level
		current = int(str(cardlist[position]), 16) if len(cardlist) > position else 0
		if level > current: # drew a higher-grade card, overwrite new level and add half of current
			new = level + math.ceil(current / 2)
		else:
			new = current + math.ceil(level / 2)
			if level != gachaLevelList[0]: new -= 1
		if new > MAX_LEVEL: new = MAX_LEVEL
		# run update command
		pipe.set("u8", f"#{position}", new)
		# update current list so that duplicate cards are not messed up
		while position >= len(cardlist):
			cardlist.append("0")
		cardlist[position] = hex(new)
		# add result to changes list
		changes.append(f"{collection}:{memberId}:{level}:{current}:{new}")
		
	# update db
	pipe.execute()
	return changes

def getCardPosition(collectionId, memberId):
	return int(collectionId) * 12 + int(memberId)

@app.route("/card_gacha", methods=["POST"])
def card_gacha():
	userid = request.form.get("userid")
	password = request.form.get("password")
	itemid = int(request.form.get("itemid", -1))
	amount = request.form.get("amount")
	version = request.form.get("version", "0")
	lang = request.form.get("lang", "ko")
	
	if userid == None or password == None or itemid == -1 or amount == None: return STRINGS["invalid"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	# check version
	if version < db.hget("server-settings", "minimum-version"):
		return STRINGS["update_required"][lang]
	
	if not amount.isdigit(): return STRINGS["invalid"][lang]
	# check password
	if password != db.hget(f"user:{userid}", "password"): return STRINGS["invalid"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	if db.exists(f"gacha-item:{itemid}") == 0:
		if lang == "en":
			return { "success": False, "message": "Sale for this item has ended." }
		else:
			return { "success": False, "message": "판매가 종료된 상품입니다." }
	
	# check sale time
	itemTime = db.hmget(f"gacha-item:{itemid}", "start-time", "end-time")
	if int(itemTime[1]) != 0:
		if time.time() < int(itemTime[0]) or time.time() > int(itemTime[1]):
			if lang == "en":
				return { "success": False, "message": "Sale for this item has ended." }
			else:
				return { "success": False, "message": "판매가 종료된 상품입니다." }
	
	# get item info
	itemInfo = db.hmget(f"gacha-item:{itemid}", "gacha-collection", "gacha-member", "gacha-level-table", "is-diamond", "unit-price")
	
	# check user wallet
	currency = "diamond" if itemInfo[3] == "true" else "sscoin"
	money = int(db.hget(f"user:{userid}", currency))
	amount = int(amount)
	unitprice = int(itemInfo[4])
	if money < unitprice * amount:
		if lang == "en":
			return { "success": False, "message": "You do not have enough diamonds or SS coins to purchase this item." }
		else:
			return { "success": False, "message": "구매에 필요한 재화가 부족합니다." }
	else:
		new_money = db.hincrby(f"user:{userid}", currency, -unitprice*amount)
	
	# draw
	changes = gachaCard(userid, json.loads(itemInfo[0]), json.loads(itemInfo[1]), json.loads(itemInfo[2]), amount)
	
	returnDict = { "success": True, "result": changes, "update": { "cards": db_raw.get(f"user-cards:{userid}").hex() } }
	returnDict["update"][currency] = new_money
	
	logger.info(f"{userid} | 카드뽑 #{itemid} {amount}장")
	return returnDict
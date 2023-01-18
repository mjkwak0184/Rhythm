import sys, os
if hasattr(sys.modules["__main__"], "__file__"):
	main_name = os.path.basename(sys.modules["__main__"].__file__)
	if main_name == "run.py":
		from __main__ import app, db, db_raw
	else:
		from server import app, db, db_raw
from main import Helper, logger, STRINGS

from flask import request
import unicodedata
import uuid
import random
import datetime
import time
import json

import cards as module_cards
import group as module_group
import main as module_main


# LOGIN REWARD
def get_event_login():
	date = datetime.datetime.now().strftime("%m/%d")
	EVENT_LOGIN_REWARD = {
		"01/11": '''{"type":"gacha","description":"이채연 생일 기념","gachaDescription":"Birthday Collection - 이채연","receivedAt":"%s","collectionList":[0],"memberList":[4],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"02/05": '''{"type":"gacha","description":"김민주 생일 기념","gachaDescription":"Birthday Collection - 김민주","receivedAt":"%s","collectionList":[0],"memberList":[6],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"02/06": '''{"type":"gacha","description":"Suki to Iwasetai 발매 기념","gachaDescription":"SUKI TO IWASETAI Collection 뽑기","receivedAt":"%s","collectionList":[15,16],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0,0.75,0.2,0.05],"value": 3}''',
		"02/17": '''{"type":"gacha","description":"BLOOM*IZ 발매 기념","gachaDescription":"Blooming Collection 뽑기","receivedAt":"%s","collectionList":[57,58,59,60],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0,0.75,0.2,0.05],"value": 3}''',
		"03/19": '''{"type":"gacha","description":"미야와키 사쿠라 생일 기념","gachaDescription":"Birthday Collection - 미야와키 사쿠라","receivedAt":"%s","collectionList":[0],"memberList":[1],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"04/01": '''{"type":"gacha","description":"HEART*IZ 발매 기념","gachaDescription":"Violeta & Sapphire Collection 뽑기","receivedAt":"%s","collectionList":[53,54,55,56],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0,0.75,0.2,0.05],"value": 3}''',
		"06/15": '''{"type":"gacha","description":"Oneiric Diary 발매 기념","gachaDescription":"Diary & Oneiric Collection 뽑기","receivedAt":"%s","collectionList":[61,62,64],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0,0.75,0.2,0.05],"value": 3}''',
		"06/18": '''{"type":"gacha","description":"야부키 나코 생일 기념","gachaDescription":"Birthday Collection - 야부키 나코","receivedAt":"%s","collectionList":[0],"memberList":[7],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"06/26": '''{"type":"gacha","description":"Buenos Aires 발매 기념","gachaDescription":"Buenos Aires Collection 뽑기","receivedAt":"%s","collectionList":[17,18,19],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0,0.75,0.2,0.05],"value": 3}''',
		"07/05": '''{"type":"gacha","description":"강혜원 생일 기념","gachaDescription":"Birthday Collection - 강혜원","receivedAt":"%s","collectionList":[0],"memberList":[2],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"08/01": '''{"type":"gacha","description":"김채원 생일 기념","gachaDescription":"Birthday Collection - 김채원","receivedAt":"%s","collectionList":[0],"memberList":[5],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"08/31": '''{"type":"gacha","description":"장원영 생일 기념","gachaDescription":"Birthday Collection - 장원영","receivedAt":"%s","collectionList":[0],"memberList":[11],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"09/01": '''{"type":"gacha","description":"안유진 생일 기념","gachaDescription":"Birthday Collection - 안유진","receivedAt":"%s","collectionList":[0],"memberList":[10],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"09/25": '''{"type":"gacha","description":"Vampire 발매 기념","gachaDescription":"Vampire Collection 뽑기","receivedAt":"%s","collectionList":[22,23],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0,0.75,0.2,0.05],"value": 3}''',
		"09/27": '''{"type":"gacha","description":"권은비 생일 기념","gachaDescription":"Birthday Collection - 권은비","receivedAt":"%s","collectionList":[0],"memberList":[0],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"09/29": '''{"type":"gacha","description":"최예나 생일 기념","gachaDescription":"Birthday Collection - 최예나","receivedAt":"%s","collectionList":[0],"memberList":[3],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"10/06": '''{"type":"gacha","description":"혼다 히토미 생일 기념","gachaDescription":"Birthday Collection - 혼다 히토미","receivedAt":"%s","collectionList":[0],"memberList":[8],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"10/21": '''{"type":"gacha","description":"Twelve 발매 기념","gachaDescription":"Twelve Collection Pt. 1","receivedAt":"%s","collectionList":[46],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0,0.75,0.2,0.05],"value": 3}''',
		"10/22": '''{"type":"gacha","description":"조유리 생일 기념","gachaDescription":"Birthday Collection - 조유리","receivedAt":"%s","collectionList":[0],"memberList":[9],"levelChanceTable":[0,0,0,0,1],"value": 1}''',
		"10/29": '''{"type":"diamond","description":"아이즈원 데뷔 기념","receivedAt":"%s","value":3000}''',
		"12/07": '''{"type":"gacha","description":"One-reeler / Act IV 발매 기념","gachaDescription":"One-reeler / Act IV Collection 뽑기","receivedAt":"%s","collectionList":[65,66,67],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0,0.75,0.2,0.05],"value": 3}'''
	}
	if date in EVENT_LOGIN_REWARD:
		return (EVENT_LOGIN_REWARD[date] % datetime.datetime.now().strftime("%Y.%m.%d %H:%M"))
	else:
		return None

def get_daily_login():
	DAILY_REWARDS = [
		'''{"type":"sscoin","description":"데일리 로그인 보너스","receivedAt":"%s","value":10000}''',
		'''{"type":"sscoin","description":"데일리 로그인 보너스","receivedAt":"%s","value":20000}''',
		'''{"type":"sscoin","description":"데일리 로그인 보너스","receivedAt":"%s","value":30000}''',
		'''{"type":"gacha","description":"데일리 로그인 보너스","gachaDescription":"데일리 로그인 보너스 뽑기","receivedAt":"%s","collectionList":[3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0.65,0.345,0.005,0],"value": 1}''',
		'''{"type":"gacha","description":"데일리 로그인 보너스","gachaDescription":"데일리 로그인 보너스 뽑기","receivedAt":"%s","collectionList":[3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0.65,0.345,0.005,0],"value": 2}''',
		'''{"type":"gacha","description":"데일리 로그인 보너스","gachaDescription":"데일리 로그인 보너스 뽑기","receivedAt":"%s","collectionList":[3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0.65,0.345,0.005,0],"value": 3}''',
		'''{"type":"gacha","description":"데일리 로그인 보너스","gachaDescription":"데일리 로그인 보너스 뽑기","receivedAt":"%s","collectionList":[3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49],"memberList":[0,1,2,3,4,5,6,7,8,9,10,11],"levelChanceTable":[0,0.65,0.345,0.005,0],"value": 5}'''
	]
	draw = random.choices(DAILY_REWARDS, weights=[0.3, 0.15, 0.05, 0.15, 0.15, 0.15, 0.05])[0]
	return draw % datetime.datetime.now().strftime("%Y.%m.%d %H:%M")

def get_consecutive_login(days):
	lang = request.form.get("lang", "ko")
	if days == 1:
		value = 100
	elif days == 2:
		value = 110
	elif days == 3:
		value = 120
	elif days == 4:
		value = 130
	elif days == 5:
		value = 140
	elif days == 6:
		value = 150
	elif days == 7:
		value = 160
	elif days == 8:
		value = 170
	elif days == 9:
		value = 180
	elif days == 10:
		value = 190
	elif days >= 11 and days < 100:
		value = 200
	elif days >= 100 and days < 365:
		value = 220
	elif days >= 365:
		value = 250
	else:
		value = 100
	if lang == "en":
		return '''{"type":"diamond","description":"Consecutive login bonus (Day %s)","receivedAt":"%s","value":%s}''' % (days, datetime.datetime.now().strftime("%Y.%m.%d %H:%M"), value)
	else:
		return '''{"type":"diamond","description":"연속 로그인 보너스 (%s일차)","receivedAt":"%s","value":%s}''' % (days, datetime.datetime.now().strftime("%Y.%m.%d %H:%M"), value)


# New User check email eligibility
@app.route("/newuser_checkemail", methods=["POST"])
def newuser_checkemail():
	lang = request.form.get("lang", "ko")
	playAllowed = Helper.playNotAllowed(None)
	if playAllowed != None: return { "success": False, "message": playAllowed }

	# return true if email auth is not required
	if db.hget("server-settings", "require-email-auth") != "true":
		return { "success": True }
	
	# get email
	email = request.form.get("email")
	if email == None: return STRINGS["invalid"][lang]
	
	# check email
	if db.exists(f"user:{email}") != 0:
		if lang == "en":
			return { "success": False, "message": f"[{email}]\n\nAn user account is already registered with the above email.\nPlease connect to your existing account." }
		else:
			return { "success": False, "message": f"[{email}]\n\n위 이메일로 등록된 계정이 이미 있습니다.\n기존 계정 연결을 통해 로그인해 주세요." }
		
	
	status = module_group.email_in_group(email)
	if not status:
		if lang == "en":
			return { "success": False, "message": "You need to register a Gmail account in order to play.\nPlease try again after registering your email.\n\n<Press OK button to proceed to registration website>", "url": "https://wiz-one.space/rhythmiz_register" }
		else:
			return { "success": False, "message": "게임을 플레이하려면 구글 계정 등록이 필요합니다.\n이메일 등록 사이트에서 사용 메일 등록 후 다시 시도해 주세요.\n\n<확인 버튼을 누르면 사이트로 이동합니다>", "url": "https://wiz-one.space/rhythmiz_register" }
	else:
		return { "success": True }
	

# TitleScene NewUser
@app.route("/newuser", methods=["POST"])
def newuser():
	lang = request.form.get("lang", "ko")
	# check play not allowed
	if request.form.get("private") == None:
		playAllowed = Helper.playNotAllowed(None)
		if playAllowed != None: return { "success": False, "message": playAllowed }
	
	# get username
	username = request.form.get("username")
	if username == None:	# if username is not included in the POST body, return error
		return STRINGS["invalid"][lang]
	if username.find(",") != -1 or username.find(":") != -1:
		if lang == "en":
			return { "success": False, "message": "Your nickname contains  special characters that are not allowed.\nPlease try again with a different nickname." }
		else:
			return { "success": False, "message": "사용할 수 없는 특수문자가 포함되었습니다.\n다시 시도해 주세요." }
	username = unicodedata.normalize("NFC", username)	# normalize Korean
	if len(username) < 2 or len(username) > 12:		# filter out names too long/short
		if lang == "en":
			return { "success": False, "message": "Nickname must be between 2~12 characters." }
		else:
			return { "success": False, "message": "닉네임은 최소 2자, 최대 12자까지 입력 가능합니다." }
	
	# check email
	email = request.form.get("email")
	if email == None:
		return STRINGS["invalid"][lang]
	status = module_group.email_in_group(email)
	if not status:
		if lang == "en":
			return { "success": False, "message": "You need to register a Google email account in order to play.\nPlease try again after registering your email."}
		else:
			return { "success": False, "message": "계정을 생성하려면 구글 계정 등록이 필요합니다.\n이메일 등록 사이트에서 사용 메일 등록 후 다시 시도해 주세요." }
	
	if db.exists(f"user:{email}") != 0:
		if lang == "en":
			return { "success": False, "message": f"[{email}]\n\nAn user account is already registered with the above email.\nPlease connect to your existing account." }
		else:
			return { "success": False, "message": f"[{email}]\n\n위 이메일로 등록된 계정이 이미 있습니다.\n기존 계정 연결을 통해 로그인해 주세요." }
	
	userid = email.replace("\n", "").replace("\t", "").replace(" ", "")
	if userid.find(",") != -1 or userid.find(":") != -1:
		if lang == "en":
			return { "success": False, "message": "Your email contains special characters that cannot be used.\nPlease register with a different email." }
		else:
			return { "success": False, "message": "이메일에 사용할 수 없는 특수문자가 포함되어 있습니다.\n다른 이메일 계정으로 다시 등록해주세요." }
	password = f"{random.randrange(0, 10000000000000000):016d}"
	privateid = str(uuid.uuid4())
	# set initial user data
	pipe = db.pipeline()
	pipe.set(f"user-cards:{userid}", "")		# card data
	pipe.set(f"user-scores:{userid}", "") #user high score
	pipe.set(f"user-songclearstar:{userid}", "")		# song clear star
	pipe.set(f"user-songclearcount:{userid}", "")
	if lang == "en":
		pipe.hset(f"user-inbox:{userid}", str(uuid.uuid4()), '''{"type":"gacha","description":"Welcome Bonus","gachaDescription":"Party Dress Collection Pt. 1 - Eunbi","receivedAt":"%s","collectionList":[1],"memberList":[0],"levelChanceTable":[0,0,0,1,0],"value": 1}''' % datetime.datetime.now().strftime("%Y.%m.%d %H:%M"))
	else:
		pipe.hset(f"user-inbox:{userid}", str(uuid.uuid4()), '''{"type":"gacha","description":"웰컴 보너스","gachaDescription":"Party Dress Collection Pt. 1 - 권은비","receivedAt":"%s","collectionList":[1],"memberList":[0],"levelChanceTable":[0,0,0,1,0],"value": 1}''' % datetime.datetime.now().strftime("%Y.%m.%d %H:%M"))
	pipe.hset("privateid-userid", privateid, userid)
	pipe.execute()
	
	result = db.hset("user:%s" % userid, mapping={
		"username": username,
		"password": password,
		"level": "1",
		"exp": "0",
		"diamond": "1200",
		"sscoin": "100000",
		"lastlogin": 0,
		"totallogin": 0,
		"consecutivelogin": 1,
		"consecutiveloginrecord": 0,
		"playcount": 0,
		"nomisscount": 0,
		"allsperfectcount": 0,
		"top12": 0,
		"profilecard": "",
		"privateid": privateid
	})
	# ce634019-5bff-407f-bbd1-1ee5b3296f24
	
	logger.info(f"{userid} | 계정생성")
	if result:	# if DB write was successful return data
		return {
			"success": True,
			"userid": userid,
			"password": password
		}
	else:	# if DB write failed, return error
		return STRINGS["error"][lang]
		

@app.route("/generate_password", methods=["POST"])
def generate_password():
	userid = request.form.get("userid")
	password = request.form.get("password")
	lang = request.form.get("lang", "ko")
	if userid == None or password == None: return STRINGS["invalid"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	if not db.exists("user:%s" % userid):
		if lang == "en":
			return { "success": False, "message": "User account does not exist." }
		else:
			return { "success": False, "message": "존재하지 않는 유저입니다." }
	if password != db.hget(f"user:{userid}", "password"):	# password doesn't match
		if lang == "en":
			return { "success": False, "message": "Your login credentials have expired.\nPlease reload the game to update your account data." }
		else:
			return { "success": False, "message": "로그인 정보가 만료되어 재발급에 실패하였습니다.\n재접속하여 로그인 정보를 갱신해 주세요." }

	# generate new password
	newpass = f"{random.randrange(0, 10000000000000000):016d}"
	
	# update DB
	db.hset(f"user:{userid}", "password", newpass)
	logger.info(f"{userid} | 연동코드생성: [{newpass}]")
	return { "success": True, "password": newpass }
	
@app.route("/link_account", methods=["POST"])
def link_account():
	userid = request.form.get("userid")
	password = request.form.get("password")
	lang = request.form.get("lang", "ko")
	if userid == None or password == None: return STRINGS["invalid"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	if not db.exists("user:%s" % userid):
		if lang == "en":
			return { "success": False, "message": "Requested user account does not exist.\nPlease try again." }
		else:
			return { "success": False, "message": "존재하지 않는 유저ID입니다.\n다시 시도해 주세요." }
	if password != db.hget(f"user:{userid}", "password"):	# password doesn't match
		if lang == "en":
			return { "success": False, "message": "Entered account login code is incorrect.\nPlease try again." }
		else:
			return { "success": False, "message": "연동 코드를 잘못 입력했습니다.\n다시 시도해 주세요." }
	return { "success": True }
	
# TitleScene Login
@app.route("/login", methods=["POST"])
def login():
	# check POST body
	version = request.form.get("version", "0")
	userid = request.form.get("userid")
	password = request.form.get("password")
	lang = request.form.get("lang", "ko")
	if userid == None or version == None: return STRINGS["invalid"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	# obtain settings
	settings = db.hmget(f"server-settings", "minimum-version", "latest-version")
	
	# get user data
	if not db.exists("user:%s" % userid):
		if lang == "en":
			return { "success": False, "message": "Failed to load user account information.", "reset": True }
		else:
			return { "success": False, "message": "존재하지 않는 유저입니다.", "reset": True }
	
	# fetch user data
	user = db.hgetall("user:%s" % userid)
		
	# check password
	if password != user["password"]:
		if lang == "en":
			return { "success": False, "message": "Your login credentials have expired.\nPlease connect your account again.", "reset": True }
		else:
			return { "success": False, "message": "로그인 정보가 만료되었습니다.\n계정을 다시 연결해 주세요.", "reset": True }
	
	pipe = db.pipeline()
	
	# give daily login bonus
	t = int(time.time())
	dayDelta = datetime.datetime.fromtimestamp(t).toordinal() - datetime.datetime.fromtimestamp(int(user["lastlogin"])).toordinal()
	if dayDelta > 0:
		pipe.hset(f"user-inbox:{userid}", str(uuid.uuid4()), get_daily_login())
		user["totallogin"] = db.hincrby("user:%s" % userid, "totallogin")
		# check login events
		eventBonus = get_event_login()
		if eventBonus != None:
			pipe.hset(f"user-inbox:{userid}", str(uuid.uuid4()), eventBonus)
		if dayDelta == 1:
			# consecutive login
			consecutiveLogins = db.hincrby(f"user:{userid}", "consecutivelogin")
			pipe.hset(f"user-inbox:{userid}", str(uuid.uuid4()), get_consecutive_login(consecutiveLogins))
			if consecutiveLogins > int(db.hget(f"user:{userid}", "consecutiveloginrecord") or 0):
				pipe.hset(f"user:{userid}", "consecutiveloginrecord", consecutiveLogins)
				user["consecutiveloginrecord"] = consecutiveLogins
		else:
			# consecutive login reset
			pipe.hset(f"user:{userid}", "consecutivelogin", 1)
			pipe.hset(f"user-inbox:{userid}", str(uuid.uuid4()), get_consecutive_login(1))
			user["consecutiveloginrecord"] = 1
		
	# increment db records
	pipe.hset("user:%s" % userid, "lastlogin", t)
	user["lastlogin"] = str(t)
	
	pipe.execute()
	
	# play allowed
	return_dict = { "success": True, "user": user }
	if settings[1] > version:
		if lang == "en":
			return_dict["message"] = f"A new game version is available. ({settings[1]}) Please update the game."
		else:
			return_dict["message"] = f"새 버전이 있습니다. ({settings[1]}) 게임을 업데이트 해 주세요."
		return_dict["url"] = db.hget("server-settings", "drive-address")
	
	# BITFIELD
	# fetch card data
	cards = db_raw.get(f"user-cards:{userid}")
	return_dict["user"]["cards"] = db_raw.get(f"user-cards:{userid}").hex() if cards != None else ""
	#fetch song clear stars
	return_dict["user"]["songclearstar"] = db_raw.get(f"user-songclearstar:{userid}").hex()
	
	raw_scores = db_raw.get(f"user-scores:{userid}") or b""
	return_dict["user"]["scores"] = raw_scores.hex()
	
	# fetch inbox data
	return_dict["user"]["inbox"] = db.hgetall(f"user-inbox:{userid}")


	# fetch game data
	return_dict["gameData"] = module_main.getGameData()
	
	return return_dict

@app.route("/changename", methods=["POST"])
def changename():
	userid = request.form.get("userid")
	password = request.form.get("password")
	username = request.form.get("username")
	lang = request.form.get("lang", "ko")
	if userid == None or password == None or username == None: return STRINGS["invalid"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	if not db.exists("user:%s" % userid):
		if lang == "en":
			return { "success": False, "message": "Failed to load account information." }
		else:
			return { "success": False, "message": "존재하지 않는 유저입니다." }
	if password != db.hget(f"user:{userid}", "password"):
		if lang == "en":
			return { "success": False, "message": "Your login credentials have expired. Please refresh the game." }
		else:
			return { "success": False, "message": "로그인 정보가 잘못되어 닉네임 변경에 실패하였습니다. 다시 로그인 해 주세요." }
	username = unicodedata.normalize("NFC", username)	# normalize Korean
	if len(username) < 2 or len(username) > 12:		# filter out names too long/short
		if lang == "en":
			return { "success": False, "message": "Nickname must be between 2~12 characters." }
		else:
			return { "success": False, "message": "닉네임은 최소 2자, 최대 12자까지 입력 가능합니다." }

	existingName = db.hget(f"user:{userid}", "username")
	db.hset(f"user:{userid}", "username", username)
	logger.info(f"{userid} | 닉변: [{existingName}] -> [{username}]")

	returnDict = {
		"success": True,
		"update": {
			"username": username
		}
	}
	returnDict["worldRecord"] = {}
	for key, value in db.hgetall("world-records").items():
		userid = value.split(":")[0]
		score = value.split(":")[1]
		username = db.hget(f"user:{userid}", "username")
		returnDict["worldRecord"][key] = f"{username}:{score}"
	
	return returnDict
	

@app.route("/inbox_receive", methods=["POST"])
def inbox_receive():
	userid = request.form.get("userid")
	password = request.form.get("password")
	itemid = request.form.get("itemid")
	lang = request.form.get("lang", "ko")
	if userid == None or itemid == None: return STRINGS["invalid"][lang]
	if not Helper.verifyUser(userid, password): return STRINGS["authfailed"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	# get item data
	item = db.hget(f"user-inbox:{userid}", itemid)
	if item == None:
		if lang == "en":
			return { "success": False, "message": "This item has already been received." }
		else:
			return { "success": False, "message": "이미 수령하였거나 존재하지 않는 아이템입니다." }
	
	# handle item
	return_dict = { "success": True, "update": {} }
	itemInfo = json.loads(item)
	if itemInfo["type"] == "sscoin" or itemInfo["type"] == "diamond":
		if not str(itemInfo["value"]).isdigit():
			if lang == "en":
				return { "success": False, "message": "Item data is invalid.\nPlease contact game support." }
			else:
				return { "success": False, "message": "아이템 정보가 잘못되었습니다.\n관리자에게 문의 바랍니다." }
		amount = db.hincrby(f"user:{userid}", itemInfo["type"], itemInfo["value"])
		return_dict["update"][itemInfo["type"]] = amount
		return_dict["received"] = itemInfo["value"]
	elif itemInfo["type"] == "gacha":
		if "collectionList" not in itemInfo or "memberList" not in itemInfo or "levelChanceTable" not in itemInfo:
			if lang == "en":
				return { "success": False, "message": "Item data is invalid.\nPlease contact game support." }
			else:
				return { "success": False, "message": "아이템 정보가 잘못되었습니다.\n관리자에게 문의 바랍니다." }
		return_dict["received"] = module_cards.gachaCard(userid, itemInfo["collectionList"], itemInfo["memberList"], itemInfo["levelChanceTable"], int(itemInfo["value"]))
		return_dict["update"]["cards"] = db_raw.get(f"user-cards:{userid}").hex()
	else:
		if lang == "en":
			return { "success": False, "message": "Item data is invalid.\nPlease contact game support." }
		else:
			return { "success": False, "message": "아이템 정보가 잘못되었습니다.\n관리자에게 문의 바랍니다." }
	# remove item
	db.hdel(f"user-inbox:{userid}", itemid)
	return_dict["item"] = itemInfo
	return return_dict


@app.route("/inbox_receiveall_money", methods=["POST"])
def inbox_receiveall_money():
	userid = request.form.get("userid")
	password = request.form.get("password")
	lang = request.form.get("lang", "ko")
	if userid == None or password == None: return STRINGS["invalid"][lang]
	if not Helper.verifyUser(userid, password): return STRINGS["authfailed"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	received_diamond = 0
	received_sscoin = 0
	pipe = db.pipeline()
	# fetch
	items = db.hgetall(f"user-inbox:{userid}")
	for key, value in items.items():
		itemInfo = json.loads(value)
		if itemInfo["type"] == "sscoin":
			if not str(itemInfo["value"]).isdigit(): continue
			pipe.hincrby(f"user:{userid}", itemInfo["type"], itemInfo["value"])
			received_sscoin += int(itemInfo["value"])
		elif itemInfo["type"] == "diamond":
			if not str(itemInfo["value"]).isdigit(): continue
			pipe.hincrby(f"user:{userid}", itemInfo["type"], itemInfo["value"])
			received_diamond += int(itemInfo["value"])
		else:
			continue
		pipe.hdel(f"user-inbox:{userid}", key)
	pipe.execute()
	return { "success": True, "update": {"sscoin": db.hget(f"user:{userid}", "sscoin"), "diamond": db.hget(f"user:{userid}", "diamond"), "inbox": db.hgetall(f"user-inbox:{userid}")}, "received_sscoin": received_sscoin, "received_diamond": received_diamond }

@app.route("/inbox_receiveall_card", methods=["POST"])
def inbox_receiveall_card():
	userid = request.form.get("userid")
	password = request.form.get("password")
	lang = request.form.get("lang", "ko")
	if userid == None or password == None: return STRINGS["invalid"][lang]
	if not Helper.verifyUser(userid, password): return STRINGS["authfailed"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	received_card = []
	# fetch
	items = db.hgetall(f"user-inbox:{userid}")
	for key, value in items.items():
		itemInfo = json.loads(value)
		if itemInfo["type"] == "gacha":
			if "collectionList" not in itemInfo or "memberList" not in itemInfo or "levelChanceTable" not in itemInfo:
				continue
			
			# limit up to 10 cards at once
			amount = int(itemInfo["value"])
			if len(received_card) + amount > 10:
				break
			
			received_card += module_cards.gachaCard(userid, itemInfo["collectionList"], itemInfo["memberList"], itemInfo["levelChanceTable"], int(itemInfo["value"]))
		else:
			continue
		db.hdel(f"user-inbox:{userid}", key)
	return { "success": True, "update": {"cards": db_raw.get(f"user-cards:{userid}").hex(), "inbox": db.hgetall(f"user-inbox:{userid}")}, "received": received_card }
	

@app.route("/setprofilecard", methods=["POST"])
def rhythmiz_setprofilecard():
	userid = request.form.get("userid")
	password = request.form.get("password")
	cardindex = request.form.get("cardindex")
	lang = request.form.get("lang", "ko")
	if userid == None or password == None or cardindex == None: return STRINGS["invalid"][lang]
	
	# check play not allowed
	playAllowed = Helper.playNotAllowed(userid)
	if playAllowed != None: return { "success": False, "message": playAllowed }
	
	if not db.exists("user:%s" % userid):
		if lang == "en":
			return { "success": False, "message": "User account does not exist." }
		else:
			return { "success": False, "message": "존재하지 않는 유저입니다." }
	
	if not Helper.verifyUser(userid, password): return STRINGS["authfailed"][lang]
	
	# check card
	if Helper.bitfieldHexToInt(db_raw.get(f"user-cards:{userid}").hex(), 2, int(cardindex)) == 0:
		if lang == "en":
			return { "success": False, "message": "You cannot set a card you do not own as profile card." }
		else:
			return { "success": False, "message": "보유하지 않은 카드는 프로필카드로 설정할 수 없습니다." }
	
	# update db
	db.hset(f"user:{userid}", "profilecard", cardindex)
	return {
		"success": True,
		"update": {
			"profilecard": cardindex
		}
	}


@app.route("/getuserprofile", methods=["POST"])
def getuserprofile():
	privateid = request.form.get("userid")
	lang = request.form.get("lang")
	userid = db.hget("privateid-userid", privateid)
	if privateid == None: return STRINGS["invalid"][lang]
	if userid == None:
		if lang == "en":
			return { "success": False, "message": "Failed to load user information." }
		else:
			return { "success": False, "message": "유저 정보를 불러오는데 실패하였습니다." }
	top12 = db.hget(f"user:{userid}", "top12") or "0"
	
	return {
		"success": True,
		"username": db.hget(f"user:{userid}", "username"),
		"cards": db_raw.get(f"user-cards:{userid}").hex(),
		"profilecard": db.hget(f"user:{userid}", "profilecard"),
		"profilemusic": db.hget(f"user:{userid}", "profilemusic"),
		"level": int(db.hget(f"user:{userid}", "level")),
		"top12": int(top12)
	}


@app.route("/backup_settings", methods=["POST"])
def backup_settings():
	userid = request.form.get("userid")
	password = request.form.get("password")
	payload = request.form.get("payload")
	lang = request.form.get("lang", "ko")

	if not db.exists(f"user:{userid}"):
		if lang == "en":
			return { "success": False, "message": "User account does not exist." }
		else:
			return { "success": False, "message": "존재하지 않는 유저입니다." }
	
	if not Helper.verifyUser(userid, password): return STRINGS["authfailed"][lang]

	# set user save data
	db.hset(f"user-data:{userid}", "savedata_backup", payload)
	return { "success": True }

@app.route("/restore_settings", methods=["POST"])
def restore_settings():
	userid = request.form.get("userid")
	password = request.form.get("password")
	lang = request.form.get("lang", "ko")

	if not db.exists(f"user:{userid}"):
		if lang == "en":
			return { "success": False, "message": "User account does not exist." }
		else:
			return { "success": False, "message": "존재하지 않는 유저입니다." }
	
	if not Helper.verifyUser(userid, password): return STRINGS["authfailed"][lang]

	if not db.hexists(f"user-data:{userid}", "savedata_backup"):
		return {
			"success": False,
			"message": "You do not have any backup data. Please back up your data first." if lang == "en" else "백업 데이터가 없습니다. 설정을 먼저 백업해 주세요."
		}
	else:
		return {
			"success": True,
			"payload": db.hget(f"user-data:{userid}", "savedata_backup")
		}
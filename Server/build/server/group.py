import sys, os
if hasattr(sys.modules["__main__"], "__file__"):
	main_name = os.path.basename(sys.modules["__main__"].__file__)
	if main_name == "run.py":
		from __main__ import app, db
	else:
		from server import app, db
from main import logger
from flask import Flask, request
from flask_cors import cross_origin
import random
import json




# initialize firebase admin
import firebase_admin
from firebase_admin import credentials, auth
firebase_admin.initialize_app(credentials.Certificate("firebase-admin-credentials.json"))

# initialize cloud identity API
import googleapiclient.discovery
from googleapiclient.errors import HttpError
from google.oauth2 import service_account
SCOPES = ['https://www.googleapis.com/auth/cloud-identity.groups']
credentials = service_account.Credentials.from_service_account_file("group-account-manager-credentials.json", scopes=SCOPES)
api = googleapiclient.discovery.build("cloudidentity", "v1", credentials=credentials)



@app.route("/group/add", methods=["POST"])
@cross_origin(origins=["https://wiz-one.space", "https://iz-one.web.app"], methods=["POST"])
def add_email_group():
	token = request.form.get("token")
	lang = request.form.get("lang", "ko")
	if token == None:
		if lang == "en":
			return { "success": False, "message": "Failed to connect your Google account. Please try again." }
		else:
			return { "success": False, "message": "구글 계정 연결에 실패하였습니다. 다시 로그인해 주세요." }

	# verify email and token
	try:
		decoded_token = auth.verify_id_token(token)
	except Exception as e:
		return { "success": False, "message": str(e) }

	# Send group member add request
	req = api.groups().memberships().create(parent="groups/032hioqz1m24bp9", body={
		"preferredMemberKey": {
			"id": decoded_token["email"]
		},
		"roles": [{ "name": "MEMBER" }]
	})
	
	try:
		result = req.execute()
		if result["done"]:
			logger.info(f"그룹 신규추가: {decoded_token['email']}")
			return { "success": True, "url": db.hget("server-settings", "drive-address") }
		else:
			if lang == "en":
				return { "success": False, "message": "Failed to process your request. Please try again." }
			else:
				return { "success": False, "message": "요청을 처리하는데 실패하였습니다." }
	except HttpError as e:
		errorObj = json.loads(e.content)["error"]
		if errorObj["status"] == "ALREADY_EXISTS":
			# Already in
			return { "success": True, "already_exists": True, "url": db.hget("server-settings", "drive-address") }
		else:
			if lang == "en":
				return { "success": False, "message": "An error occurred. Please contact the server administrator." }
			else:
				return { "success": False, "message": "오류가 발생하였습니다. 관리자에게 문의 바랍니다." }


@app.route("/reset_password", methods=["POST"])
@cross_origin(origins=["https://wiz-one.space", "https://iz-one.web.app"], methods=["POST"])
def reset_password():
	token = request.form.get("token")
	lang = request.form.get("lang", "ko")
	if token == None:
		if lang == "en":
			return { "success": False, "message": "Failed to connect your Google account. Please try again." }
		else:
			return { "success": False, "message": "구글 계정 연결에 실패하였습니다. 다시 로그인해 주세요." }

	# verify email and token
	try:
		decoded_token = auth.verify_id_token(token)
	except Exception as e:
		return { "success": False, "message": str(e) }
	
	email = decoded_token["email"]
	
	if not db.exists("user:%s" % email):
		if lang == "en":
			return { "success": False, "message": "User account does not exist. Please open the game app and create a new account using this email. (%s)" % email }
		else:
			return { "success": False, "message": "조회된 사용자 계정이 없습니다. 게임을 열어서 이 이메일로 새 계정을 만들어주세요. (%s)" % email }

	# generate new password
	newpass = f"{random.randrange(0, 10000000000000000):016d}"
	
	# update DB
	db.hset(f"user:{email}", "password", newpass)
	logger.info(f"{email} | 연동코드리셋: [{newpass}]")
	return { "success": True, "password": newpass }


def email_in_group(email):
	if db.sismember("banned-emails", email):
		return False
	req = api.groups().memberships().lookup(parent="groups/032hioqz1m24bp9")
	req.uri += f"&memberKey.id={email}"
	try:
		result = req.execute()
		return True
	except HttpError as e:
		return False
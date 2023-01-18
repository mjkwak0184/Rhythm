import os
import redis
log_path = input("Enter path for log directory: ")

# fetch top rank list
db = redis.Redis("localhost", password="tfs-4ga-1qm-4Ex", decode_responses=True)


rank_emails = set()
for num in range(1, 105):
	if db.exists(f"scoreboard:{num}"):
		rank = db.zrange(f"scoreboard:{num}", 0, 4, desc=True, withscores=True)
		for entry in rank:
			rank_emails.add(entry[0].split(",")[1].split(":")[0])

print(f"Rank emails: {rank_emails}")
# obtained rank emails
			
entry_cards = []

# read all logs
for entry in os.scandir(log_path):
	if entry.is_dir(follow_symlinks=False): continue
	if entry.name.find("output") == -1: continue
	with open(entry.path, "r") as log:
		for line in log.readlines():
			if line.find("Cards:") == -1: continue
			email = line.replace(" ", "", -1).split("|")[0].split("-")[-1]
			if email in rank_emails:
				# rank email, record card
				entry_cards.append(line.replace(" ", "").replace("\n", "").split("Cards:")[1])


collection_count = [0 for x in range(70)]
print("Processing card data")
for i in entry_cards:
	for j in i.split(","):
		collection_count[int( int(j) / 12)] += 1

for i in range(len(collection_count)):
	print(f"Collection {i} : {collection_count[i]}")
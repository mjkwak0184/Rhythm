bpm = 125
base_time = 1.4
multiplier = 2
song_length = 90
beat_time = 120/bpm

with open("custom_notes.txt", "w") as f:
	while base_time + beat_time * multiplier <= song_length:
		f.writelines(f"s;6,{base_time + multiplier * beat_time:.3f}\n")
		multiplier += 1

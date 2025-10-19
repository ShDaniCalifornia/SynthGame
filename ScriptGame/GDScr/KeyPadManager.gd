extends Node3D

var index_to_key = {
	0: "1", 1: "1_5", 2: "2", 3: "2_5", 4: "3",
	5: "4", 6: "4_5", 7: "5", 8: "5_5", 9: "6",
	10: "6_5", 11: "7", 12: "8", 13: "8_5", 14: "9",
	15: "9_5", 16: "10", 17: "11", 18: "11_5", 19: "12",
	20: "12_5", 21: "13", 22: "13_5", 23: "14", 24: "15"
}

var pressed_keys = {}
signal key_pressed(key_name)
var octave_shift := 0

func get_visible_note_start() -> int:
	return 60 + (octave_shift * 12)

func press_midi_key(midi_note: int, velocity := 127):
	# Нота, с которой начинается клавиатура
	var start_note = get_visible_note_start() 
	var index = midi_note - start_note # Вычисление позиции клавиш
	# Получение конкретной клавиши в диапазоне
	if index >= 0 and index < 25:
		var key_name = index_to_key.get(index)
		call_deferred("_press_key", key_name, true)
	else: # Иначе - смещаем октаву
		if midi_note < start_note:
			change_octave(-1)
		elif midi_note > start_note + 24:
			change_octave(1)
# После смены октавы снова поиск клавиши
		start_note = get_visible_note_start()
		index = midi_note - start_note
		if index >= 0 and index < 25:
			var key_name = index_to_key.get(index)
			call_deferred("_press_key", key_name, true)

func release_midi_key(midi_note: int, velocity := 127):
	var start_note = get_visible_note_start()
	var index = midi_note - start_note
	
	if index >= 0 and index < 25:
		var key_name = index_to_key.get(index)
		call_deferred("_press_key", key_name, false)
	else:
		if midi_note < start_note:
			change_octave(-1)
		elif midi_note > start_note + 24:
			change_octave(1)

		start_note = get_visible_note_start()
		index = midi_note - start_note
		if index >= 0 and index < 25:
			var key_name = index_to_key.get(index)
			call_deferred("_press_key", key_name, false)

func _press_key(key_name: String, is_pressed: bool):
	var key = get_node_or_null(key_name)
	if key and key is Sprite3D:
		if pressed_keys.get(key_name, false) != is_pressed:
			pressed_keys[key_name] = is_pressed
			if is_pressed:
				emit_signal("key_pressed", key_name)

func change_octave(delta: int):
	octave_shift = clamp(octave_shift + delta, -3, 3)

func _process(_delta):
	for key_name in pressed_keys.keys():
		var key = get_node_or_null(key_name)
		if key:
			var is_pressed = pressed_keys[key_name]
			var target_rot_x = -PI / 30.0 if is_pressed else 0.0
			key.rotation.x = lerp(key.rotation.x, target_rot_x, 0.2)

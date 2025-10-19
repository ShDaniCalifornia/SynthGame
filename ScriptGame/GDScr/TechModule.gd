extends Node2D

@onready var Text: Label = $Context/TileMapLayer/Text
@onready var KeyPad: Node = get_node("/root/MainScene/Keys")

@onready var Context: Node2D = $Context
@onready var menu_window = $MenuContext
@onready var warning_window = $WarningContext
@onready var settings_window = $SettingsContext

@onready var finish_window = $FinishContext

# Настройка громкости
@onready var volume_slider = $SettingsContext/TileMapLayer/SoundVolume/VolumeSlider

# SoundFont
@onready var soundfont_selector = $SettingsContext/TileMapLayer/SoundFont/SoundFountSelector

# С#-скрипт настроек
@onready var settings_script = get_node("/root/MainScene/TutorialUI/SettingsContext/SettingsScript")

# Настройки
@onready var midi_selector = $SettingsContext/TileMapLayer/Input/MidiInputSelector
@onready var audio_selector = $SettingsContext/TileMapLayer/Output/AudioOutputSelector

# Блокировка
var input_locked := false

# --- Список инструментов и SoundFont-файлов ---
var instruments := [
	{ "name": "Acoustic Grand Piano", "program": 0 },
	{ "name": "Electric Piano 1", "program": 4 },
	{ "name": "Violin", "program": 40 },
	{ "name": "Flute", "program": 73 },
	{ "name": "Trumpet", "program": 56 }
]
var soundfonts := ["FluidR3_GM.sf2"] 
# ---

@onready var lesson_service: Node = get_node("/root/MainScene/TutorialUI/LessonService")
var lesson_data = {}

var stage := 0
var current_key_index := 0
var target_keys: Array = []
var is_gamemode := false

var menu_show := false
var warning_show := false
var context_show := false
var settings_show := false

func _ready():
	Context.modulate.a = 0.0
	Context.scale = Vector2(0.75, 0.75)
	Text.text = ""
	
	if get_tree().has_meta("selected_module_id"):
		var module_id = get_tree().get_meta("selected_module_id")
		lesson_data["module_id"] = module_id
		get_tree().set_meta("selected_module_id", null)
		lesson_service.LoadLesson(module_id, 1)
	
	# SoundFont
	settings_script.connect("SoundFontChanged", Callable(self, "_on_soundfont_selected"))
	soundfont_selector.item_selected.connect(func(index):
		_on_soundfont_selected(index))
	
	KeyPad.connect("key_pressed", Callable(self, "_on_key_pressed"))
	
	lesson_service.connect("LessonLoaded", Callable(self, "_on_lesson_loaded"))
	
	# Громкость
	volume_slider.value = VolumeManager.volume_db
	volume_slider.value_changed.connect(_on_volume_changed)
	
	# Подключение сигналов к изменению выбора
	midi_selector.item_selected.connect(_on_midi_selected)
	audio_selector.item_selected.connect(_on_audio_selected)
	_populate_device_selectors()
	
	_populate_soundfonts()
	
	# --- сохранение выбранного SoundFont ---
	var config = ConfigFile.new()
	if config.load("user://settings.cfg") == OK:
		if config.has_section_key("soundfont", "selected"):
			var selected = config.get_value("soundfont", "selected")
			var idx = find_item_index_by_text(soundfont_selector, selected)
			if idx >= 0:
				soundfont_selector.select(idx)
				call_deferred("_on_soundfont_selected", idx)
	if config.load("user://settings.cfg") == OK:
		var audio_index = config.get_value("audio", "device", 0)
		if audio_index >= 0 and audio_index < audio_selector.get_item_count():
			audio_selector.select(audio_index)
		else:
			audio_selector.select(0)
		var midi_index = config.get_value("midi", "device", 0)
		if midi_index >= 0 and midi_index < midi_selector.get_item_count():
			midi_selector.select(midi_index)
		else:
			midi_selector.select(0)

# Уровень громкости
func _on_volume_changed(value: float):
	VolumeManager.set_volume_db(value)

# --- Логика этапов ---
func _on_lesson_loaded(data: Dictionary):
	lesson_data = data
	stage = 0
	_next_stage()

func _next_stage():
	stage += 1
	if "steps" in lesson_data and stage in lesson_data["steps"]:
		var step = lesson_data["steps"][stage]

		_clear_targets()

		var step_type = step.get("type", "").to_lower()
		if step_type == "задание":
			var keys = step.get("keys", [])
			if keys.size() > 0:
				_set_targets(keys)
				
		_show_text(step.get("text", ""))
		context_show = true
		Context.modulate.a = 0.0
	else:
		context_show = false
		finish_window.visible = true

func _complete_module_exp():
	var user_id = get_current_user_id()
	var module_id = lesson_data.get("module_id", 0)
	var result = lesson_service.CompleteModule(user_id, module_id)
	if result == null:
		return
	if result.get("success", false):
		lesson_data["new_exp"] = result.get("new_exp", 0)
		lesson_data["new_level"] = result.get("new_level", 0)
		lesson_data["total_exp"]  = result.get("total_exp", 0)

func get_current_user_id() -> int:
	return UserSession.user_id

func _process(delta: float):
	warning_window.visible = warning_show
	menu_window.visible = menu_show
	settings_window.visible = settings_show
	
	input_locked = warning_show or menu_show or settings_show or finish_window.visible
	
	if context_show:
		Context.modulate.a = min(Context.modulate.a + delta, 1.0)
	var target_scale := 0.75 + (0.25 if context_show else 0.0)
	Context.scale = Context.scale.lerp(Vector2(target_scale, target_scale), delta * 20.0)

# Загрузка модулей при выборе
func load_lesson_from_module(module_id: int) -> void:
	stage = 0
	lesson_data.clear()
	lesson_service.LoadLesson(module_id, 1)

func _show_text(text: String):
	Text.text = text

func _highlight_keys(keys: Array):
	for k in keys:
		var key_node = get_node_or_null("/root/MainScene/Keys/" + k)
		if key_node:
			key_node.modulate = Color(1.0, 0.5, 0.5, 1.0)

func _clear_highlights():
	for child in KeyPad.get_children():
		if child is Sprite3D:
			child.modulate = Color(1, 1, 1, 1)

func _set_targets(keys: Array, gamemode: bool = false):
	target_keys = keys
	current_key_index = 0
	is_gamemode = gamemode
	_highlight_keys(keys)

func _clear_targets():
	_clear_highlights()
	target_keys.clear()
	current_key_index = 0
	is_gamemode = false

func _advance_to(next_stage: int):
	stage = next_stage - 1
	_next_stage()

func _on_key_pressed(key_name: String):
	if input_locked:
		return
	if "steps" in lesson_data and stage in lesson_data["steps"]:
		var step = lesson_data["steps"][stage]
		if step.get("type", "").to_lower() == "задание":
			var expected_keys = step.get("keys", [])
			if expected_keys.size() == 0:
				return

			if expected_keys.size() == 1:
				# Одинарный ответ
				if key_name == expected_keys[0]:
					_clear_targets()
					_next_stage()
			else:
				# Последовательность (гамма)
				if key_name == expected_keys[current_key_index]:
					current_key_index += 1
					if current_key_index >= expected_keys.size():
						_clear_targets()
						_next_stage()

func _input(event):
	if menu_show or warning_show or settings_show or finish_window.visible:
		return
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		if "steps" in lesson_data and stage in lesson_data["steps"]:
			var step = lesson_data["steps"][stage]
			if step.get("type", "").to_lower() == "теория":
				_next_stage()
				return
	
	if input_locked:
		return

	if Input.is_action_just_pressed("Back"):
		menu_show = true
		return

# WarningWindow
func _on_back_to_menu() -> void:
	get_tree().change_scene_to_file("res://Scene/StartMenu.tscn")

func _close_warning_button_pressed() -> void:
	warning_show = false
	menu_show = true

func _finish_button_pressed() -> void:
	_complete_module_exp()
	var module_id = lesson_data.get("module_id", 0)
	var exp_data = {
		"exp": lesson_data.get("total_exp", 0),
		"level": lesson_data.get("new_level", 0)
	}
	get_tree().set_meta("profile_update", exp_data)
	var config = ConfigFile.new()
	var path = "user://progress_%s.cfg" % str(UserSession.user_id)
	config.load(path)
	config.set_value("modules", str(module_id), true)
	config.save(path)
	get_tree().change_scene_to_file("res://Scene/StartMenu.tscn")

# SettingsWindow
func _close_settings_button_pressed() -> void:
	settings_show = false
	menu_show = true

# MenuWindow
func _go_back_button_pressed() -> void:
	menu_show = false

func _on_Settings_pressed() -> void:
	menu_show = false
	settings_show = true

func _open_warning_button_pressed() -> void:
	menu_show = false
	warning_show = true

# --- Методы для работы с аудио и MIDI ---
func _populate_device_selectors():
	midi_selector.clear()
	audio_selector.clear()

	var midi_devices = settings_script.GetMidiDevices()
	var audio_devices = settings_script.GetAudioDevices()

	for name in midi_devices:
		midi_selector.add_item(str(name))

	for name in audio_devices:
		audio_selector.add_item(str(name))

	if midi_selector.get_item_count() == 0:
		midi_selector.add_item("Нет MIDI устройств")
	if audio_selector.get_item_count() == 0:
		audio_selector.add_item("Нет аудиоустройств")

# --- Автосохранение настроек ---
func _on_midi_selected(index: int):
	var config = ConfigFile.new()
	if config.load("user://settings.cfg") != OK:
		config = ConfigFile.new()

	config.set_value("midi", "device", index)
	config.save("user://settings.cfg")

	settings_script.ApplySettings(index, audio_selector.get_selected_id())

func _on_audio_selected(index: int):
	var config = ConfigFile.new()
	if config.load("user://settings.cfg") != OK:
		config = ConfigFile.new()

	config.set_value("audio", "device", index)
	config.save("user://settings.cfg")

	settings_script.ApplySettings(midi_selector.get_selected_id(), index)

# --- SoundFont ---
signal SoundFontChanged(font_name)

func _on_soundfont_selected(index: int):
	var meta = soundfont_selector.get_item_metadata(index)
	if meta == null or not meta.has("sf") or not meta.has("program"):
		return

	var sf_path = meta["sf"]
	var program = meta["program"]

	var audio_manager = settings_script.get("audioManager")
	if audio_manager == null:
		return

	audio_manager.call("LoadSoundFont", sf_path)
	audio_manager.call("SetInstrument", program)

	var config = ConfigFile.new()
	var err = config.load("user://settings.cfg")
	if err != OK:
		print("Can't load config for saving soundfont, creating new")
	var selected_text = soundfont_selector.get_item_text(index)
	config.set_value("soundfont", "selected", selected_text)
	config.set_value("soundfont", "sf_path", sf_path)
	config.set_value("soundfont", "program", program)
	config.save("user://settings.cfg")

func find_item_index_by_text(option_button: OptionButton, text: String) -> int:
	for i in range(option_button.item_count):
		if option_button.get_item_text(i) == text:
			return i
	return -1

func _populate_soundfonts():
	soundfont_selector.clear()
	for sf in soundfonts:
		for inst in instruments:
			var display_name = "%s - %s" % [sf, inst["name"]]
			var meta = { "sf": sf, "program": inst["program"] }
			soundfont_selector.add_item(display_name)
			soundfont_selector.set_item_metadata(soundfont_selector.item_count - 1, meta)
	soundfont_selector.item_selected.connect(func(index):
		_on_soundfont_selected(index)
)
	soundfont_selector.disabled = soundfont_selector.item_count == 0

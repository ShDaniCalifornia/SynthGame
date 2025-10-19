extends Node2D

@onready var KeyPad: Node = get_node("/root/MainScene/Keys")

@onready var menu_window = $MenuContext
@onready var warning_window = $WarningContext
@onready var settings_window = $SettingsContext

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
var menu_show := false
var warning_show := false
var settings_show := false

func _ready():
	
	# SoundFont
	settings_script.connect("SoundFontChanged", Callable(self, "_on_soundfont_selected"))
	soundfont_selector.item_selected.connect(func(index):
		_on_soundfont_selected(index))
	
	KeyPad.connect("key_pressed", Callable(self, "_on_key_pressed"))
		
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


func _process(delta: float):
	warning_window.visible = warning_show
	menu_window.visible = menu_show
	settings_window.visible = settings_show

	if Input.is_action_just_pressed("Back"):
		menu_show = true
		return

# WarningWindow
func _on_back_to_menu() -> void:
	get_tree().change_scene_to_file("res://Scene/StartMenu.tscn")

func _close_warning_button_pressed() -> void:
	warning_show = false
	menu_show = true

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

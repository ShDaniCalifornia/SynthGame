extends Control

# Получаем объект (узел) контекстного меню
@onready var auth_window = $Auth
@onready var settings_window = $SettingsContext
@onready var profile_window = $ProfileContext
@onready var modules_window = $ModuleContext

# Профиль
@onready var profile_nickname_label = $ProfileContext/TileMapLayer/Username
@onready var profile_level_label = $ProfileContext/TileMapLayer/Level/Level
@onready var profile_id_label = $ProfileContext/TileMapLayer/ID/IDnum
@onready var exp_bar = $ProfileContext/TileMapLayer/ProgressBar
@onready var profile_exp_label = $ProfileContext/TileMapLayer/Exp
var current_exp = 0
var current_level = 1

# С#-скрипт авторизации / регистрации
@onready var auth = $Auth

# Настройки
@onready var midi_selector = $SettingsContext/TileMapLayer/Input/MidiInputSelector
@onready var audio_selector = $SettingsContext/TileMapLayer/Output/AudioOutputSelector

# SoundFont
@onready var soundfont_selector = $SettingsContext/TileMapLayer/SoundFont/SoundFountSelector

# С#-скрипт настроек
@onready var settings_script = $SettingsContext/SettingsScript
# Настройка громкости
@onready var volume_slider = $SettingsContext/TileMapLayer/SoundVolume/VolumeSlider
@onready var menu_music_player = $MenuMusicPlayer

# Прокрутка модулей
@onready var scroll_content = $ModuleContext/ScrollWindow/ScrollContent
@onready var v_slider = $ModuleContext/VSlider

@onready var DARK : ColorRect = get_node("Dark/Dark")

var settings_show: bool = false
var profile_show: bool = false
var modules_show: bool = false

var dark_visible : bool = true

var animation_speed: float = 20.0

# Загрузка данных после авторизации
func _on_login_success(user_id: int, username: String, level: int, exp: int) -> void:
	profile_nickname_label.text = username
	profile_id_label.text = str(user_id)
	current_exp = exp
	current_level = level
	profile_level_label.text = str(level)
	update_exp_bar()

	UserSession.user_id = user_id

	# Загрузка данных после регистрации
func _on_register_success(user_id: int, username: String, level: int, exp: int) -> void:
	profile_nickname_label.text = username
	profile_id_label.text = str(user_id)
	current_exp = exp
	current_level = level
	profile_level_label.text = str(level)
	update_exp_bar()
	UserSession.user_id = user_id

func _ready():
	auth.connect("login_success", Callable(self, "_on_login_success"))
	auth.connect("register_success", Callable(self, "_on_register_success"))
	if UserSession.is_logged_in:
		auth_window.visible = false
		profile_nickname_label.text = UserSession.username
		profile_id_label.text = str(UserSession.user_id)
		current_exp = UserSession.exp
		current_level = UserSession.level
		profile_level_label.text = str(current_level)
		update_exp_bar()
	else:
		auth_window.visible = true
	
	# Заполняем списки устройств
	_populate_device_selectors()
	
	_populate_soundfonts()
	
	# SoundFont
	settings_script.connect("SoundFontChanged", Callable(self, "_on_soundfont_selected"))
	
	# AudioManager
	settings_script.set("audioManager", $SettingsContext/AudioManager)
	settings_script.get("audioManager").call("PlayMenuMusic", "res://Resources/MusicBackground.mp3")
	
	if get_tree().has_meta("selected_module_id"):
		var module_id = get_tree().get_meta("selected_module_id")
		get_tree().set_meta("selected_module_id", null)
		LessonService.LoadLesson(module_id, 1)
	
	if get_tree().has_meta("profile_update"):
		var data = get_tree().get_meta("profile_update")
		get_tree().set_meta("profile_update", null)
		update_ui(data["exp"], data["level"])
	
	_show_completed_modules()
	
	# Громкость
	volume_slider.value = VolumeManager.volume_db
	volume_slider.value_changed.connect(_on_volume_changed)

	# Подключение сигналов к изменению выбора
	midi_selector.item_selected.connect(_on_midi_selected)
	audio_selector.item_selected.connect(_on_audio_selected)
	
	menu_music_player.volume_db = VolumeManager.volume_db
	
	# Прокрутка модулей
	v_slider.min_value = 0
	v_slider.max_value = 900
	v_slider.page = 550  # высота окна
	v_slider.value_changed.connect(_on_slider_changed)
	$ModuleContext/ScrollWindow.gui_input.connect(_on_scroll_area_input)

func _process(delta: float):
	if dark_visible and DARK.color.a < 1.0: DARK.color.a += delta / 2.0
	elif !dark_visible and DARK.color.a > 0.0: DARK.color.a -= delta / 2.0
	
	if DARK.color.a > 0.0: DARK.visible = true
	else: DARK.visible = false
	
	modules_window.modulate.a += ((1 * int(modules_show)) - modules_window.modulate.a) * delta * animation_speed
	modules_window.scale += Vector2(((0.75 +  0.25 * int(modules_show)) - modules_window.scale.x) * delta * animation_speed, ((0.75 + 0.25 * int(modules_show)) - modules_window.scale.y) * delta * animation_speed)
	
	settings_window.modulate.a += ((1 * int(settings_show)) - settings_window.modulate.a) * delta * animation_speed
	settings_window.scale += Vector2(((0.75 +  0.25 * int(settings_show)) - settings_window.scale.x) * delta * animation_speed, ((0.75 + 0.25 * int(settings_show)) - settings_window.scale.y) * delta * animation_speed)
	
	profile_window.modulate.a += ((1 * int(profile_show)) - profile_window.modulate.a) * delta * animation_speed
	profile_window.scale += Vector2(((0.75 +  0.25 * int(profile_show)) - profile_window.scale.x) * delta * animation_speed, ((0.75 + 0.25 * int(profile_show)) - profile_window.scale.y) * delta * animation_speed)

func _physics_process(delta: float):
	get_node("ModuleContext/ScrollWindow/ScrollContent/Module1").visible = modules_show
	get_node("ModuleContext/ScrollWindow/ScrollContent/Module2").visible = modules_show
	get_node("ModuleContext/ScrollWindow/ScrollContent/Module3").visible = modules_show
	get_node("SettingsContext/TileMapLayer/Input/MidiInputSelector").disabled = !settings_show
	get_node("SettingsContext/TileMapLayer/Output/AudioOutputSelector").disabled = !settings_show
	modules_window.visible = !auth_window.visible

func _input(event: InputEvent):
	if Input.is_action_just_pressed("Back"):
		if modules_window.modulate.a > 0.0: modules_show = false
		if settings_window.modulate.a > 0.5: settings_show = false
		if profile_window.modulate.a > 0.0: profile_show = false

# Опыт
func get_current_user_id() -> int:
	return profile_id_label.text.to_int()

func _complete_module_exp():
	var user_id = get_current_user_id()
	var module_id = get_tree().get_meta("selected_module_id", 0)
	var result = LessonService.CompleteModule(user_id, module_id)
	if result.get("success", false):
		var total_exp = result.get("total_exp", 0)
		var new_level = result.get("new_level", 1)
		var config = ConfigFile.new()
		var path = "user://progress_%s.cfg" % str(user_id)
		var err = config.load(path)
		if err != OK:
			config = ConfigFile.new()
		config.set_value("modules", str(module_id), true)
		config.save(path)
		update_ui(total_exp, new_level)

func _exp_for_next_level(level: int) -> int:
	return level * 100

func calculate_level_from_total_exp(total_exp: int) -> Dictionary:
	var level = 1
	var exp_left = total_exp
	while exp_left >= _exp_for_next_level(level):
		exp_left -= _exp_for_next_level(level)
		level += 1
	return {"level": level, "exp": exp_left}

func update_ui(total_exp: int, new_level: int) -> void:
	if total_exp < 0:
		return
	var result = calculate_level_from_total_exp(total_exp)
	current_exp = result["exp"]
	current_level = result["level"]
	profile_exp_label.text = str(current_exp)
	profile_level_label.text = str(current_level)
	update_exp_bar()
	UserSession.exp = total_exp 
	UserSession.level = current_level
	_show_completed_modules()

func update_exp_bar():
	var exp_for_next = _exp_for_next_level(current_level)
	var ratio = float(current_exp) / float(exp_for_next)
	var needed = _exp_for_next_level(current_level)
	exp_bar.value = current_exp / float(needed) * exp_bar.max_value
	profile_exp_label.text = str(current_exp) + " / " + str(needed) + " XP"

func _show_completed_modules():
	var user_id = get_current_user_id()
	var completed = get_completed_modules(user_id)
	for i in range(1, 4):
		var module_node = scroll_content.get_node_or_null("Module%d" % i)
		if module_node:
			var icon = module_node.get_node_or_null("CompletedIcon")
			if icon:
				var module_id = str(i)
				icon.text = "Пройдено" if completed.has(module_id) else ""

func get_completed_modules(user_id: int) -> Dictionary:
	var config = ConfigFile.new()
	var path = "user://progress_%s.cfg" % str(user_id)
	var err = config.load(path)
	if err != OK:
		return {}
	var result = {}
	if config.has_section("modules"):
		var keys = config.get_section_keys("modules")
		for key in keys:
			var value = config.get_value("modules", key)
			if value == true:
				result[key] = true
	return result

# Уровень громкости
func _on_volume_changed(value: float):
	if settings_window.modulate.a > 0.0: VolumeManager.set_volume_db(value)

func _exit_button_pressed() -> void:
	if !modules_show and !profile_show and !settings_show and !auth_window.visible: get_tree().quit()

# --- Обучение ---
func _modules_button_pressed() -> void:
	if !modules_show and !profile_show and !settings_show and !auth_window.visible: modules_show = true

# --- Профиль ---
func _profile_button_pressed() -> void:
	if !modules_show and !profile_show and !settings_show and !auth_window.visible: profile_show = true

# --- Настройки ---
func _settings_button_pressed() -> void:
	if !modules_show and !profile_show and !settings_show and !auth_window.visible: settings_show = true

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

	settings_script.set("audioManager", get_node("SettingsContext/AudioManager"))

func _on_audio_selected(index: int):
	var config = ConfigFile.new()
	if config.load("user://settings.cfg") != OK:
		config = ConfigFile.new()

	config.set_value("audio", "device", index)
	config.save("user://settings.cfg")

	settings_script.call("ApplySettings", -1, index) # -1 если не меняем MIDI
	settings_script.get("audioManager").call("SelectOutputDevice", index)

# --- SoundFont ---
func _on_soundfont_selected(font_path: String) -> void:
	print("Выбран SoundFont: ", font_path)

	var audio_manager = settings_script.get("audioManager")
	if audio_manager:
		audio_manager.call("LoadSoundFont", font_path)

func _populate_soundfonts():
	soundfont_selector.clear()

	var soundfonts = settings_script.GetSoundFonts()
	for sf in soundfonts:
		soundfont_selector.add_item(str(sf))

	if soundfont_selector.get_item_count() == 0:
		soundfont_selector.add_item("Нет файлов .sf2")
		soundfont_selector.disabled = true
	else:
		soundfont_selector.disabled = false

# --- Выбор модулей ---
func _1_module_pressed() -> void:
	if modules_show: _change_to_tutorial_scene_with_module(1)

func _2_module_pressed() -> void:
	if modules_show: _change_to_tutorial_scene_with_module(2)

func _3_module_pressed() -> void:
	if modules_show: _change_to_tutorial_scene_with_module(3)

func _change_to_tutorial_scene_with_module(module_id: int) -> void:
	LessonService.LoadLesson(module_id, 1)
	get_tree().set_meta("selected_module_id", module_id)
	get_tree().change_scene_to_file("res://Scene/TutorialScene.tscn")

func _on_slider_changed(value):
	scroll_content.position.y = -value

func _on_scroll_area_input(event):
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			v_slider.value -= 50
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			v_slider.value += 50

func _on_dark_timer_timeout() -> void:
	dark_visible = false

func _freeplay_button_pressed() -> void:
	if !modules_show and !profile_show and !settings_show and !auth_window.visible: get_tree().change_scene_to_file("res://Scene/FreeModeScene.tscn")

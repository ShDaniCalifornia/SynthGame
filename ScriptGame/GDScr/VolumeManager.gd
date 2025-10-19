extends Node

const CONFIG_PATH = "user://settings.cfg"
var volume_db: float = 0.0

func _ready():
	load_volume()

func set_volume_db(value: float):
	volume_db = value
	AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Master"), value)
	save_volume()

func save_volume():
	var config = ConfigFile.new()
	if config.load(CONFIG_PATH) != OK:
		config = ConfigFile.new()

	config.set_value("audio", "volume_db", volume_db)
	config.save(CONFIG_PATH)

func load_volume():
	var config = ConfigFile.new()
	if config.load(CONFIG_PATH) == OK and config.has_section_key("audio", "volume_db"):
		volume_db = config.get_value("audio", "volume_db")
		AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Master"), volume_db)

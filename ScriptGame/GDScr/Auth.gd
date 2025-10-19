extends Node2D

signal login_success(user_id: int, username: String, level: int, exp: int)
signal register_success(user_id: int, username: String, level: int, exp: int)

# Регистрация
@onready var reg_username_field = $RegContext/TileMapLayer/Username/Username
@onready var reg_login_field = $RegContext/TileMapLayer/Login/Login
@onready var reg_password_field = $RegContext/TileMapLayer/Password/Password
@onready var reg_repeat_password_field = $RegContext/TileMapLayer/RepPassword/RepPassword
@onready var register_button = $RegContext/TileMapLayer/RegBtn/TouchScreenButton
@onready var reg_password_eye_button = $RegContext/TileMapLayer/Password/EyeButton
@onready var reg_repeat_password_eye_button = $RegContext/TileMapLayer/RepPassword/EyeButton

# Авторизация
@onready var login_login_field = $LogContext/TileMapLayer/Login/Login
@onready var login_password_field = $LogContext/TileMapLayer/Password/Password
@onready var login_button = $LogContext/TileMapLayer/LogBtn/TouchScreenButton
@onready var login_password_eye_button = $LogContext/TileMapLayer/Password/EyeButton

# Общее
@onready var auth_service = $AuthService
@onready var notificationReg_label = $RegContext/NotificationLabel
@onready var notificationLog_label = $LogContext/NotificationLabel
@onready var registration_window = $RegContext
@onready var login_window = $LogContext

# Иконки глазиков
var icon_eye := preload("res://Resources/icon/icons8-invisible-50.png")
var icon_eye_closed := preload("res://Resources/icon/icons8-closed-eye-50.png")

var reg_password_visible := false
var reg_repeat_password_visible := false
var login_password_visible := false

# --- Окна
var registration_show: bool = true
var login_show: bool = false

func _ready():
	auth_service.connect("LoginResult", Callable(self, "_on_login_result"))
	auth_service.connect("RegisterResult", Callable(self, "_on_register_result"))
	notificationReg_label.text = ""
	
	reg_password_eye_button.connect("pressed", Callable(self, "_on_reg_password_eye_pressed"))
	reg_repeat_password_eye_button.connect("pressed", Callable(self, "_on_reg_repeat_password_eye_pressed"))
	login_password_eye_button.connect("pressed", Callable(self, "_on_login_password_eye_pressed"))

	reg_password_eye_button.texture_normal = icon_eye_closed
	reg_repeat_password_eye_button.texture_normal = icon_eye_closed
	login_password_eye_button.texture_normal = icon_eye_closed
	
	_update_eye_state()

func _physics_process(_delta):
	registration_window.visible = registration_show
	login_window.visible = login_show

func _on_register_pressed():
	var username = reg_username_field.text.strip_edges()
	var login = reg_login_field.text.strip_edges()
	var password = reg_password_field.text.strip_edges()
	var repeat_password = reg_repeat_password_field.text.strip_edges()

	if login.is_empty() or password.is_empty() or repeat_password.is_empty() or username.is_empty():
		notificationReg_label.text = "Заполните все поля для регистрации."
		return

	if password != repeat_password:
		notificationReg_label.text = "Пароли не совпадают."
		return

	auth_service.RegisterUser(username, login, password)

func _on_register_result(success: bool, user_id := 0, username := "", level := 0, exp := 0) -> void:
	if success:
		emit_signal("register_success", user_id, username, level, exp)
		notificationReg_label.text = "Регистрация успешна! Теперь можете войти."
		hide()
	else:
		notificationReg_label.text = "Пользователь с таким логином уже существует."

func _on_login_pressed():
	var login = login_login_field.text.strip_edges()
	var password = login_password_field.text.strip_edges()

	if login.is_empty() or password.is_empty():
		notificationLog_label.text = "Введите логин и пароль для входа."
		return

	auth_service.TryLogin(login, password)

func _on_login_result(success: bool, user_id := 0, username := "", level := 0, exp := 0) -> void:
	if success:
		UserSession.user_id = user_id
		UserSession.username = username
		UserSession.level = level
		UserSession.exp = exp
		UserSession.is_logged_in = true
		emit_signal("login_success", user_id, username, level, exp)
		notificationLog_label.text = "Вход выполнен!"
		hide()
	else:
		notificationLog_label.text = "Неверный логин или пароль"

func _on_go_to_login_pressed():
	registration_show = false
	login_show = true
	notificationLog_label.text = ""

func _on_go_to_register_pressed():
	login_show = false
	registration_show = true
	notificationReg_label.text = ""

func _update_eye_state():
	reg_password_field.secret = not reg_password_visible
	reg_password_eye_button.texture_normal = icon_eye if reg_password_visible else icon_eye_closed

	reg_repeat_password_field.secret = not reg_repeat_password_visible
	reg_repeat_password_eye_button.texture_normal = icon_eye if reg_repeat_password_visible else icon_eye_closed

	login_password_field.secret = not login_password_visible
	login_password_eye_button.texture_normal = icon_eye if login_password_visible else icon_eye_closed

func _on_reg_password_eye_pressed():
	reg_password_visible = !reg_password_visible
	_update_eye_state()

func _on_reg_repeat_password_eye_pressed():
	reg_repeat_password_visible = !reg_repeat_password_visible
	_update_eye_state()

func _on_login_password_eye_pressed():
	login_password_visible = !login_password_visible
	_update_eye_state()

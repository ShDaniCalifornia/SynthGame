extends Label

@export var hover_color: Color = Color.TURQUOISE
@export var normal_color: Color = Color.WHITE

func _ready():
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)
	modulate = normal_color

func _on_mouse_entered():
	modulate = hover_color

func _on_mouse_exited():
	modulate = normal_color

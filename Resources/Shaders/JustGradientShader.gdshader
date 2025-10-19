shader_type canvas_item;

void fragment() {
	vec4 new_color = COLOR;
	COLOR = texture(TEXTURE, mod(UV, 1.0)) + COLOR.b;
}

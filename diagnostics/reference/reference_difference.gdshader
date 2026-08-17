shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear, repeat_disable;
uniform sampler2D reference_texture : source_color, filter_linear, repeat_disable;

void fragment() {
    vec3 live_color = texture(screen_texture, SCREEN_UV).rgb;
    vec3 reference_color = texture(reference_texture, UV).rgb;
    COLOR = vec4(abs(live_color - reference_color), 1.0);
}

shader_type canvas_item;

uniform vec4 color_top : source_color = vec4(1.0, 1.0, 0.0, 1.0);
uniform vec4 color_bottom : source_color = vec4(0.0, 1.0, 1.0, 1.0);
uniform float label_top = 0.0;
uniform float label_height = 100.0;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    float t = clamp((VERTEX.y - label_top) / label_height, 0.0, 1.0);
    vec4 grad = mix(color_top, color_bottom, t);
    COLOR = vec4(tex.rgb * grad.rgb, tex.a * COLOR.a);
}
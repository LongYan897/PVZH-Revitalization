shader_type canvas_item;

uniform float saturation : hint_range(0.0, 1.0) = 0.05;

void fragment() {
    vec3 c = COLOR.rgb;
    float gray = dot(c, vec3(0.299, 0.587, 0.114));
    COLOR.rgb = mix(vec3(gray), c, saturation);
}
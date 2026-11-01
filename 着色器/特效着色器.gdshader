shader_type canvas_item;

uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float tint_intensity : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec3 base_rgb = COLOR.rgb;
    float base_a = COLOR.a;
    
    vec3 final_rgb = mix(base_rgb, tint_color.rgb, tint_intensity);
    
    COLOR = vec4(final_rgb, base_a);
}
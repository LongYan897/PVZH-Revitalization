shader_type canvas_item;

uniform float hue_shift : hint_range(0.0, 1.0) = 0.0;
uniform float contrast : hint_range(0.0, 2.0) = 0.0;
uniform float brightness : hint_range(0.0, 2.0) = 1.0;

vec3 rgb_to_hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv_to_rgb(vec3 c) {
    vec3 p = abs(fract(c.xxx + vec3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
    return c.z * mix(vec3(1.0), clamp(p - 1.0, 0.0, 1.0), c.y);
}

float luma(vec3 c) {
    return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    vec3 rgb = tex_color.rgb;

    vec3 hsv = rgb_to_hsv(rgb);
    hsv.x = mod(hsv.x + hue_shift, 1.0);
    rgb = hsv_to_rgb(hsv);

    rgb *= brightness;

    float L = luma(rgb);
    float L_new = mix(L, smoothstep(0.0, 1.0, L), contrast);
    L_new = clamp(L_new, 0.0, 1.0);

    float scale = (L > 1e-5) ? (L_new / L) : 1.0;
    rgb = rgb * scale;

    rgb = clamp(rgb, 0.0, 1.0);

    COLOR = vec4(rgb, tex_color.a);
}
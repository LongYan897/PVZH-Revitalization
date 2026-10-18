shader_type canvas_item;

uniform float hue_shift : hint_range(0.0, 1.0) = 0.0;

uniform float luminance_threshold : hint_range(0.0, 1.0) = 0.5;

uniform float softness : hint_range(0.0, 1.0) = 0.1;

vec3 rgb2hsv(vec3 c)
{
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void fragment()
{
    vec4 tex = texture(TEXTURE, UV);
    
    float lum = dot(tex.rgb, vec3(0.299, 0.587, 0.114));
    
    float weight = 1.0 - smoothstep(
        luminance_threshold,
        luminance_threshold + softness,
        lum
    );
    
    vec3 hsv = rgb2hsv(tex.rgb);
    hsv.x = fract(hsv.x + hue_shift * weight);
    vec3 shifted = hsv2rgb(hsv);
    
    tex.rgb = mix(tex.rgb, shifted, weight);
    
    COLOR = tex;
}
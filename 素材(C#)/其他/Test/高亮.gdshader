shader_type canvas_item;

uniform vec4 color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float strength : hint_range(0.0, 1.0) = 1.0;
uniform float brightness : hint_range(-1.0, 1.0) = 0.0;
uniform float saturation : hint_range(0.0, 2.0) = 1.0;
uniform float contrast : hint_range(0.0, 2.0) = 1.0;

float luminance(vec3 c) {
	return dot(c, vec3(0.2126, 0.7152, 0.0722));
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec3 src = tex.rgb;

	float srcLum = luminance(src);
	float tgtLum = max(luminance(color.rgb), 0.0001);

	vec3 tinted = color.rgb * (srcLum / tgtLum);
	float maxc = max(max(tinted.r, tinted.g), tinted.b);
	if (maxc > 1.0) tinted /= maxc;

	vec3 result = mix(src, tinted, strength);
	result = (result - 0.5) * contrast + 0.5 + brightness;

	float gray = luminance(result);
	result = mix(vec3(gray), result, saturation);

	COLOR = vec4(clamp(result, 0.0, 1.0), tex.a * color.a);
}
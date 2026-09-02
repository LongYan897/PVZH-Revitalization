shader_type canvas_item;

uniform float outline_width : hint_range(0.0, 15.0) = 4.0;
uniform float glow_intensity : hint_range(0.0, 2.0) = 1.0;
uniform vec4 glow_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    
    // 如果当前像素透明，检测周围
    if (color.a < 0.1) {
        float max_alpha = 0.0;
        vec2 step = vec2(outline_width) / TEXTURE_PIXEL_SIZE;  // ✅ 改成 vec2
        
        // 8方向采样
        vec2 dirs[8] = vec2[](
            vec2(1.0, 0.0), vec2(-1.0, 0.0), vec2(0.0, 1.0), vec2(0.0, -1.0),
            vec2(1.0, 1.0), vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(-1.0, 1.0)
        );
        
        for (int i = 0; i < 8; i++) {
            vec2 sample_uv = UV + dirs[i] * step;  // ✅ dirs[i] * step 两个 vec2 相乘
            if (sample_uv.x < 0.0 || sample_uv.x > 1.0 || 
                sample_uv.y < 0.0 || sample_uv.y > 1.0) {
                continue;
            }
            float a = texture(TEXTURE, sample_uv).a;
            if (a > max_alpha) max_alpha = a;
        }
        
        if (max_alpha > 0.1) {
            color = glow_color;
            color.a = max_alpha * glow_intensity;
        }
    }
    
    COLOR = color;
}
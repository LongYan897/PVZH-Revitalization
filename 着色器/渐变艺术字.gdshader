shader_type canvas_item;

// 顶部颜色
uniform vec4 top_color : source_color = vec4(1.0, 0.8, 0.0, 1.0);
// 底部颜色
uniform vec4 bottom_color : source_color = vec4(1.0, 0.2, 0.0, 1.0);

void fragment() {
    // 1. 获取引擎原本算好的颜色（包含文字本体和阴影）
    float safe_alpha = COLOR.a;
    
    // 2. 计算当前字形的局部 UV
    vec2 local_uv = (UV - REGION_RECT.xy) / REGION_RECT.zw;
    
    // 3. 计算垂直渐变
    vec4 gradient_color = mix(top_color, bottom_color, local_uv.y);
    
    // 4. 【核心改动】使用乘法将渐变颜色与原本颜色混合
    // 如果原本是白色文字（1,1,1），乘以渐变，就是渐变。
    // 如果原本是黑色阴影（0,0,0），乘以渐变，还是黑色。
    COLOR = vec4(gradient_color.rgb * COLOR.rgb, safe_alpha);
}
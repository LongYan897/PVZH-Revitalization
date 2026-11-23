shader_type canvas_item;

// 节点实际大小 (包含底部的阴影留白)
uniform vec2 rect_size = vec2(520.0, 240.0); 
// 按钮底部留白（也就是你额外拉长的高度，用来放阴影）
uniform float button_bottom_margin = 10.0; 

// 1. 最外层黑色边框
uniform vec4 outer_border_color : source_color = vec4(0, 0, 0, 1.0); 
uniform float outer_border_width = 15.0;

// 2. 第二层蓝色纯色边框
uniform vec4 middle_border_color : source_color = vec4(0.017, 1.0, 1.0, 1.0); 
uniform float middle_border_width = 15.0;

// 3. 最内侧垂直渐变
uniform vec4 top_color : source_color = vec4(0.017, 1.0, 1.0, 1.0); 
uniform vec4 bottom_color : source_color = vec4(0.017, 0.7, 0.7, 1.0); 

uniform float corner_radius = 45.0;

// === 阴影参数 ===
uniform vec4 shadow_color : source_color = vec4(0.0, 0.0, 0.0, 0.5); 
uniform vec2 shadow_offset = vec2(0.0, 12.0); 
uniform float shadow_blur = 15.0; 
// 【新增】阴影向内倾斜度 (0.0 = 矩形, 0.2 = 底部向内收窄 20%)
uniform float shadow_taper = 0.1; 

// 计算圆角 SDF 距离
float rounded_rect(vec2 p, vec2 size, float radius) {
    vec2 d = abs(p) - size + vec2(radius);
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
}

void fragment() {
    // 1. 将 UV (0~1) 转换为以中心为原点的像素坐标
    vec2 p = (UV - 0.5) * rect_size;
    
    // 将坐标系向上偏移，使得下半部分留空画阴影，而上半部分保持原来的按钮尺寸
    p.y += button_bottom_margin * 0.5; 
    
    // 计算按钮本体的实际半尺寸
    vec2 button_size = vec2(rect_size.x, rect_size.y - button_bottom_margin);
    vec2 half_size = button_size * 0.5;

    // 计算主按钮距离
    float dist = rounded_rect(p, half_size, corner_radius);

    // 抗锯齿平滑范围
    float aa = 0.5;
    
    // 计算遮罩 (0.0 表示在内，1.0 表示在外)
    float m_outer = smoothstep(-aa, aa, dist); 
    float m_middle = smoothstep(-aa, aa, dist + outer_border_width); 
    float m_inner = smoothstep(-aa, aa, dist + outer_border_width + middle_border_width); 

    // === 修正渐变计算 ===
    // 因为坐标系变了，不能用全局 UV.y，需要计算局部 Y 比例
    float local_y = clamp((p.y + half_size.y) / button_size.y, 0.0, 1.0);
    vec4 grad_color = mix(top_color, bottom_color, local_y);
    
    // 从内向外逐层混合主按钮颜色
    vec4 final_color = grad_color;
    final_color = mix(final_color, middle_border_color, m_inner);
    final_color = mix(final_color, outer_border_color, m_middle);

    // 处理主按钮透明通道
    final_color.a *= (1.0 - m_outer);

    // === 计算底部阴影 (向内斜) ===
    // 将查询点反向偏移
    vec2 shadow_p = p - shadow_offset;
    
    // 【核心修改】左右向内收窄计算
    // 将 y 坐标从顶部到底部映射到 0.0 ~ 1.0
    float t_taper = clamp((shadow_p.y + half_size.y) / (2.0 * half_size.y), 0.0, 1.0);
    // 根据高度计算当前宽度的缩放因子
    float current_taper = 1.0 - t_taper * shadow_taper;
    // 放大 x 坐标，等同于图形在视觉上向内收缩
    shadow_p.x /= max(current_taper, 0.1); // 防止除以 0

    float shadow_dist = rounded_rect(shadow_p, half_size, corner_radius);
    float shadow_alpha = 1.0 - smoothstep(0.0, shadow_blur, shadow_dist);
    
    vec4 final_shadow = shadow_color;
    final_shadow.a *= shadow_alpha;

    // === Alpha 混合 (阴影在下，主按钮在上) ===
    float out_a = final_color.a + final_shadow.a * (1.0 - final_color.a);
    vec3 out_rgb = final_color.rgb * final_color.a + final_shadow.rgb * final_shadow.a * (1.0 - final_color.a);
    out_rgb /= max(out_a, 0.0001);
    
    COLOR = vec4(out_rgb, out_a);
}
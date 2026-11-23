shader_type canvas_item;

// ================= 尺寸设置 =================
// 仅用于 ColorRect。如果你用在 TextureRect 上，Shader 会自动读取图片尺寸，此值将被忽略。
uniform vec2 manual_size = vec2(0.0, 0.0); 
uniform float global_alpha : hint_range(0.0, 1.0) = 1.0;

// ================= 外形设置 =================
uniform float corner_radius : hint_range(0.0, 500.0) = 40.0; // 外轮廓圆角半径
uniform float edge_softness : hint_range(0.0, 5.0) = 1.0;    // 边缘羽化（抗锯齿）

// ================= 第一层：纯色外边框 =================
uniform float border_width : hint_range(0.0, 100.0) = 10.0;   // 边框宽度
uniform vec4 border_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 边框颜色（纯色）

// ================= 第二层：渐变区域 =================
uniform float gradient_width : hint_range(0.0, 200.0) = 20.0; // 渐变区域的宽度
uniform vec4 gradient_outer_color : source_color = vec4(1.0, 1, 1.0, 1.0); // 渐变外沿色（紧贴边框内侧）
uniform vec4 gradient_inner_color : source_color = vec4(1.0, 1.0, 1.0, 0.0); // 渐变内沿色（紧贴核心外侧）

// ================= 第三层：内部纯色核心 =================
uniform vec4 core_color : source_color = vec4(1.0, 1, 1, 0.0); // 内部矩形的纯色

// ================= 纹理混合 (可选) =================
uniform bool use_texture_as_overlay = false; // 如果用作 TextureRect，是否将原图叠加在最终效果上

// SDF 算法：计算圆角矩形的距离场
float sdRoundBox(vec2 p, vec2 b, float r) {
    vec2 q = abs(p) - b + r;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
}

void fragment() {
    // 获取矩形真实像素尺寸
    vec2 rect_size = manual_size;
    if (rect_size.x <= 0.0 || rect_size.y <= 0.0) {
        rect_size = 1.0 / TEXTURE_PIXEL_SIZE; // TextureRect 自动读取
    }
    if (rect_size.x <= 1.0 || rect_size.y <= 1.0) {
        rect_size = vec2(200.0); // 兜底尺寸
    }
    
    // 将 UV 转换到以中心为原点的像素坐标系
    vec2 p = UV * rect_size - rect_size * 0.5;
    vec2 half_size = rect_size * 0.5;
    
    // 计算当前像素到外轮廓的 SDF 距离（内部为负值，外部为正值）
    float d = sdRoundBox(p, half_size, corner_radius);
    
    // --- 颜色计算区 ---
    
    // 1. 计算渐变颜色
    // 定义渐变起始点：在边框内侧 (-border_width) 
    // 定义渐变结束点：在核心外侧 (-border_width - gradient_width)
    // grad_t 为 0 时在内部边界，为 1 时在外部边界
    float grad_t = clamp((d - (-border_width - gradient_width)) / max(gradient_width, 0.001), 0.0, 1.0);
    vec4 current_gradient = mix(gradient_inner_color, gradient_outer_color, grad_t);
    
    // 2. 设定基础色为内部核心色
    vec4 final_color = core_color;
    
    // 3. 叠加渐变层：用 smoothstep 从核心色平滑过渡到渐变层
    float grad_mask = smoothstep(-border_width - gradient_width - edge_softness, 
                                -border_width - gradient_width + edge_softness, d);
    final_color = mix(final_color, current_gradient, grad_mask);
    
    // 4. 叠加纯色边框层：用 smoothstep 从渐变层平滑过渡到边框色
    float border_mask = smoothstep(-border_width - edge_softness, 
                                  -border_width + edge_softness, d);
    final_color = mix(final_color, border_color, border_mask);
    
    // 5. 外部裁剪（抗锯齿）：处理外轮廓，让边缘平滑，外部透明
    float outer_mask = smoothstep(edge_softness, 0.0, d);
    final_color.a *= outer_mask * global_alpha;
    
    // 6. (可选) 如果挂在 TextureRect 上并且想保留图片内容
    if (use_texture_as_overlay) {
        vec4 tex_color = texture(TEXTURE, UV);
        final_color.rgb = mix(final_color.rgb, tex_color.rgb, tex_color.a);
    }

    COLOR = final_color;
}
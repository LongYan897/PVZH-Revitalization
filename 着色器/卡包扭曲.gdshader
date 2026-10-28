shader_type canvas_item;

// ── 卡包扭曲：把矩形卡包图变成"躺倒 + 弧面 + 底部有厚度"的样子 ──
// 全部用顶点几何变形实现，图片不会被裁掉，只改变形状。

// 透视：顶部横向收窄的比例，0 = 不变，0.3 = 顶部只有底部宽度的 70%
uniform float perspective : hint_range(-1.0, 1.0) = 0.22;

// 垂直压缩：0.34 = 高度压到 34%（模拟从斜上方看，卡包"躺"下来）
uniform float y_squash : hint_range(0.05, 1.0) = 0.34;

// 弧形弯曲（像素）：让上下边缘变成弧线
uniform float bend : hint_range(-128.0, 128.0) = 16.0;

// 倾斜（像素）：左右高低错开
uniform float tilt : hint_range(-128.0, 128.0) = 0.0;

// 底部锁定：最下面这一比例的高度不参与任何扭曲（保持水平直边）
uniform float bottom_lock : hint_range(0.0, 0.5) = 0.16;

// 厚度（像素）：把锁定的底部再往下拉出一段，模拟卡包的侧面
uniform float thickness : hint_range(0.0, 64.0) = 14.0;

// 底部压暗：0 = 不变暗，0.3 = 底部亮度降到 70%（让侧面有体积感）
uniform float bottom_shade : hint_range(0.0, 0.8) = 0.25;

// 波浪（果冻抖动，像素）
uniform float wave_amp : hint_range(0.0, 64.0) = 0.0;
uniform float wave_freq : hint_range(0.0, 10.0) = 2.0;
uniform float wave_speed : hint_range(0.0, 10.0) = 1.5;

// 0 = 顶部, 1 = 最底部：底部锁定区的权重（平滑过渡）
float bottom_weight(vec2 uv) {
    return smoothstep(1.0 - bottom_lock, 1.0, uv.y);
}

void vertex() {
    // t: 底部锁定区的权重（1 = 底部不动，0 = 上方正常扭曲）
    float t = bottom_weight(UV);
    float k = 1.0 - t;

    // 1) 透视：越靠顶部横向越窄；底部锁定区保持原宽
    float sx = mix(1.0 - perspective, 1.0, UV.y);
    sx = mix(sx, 1.0, t);
    VERTEX.x *= sx;

    // 2) 垂直压缩：整张图压扁（不裁剪）
    VERTEX.y *= y_squash;

    // 3) 弧形 + 倾斜 + 波浪：底部锁定区权重为 0，所以底边保持水平直线
    float dx = UV.x - 0.5;
    VERTEX.y += bend * (0.25 - dx * dx) * 4.0 * k;
    VERTEX.y += tilt * dx * 2.0 * k;
    VERTEX.y += wave_amp * sin(UV.x * wave_freq * 6.2831853 + TIME * wave_speed) * k;

    // 4) 厚度：把底部那一条往下拉，形成侧面
    VERTEX.y += thickness * t;
}

void fragment() {
    vec4 col = texture(TEXTURE, UV);

    // 底部锁定区压暗，制造"侧面背光"的厚度感
    float t = bottom_weight(UV);
    col.rgb *= mix(1.0, 1.0 - bottom_shade, t);

    COLOR = col;
}

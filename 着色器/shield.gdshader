shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec2 center = vec2(0.5, 0.5);
uniform float start_angle = 0.0;
uniform float feather : hint_range(0.0, 20.0) = 2.0;

void fragment() {
    vec2 uv = UV - center;
    float angle = atan(uv.y, uv.x);
    float radius = length(uv);
    
    // 弧度转角度
    float angle_deg = degrees(angle);
    
    // 关键：270° 偏移，让 0° 对应正上方
    angle_deg = angle_deg + 270.0;
    
    // 归一化
    if (angle_deg >= 360.0) angle_deg -= 360.0;
    
    float end_angle = start_angle + progress * 360.0;
    
    float in_sector = 0.0;
    
    if (end_angle <= 360.0) {
        if (angle_deg >= start_angle && angle_deg <= end_angle) {
            in_sector = 1.0;
        }
    } else {
        if (angle_deg >= start_angle || angle_deg <= end_angle - 360.0) {
            in_sector = 1.0;
        }
    }
    
    if (feather > 0.0 && in_sector > 0.5) {
        float edge_distance = 1000.0;
        if (end_angle <= 360.0) {
            float dist_to_start = abs(angle_deg - start_angle);
            float dist_to_end = abs(angle_deg - end_angle);
            edge_distance = min(dist_to_start, dist_to_end);
        } else {
            float dist_to_start = abs(angle_deg - start_angle);
            float dist_to_end = abs(angle_deg - (end_angle - 360.0));
            edge_distance = min(dist_to_start, dist_to_end);
        }
        if (edge_distance < feather) {
            in_sector = edge_distance / feather;
        }
    }
    
    if (radius < 0.01) in_sector = 0.0;
    
    vec4 color = texture(TEXTURE, UV);
    COLOR = color;
    COLOR.a = color.a * in_sector;
}
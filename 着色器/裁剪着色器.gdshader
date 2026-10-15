shader_type canvas_item;

uniform vec2 circle_center = vec2(0.5, 0.5);
uniform float circle_radius : hint_range(0.0, 1.0) = 0.5;
uniform float edge_softness : hint_range(0.0, 0.5) = 0;
uniform bool invert_mask = false;

void fragment() {
    float dist = distance(UV, circle_center);
    
    float mask = 1.0 - smoothstep(circle_radius - edge_softness,
                                  circle_radius + edge_softness,
                                  dist);
    
    if (invert_mask) {
        mask = 1.0 - mask;
    }
    
    COLOR.a *= mask;
}
shader_type canvas_item;

uniform vec2 tile_size = vec2(0.12, 0.12);
uniform vec2 tile_gap = vec2(0.0, 0.0);
uniform vec2 cluster_count_even = vec2(3.0, 3.0);
uniform vec2 cluster_count_odd = vec2(2.0, 2.0);
uniform vec2 max_clusters = vec2(3.0, 3.0);
uniform vec2 cluster_gap = vec2(0.02, 0.02);
uniform vec2 stagger_offset = vec2(0.0, 0.0);
uniform vec2 cluster_offset = vec2(0.0, 0.0);
uniform vec2 cluster_index_offset = vec2(0.0, 0.0);

uniform float icon_scale = 1.3;
uniform vec4 icon_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 1.0;

uniform float brightness_threshold : hint_range(0.0, 1.0) = 0.7;
uniform float saturation_threshold : hint_range(0.0, 1.0) = 0.15;
uniform float edge_smoothness : hint_range(0.01, 0.5) = 0.2;
uniform float alpha_softness : hint_range(0.01, 1.0) = 0.4;
uniform float dilation_radius : hint_range(0.0, 0.02) = 0.002;
uniform float shrink : hint_range(0.0, 0.2) = 0.04;

float get_white_mask(sampler2D tex_sampler, vec2 uv) {
    vec4 tex = texture(tex_sampler, clamp(uv, 0.0, 1.0));
    float max_c = max(tex.r, max(tex.g, tex.b));
    float min_c = min(tex.r, min(tex.g, tex.b));
    float sat = (max_c == 0.0) ? 0.0 : (max_c - min_c) / max_c;
    
    float bright_step = smoothstep(brightness_threshold - edge_smoothness, brightness_threshold + edge_smoothness * 0.5, max_c);
    float sat_step = smoothstep(saturation_threshold + edge_smoothness, saturation_threshold - edge_smoothness * 0.5, sat);
    
    return bright_step * sat_step;
}

void fragment() {
    vec2 cell_step = tile_size + tile_gap;
    vec2 max_count = max(cluster_count_even, cluster_count_odd);
    vec2 slot_size = max_count * cell_step;
    vec2 slot_step = slot_size + cluster_gap;
    
    vec2 total_size = max_clusters * slot_step;
    vec2 center_offset = (vec2(1.0) - total_size) * 0.5;
    
    vec2 uv_base = UV - center_offset - cluster_offset;
    
    vec2 raw_idx = floor(uv_base / slot_step);
    float row_mod = mod(raw_idx.y, 2.0);
    float col_mod = mod(raw_idx.x, 2.0);
    
    vec2 shifted_uv = uv_base;
    shifted_uv.x += stagger_offset.x * step(0.5, row_mod);
    shifted_uv.y += stagger_offset.y * step(0.5, col_mod);
    
    vec2 idx_float = shifted_uv / slot_step;
    vec2 idx = floor(idx_float);
    
    float in_bounds = (idx.x >= 0.0 && idx.x < max_clusters.x && 
                       idx.y >= 0.0 && idx.y < max_clusters.y) ? 1.0 : 0.0;
                       
    vec2 local_uv = fract(idx_float) * slot_step;
    
    vec2 global_idx = idx + cluster_index_offset;
    float is_odd = mod(global_idx.x + global_idx.y, 2.0);
    vec2 current_count = mix(cluster_count_even, cluster_count_odd, is_odd);
    
    vec2 current_size = current_count * cell_step;
    vec2 padding = (slot_size - current_size) * 0.5;
    vec2 cluster_uv = local_uv - padding;
    
    vec2 t_idx = floor(cluster_uv / cell_step);
    float in_cluster = (t_idx.x >= 0.0 && t_idx.x < current_count.x && t_idx.y >= 0.0 && t_idx.y < current_count.y) ? 1.0 : 0.0;
    
    vec2 t_uv = mod(cluster_uv, cell_step);
    vec2 icon_uv = clamp(t_uv / tile_size, 0.0, 1.0);
    icon_uv = (icon_uv - 0.5) / icon_scale + 0.5;
    
    float is_white = get_white_mask(TEXTURE, icon_uv);
    
    if (dilation_radius > 0.0) {
        float d = dilation_radius;
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( d,  0.0)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2(-d,  0.0)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( 0.0,  d)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( 0.0, -d)));
        
        float dd = d * 0.70710678;
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( dd,  dd)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2(-dd,  dd)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( dd, -dd)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2(-dd, -dd)));
        
        float dx1 = d * 0.5;
        float dy1 = d * 0.8660254;
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( dx1,  dy1)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2(-dx1,  dy1)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( dx1, -dy1)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2(-dx1, -dy1)));
        
        float dx2 = d * 0.8660254;
        float dy2 = d * 0.5;
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( dx2,  dy2)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2(-dx2,  dy2)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2( dx2, -dy2)));
        is_white = max(is_white, get_white_mask(TEXTURE, icon_uv + vec2(-dx2, -dy2)));
    }
    
    vec2 p = abs(icon_uv - 0.5);
    float dist = max(p.x, p.y);
    dist = max(dist, (p.x + p.y) * 0.70710678);
    
    float edge_mask = smoothstep(0.5 - shrink, 0.5 - shrink - 0.05, dist);
    
    vec4 tex = texture(TEXTURE, clamp(icon_uv, 0.0, 1.0));
    float final_alpha = is_white * edge_mask * tex.a * in_cluster * in_bounds * opacity * icon_color.a;
    
    final_alpha = smoothstep(0.0, 1.0, final_alpha);
    final_alpha = pow(final_alpha, 1.0 + alpha_softness);
    
    COLOR = vec4(icon_color.rgb, final_alpha);
}
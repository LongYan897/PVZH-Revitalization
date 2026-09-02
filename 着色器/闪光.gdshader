shader_type canvas_item;
render_mode blend_premul_alpha;

// ───────── Visual controls ─────────
uniform float Angle_deg : hint_range(-90.0, 90.0) = 30.0;   // base band angle (degrees)
uniform float Brightness : hint_range(0.0, 5.0) = 2.5;      // white intensity added over sprite

// Feather (softness). Set to 0.0 for hard edges.
uniform float Band_Soft  : hint_range(0.0, 0.3) = 0.06;     

// ───────── Staging controls ─────────
// 1, 2, or 3 sweeps per cycle
uniform int   Stage_Count : hint_range(1, 3) = 2;

// Per-stage durations (seconds). Only the first N are used (N = Stage_Count).
uniform float Stage1_Duration : hint_range(0.01, 10.0) = 1.0;
uniform float Stage2_Duration : hint_range(0.01, 10.0) = 1.5;
uniform float Stage3_Duration : hint_range(0.01, 10.0) = 1.0;

// Per-stage band widths (UV units). Only the first N are used.
uniform float Stage1_Width : hint_range(0.0, 0.5) = 0.12;
uniform float Stage2_Width : hint_range(0.0, 0.5) = 0.12;
uniform float Stage3_Width : hint_range(0.0, 0.5) = 0.12;

// Per-stage direction toggles (true = left→right; false = right→left)
uniform bool Stage1_LeftToRight = true;
uniform bool Stage2_LeftToRight = false; // default opposite for variation
uniform bool Stage3_LeftToRight = true;

// ───────── Animation mode ─────────
// Loop → continuous; One_Shot → plays full cycle once (optional delay) then stays off.
uniform bool  One_Shot = false;
uniform float OneShot_Delay : hint_range(0.0, 10.0) = 0.0;

// Loop phase offset (0..1 across the entire multi-stage cycle)
uniform float Phase_Offset : hint_range(0.0, 1.0) = 0.0;

// ───────── NEW: Angle wiggle during sweep ─────────
// Enable to vary angle by ±Wiggle_Amount_deg over the stage progress.
uniform bool  Wiggle_Angle = false;
uniform float Wiggle_Amount_deg : hint_range(0.0, 45.0) = 15.0;

// ───────── NEW: Pause between shines (seconds) ─────────
uniform float Shine_Pause : hint_range(0.0, 10.0) = 0.4;

// Rotate UVs around center
vec2 rotate_uv(vec2 uv, vec2 center, float deg){
    float a = radians(deg);
    mat2 r = mat2(vec2(cos(a), -sin(a)), vec2(sin(a), cos(a)));
    return r * (uv - center) + center;
}

void fragment() {
    // 1) Base sprite color
    vec4 base = texture(TEXTURE, UV);

    // 2) Stage durations actually used
    int N = clamp(Stage_Count, 1, 3);
    float T1 = max(1e-4, Stage1_Duration);
    float T2 = (N >= 2) ? max(1e-4, Stage2_Duration) : 0.0;
    float T3 = (N >= 3) ? max(1e-4, Stage3_Duration) : 0.0;

    // Number of pauses per cycle:
    // - Loop: pause after each stage (including last→first)
    // - One_Shot: pause only between stages (no pause after last)
    float P = max(0.0, Shine_Pause);
    float pause_count = One_Shot ? float(N - 1) : float(N);

    // 3) Total cycle duration includes pauses
    float Ttotal = T1 + T2 + T3 + P * pause_count;
    Ttotal = max(Ttotal, 1e-4); // safety

    // 4) Determine time position in the cycle (loop vs one-shot)
    float t01;
    if (One_Shot) {
        float t = max(0.0, TIME - OneShot_Delay);
        t01 = clamp(t / Ttotal, 0.0, 1.0);
    } else {
        t01 = fract(Phase_Offset + TIME / Ttotal);
    }
    float tsec = t01 * Ttotal;

    // If one-shot finished → output base only
    bool finished_one_shot = (One_Shot && t01 >= 1.0);

    // 5) Build timeline with pauses interleaved
    // Timeline order:
    // S1(T1) → P(P) → [S2(T2) → P(P)] → [S3(T3) → P(P or none if One_Shot)]
    float t = tsec;

    // Boundaries
    float s1_start = 0.0;
    float s1_end   = s1_start + T1;
    float p1_end   = s1_end + ((P > 0.0) ? P : 0.0);

    float s2_start = (N >= 2) ? p1_end : 0.0;
    float s2_end   = (N >= 2) ? (s2_start + T2) : 0.0;
    float p2_end   = (N >= 2) ? (s2_end + ((P > 0.0) ? P : 0.0)) : 0.0;

    float s3_start = (N >= 3) ? p2_end : 0.0;
    float s3_end   = (N >= 3) ? (s3_start + T3) : 0.0;
    // Pause after stage 3:
    //  - Loop: include pause 3
    //  - One_Shot: no pause after last stage
    float p3_end   = (N >= 3)
        ? (s3_end + (((!One_Shot) && (P > 0.0)) ? P : 0.0))
        : 0.0;

    // 6) Decide if we're in a stage segment or a pause segment
    int stage = 0;        // 0 = pause, 1/2/3 = in that stage
    float t_local = 0.0;  // time since current stage began
    float T_stage = 1.0;  // current stage duration

    if (t >= s1_start && t < s1_end) {
        stage = 1; T_stage = T1; t_local = t - s1_start;
    } else if (N >= 2 && t >= s2_start && t < s2_end) {
        stage = 2; T_stage = T2; t_local = t - s2_start;
    } else if (N >= 3 && t >= s3_start && t < s3_end) {
        stage = 3; T_stage = T3; t_local = t - s3_start;
    } else {
        stage = 0; // in a pause window
    }

    // If one-shot finished OR we're in a pause: show base only (premultiplied)
    if (finished_one_shot || stage == 0) {
        vec3 out_rgb0 = base.rgb * base.a; // premultiply
        COLOR = vec4(out_rgb0, base.a);
        
    }

    // 7) Normalized progress within the current stage (0..1)
    float p = clamp(t_local / T_stage, 0.0, 1.0);

    // 8) Effective angle with (optional) wiggle
    float angle_eff_deg = Angle_deg;
    if (Wiggle_Angle) {
        float wig = sin(3.14159265 * (2.0 * p - 1.0)); // [-1..1], 0 at edges, ±1 at center
        angle_eff_deg += Wiggle_Amount_deg * wig;
    }

    // 9) Rotated UVs with effective angle
    float a = radians(angle_eff_deg);
    vec2  ruv = rotate_uv(UV, vec2(0.5), angle_eff_deg);

    // 10) Rotated sprite horizontal footprint (in rotated space along ruv.x)
    float c = abs(cos(a));
    float s = abs(sin(a));
    float min_x = 0.5 - 0.5 * (c + s);
    float max_x = 0.5 + 0.5 * (c + s);

    // 11) Per-stage width & direction
    float width = Stage1_Width;
    bool  dir_now = Stage1_LeftToRight;
    if (stage == 2) { width = Stage2_Width; dir_now = Stage2_LeftToRight; }
    else if (stage == 3) { width = Stage3_Width; dir_now = Stage3_LeftToRight; }

    float half_w = max(width * 0.5, 1e-4);

    // Feather (hard edges if Band_Soft == 0.0)
    float soft_raw = Band_Soft;
    float soft     = max(Band_Soft, 1e-5); // avoid zero in smoothstep edges

    // Padding ensures the band starts & ends fully off the sprite for this stage
    float pad = half_w + soft;

    // Offscreen→offscreen sweep bounds for this stage
    float start_x = min_x - pad;
    float end_x   = max_x + pad;

    // 12) Sweep center position (off→off)
    float center = dir_now ? mix(start_x, end_x, p) : mix(end_x, start_x, p);

    // 13) Distance from band center → mask
    float d = abs(ruv.x - center);

    float band;
    if (soft_raw <= 0.0) {
        band = step(d, half_w);   // crisp hard edge
    } else {
        band = 1.0 - smoothstep(half_w, half_w + soft, d);
    }

    // Apply only where the sprite is visible
    band *= base.a;

    // 14) Add white shine and premultiply for premul-alpha blending
    vec3 lit_rgb = base.rgb + vec3(1.0) * (band * Brightness);
    float out_a   = base.a;
    vec3  out_rgb = lit_rgb * out_a;

    COLOR = vec4(out_rgb, out_a);
}
shader_type canvas_item;

uniform vec4 color_gap : source_color = vec4(0.25, 0.25, 0.25, 0.0);
uniform vec4 color_stripe : source_color = vec4(1.0, 0.75, 0.0, 1.0);

uniform sampler2D stripe_gradient;

uniform float divisions = 8.0;
uniform float speed = 0.1;
uniform float angle = 0.7854;

// Controls how much of each division is occupied by a stripe.
// 0.5 = stripe takes half the space
// 0.25 = stripe takes a quarter of the space
uniform float stripe_width : hint_range(0.01, 1.0) = 0.5;

void fragment() {
	// Rotate and animate UVs
	float w = cos(angle) * UV.x + sin(angle) * UV.y;
	w -= TIME * speed;

	// Repeating 0-1 coordinate for each stripe segment
	float pattern = fract(w * divisions);

	// Convert pattern into stripe-local UV
	float stripe_uv = pattern / stripe_width;

	// Determine whether we're inside the stripe region
	float inside_stripe = step(pattern, stripe_width);

	// Sample gradient only within stripe area
	float gradient_alpha = texture(
		stripe_gradient,
		vec2(clamp(stripe_uv, 0.0, 1.0), 0.5)
	).r;

	float stripe_mask = inside_stripe * gradient_alpha;

	COLOR = mix(
		color_gap,
		color_stripe,
		stripe_mask
	);

	COLOR.a = mix(
		color_gap.a,
		color_stripe.a,
		stripe_mask
	);
}
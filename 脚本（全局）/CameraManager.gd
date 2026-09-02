extends Node
var camera:Camera2D
var noise:FastNoiseLite
var shakeTime:float = 0
var shakeDuration:float = 0
var shakeIntensity:float = 0
func _ready() -> void:
	setup()
	camera.position = get_viewport().get_visible_rect().size / 2
	await get_tree().create_timer(1).timeout
	#shake(10,1,10)
	#tweenShake(20,5)
func _process(delta: float) -> void:
	if camera == null || noise == null:return
	shakeTime += delta
	var x = noise.get_noise_2d(shakeTime,0)
	var y = noise.get_noise_2d(shakeTime,100)
	var envelope = max(1.0 - (shakeTime / shakeDuration),0)
	var offset = Vector2(x,y) * shakeIntensity * envelope
	camera.offset = offset
func setup():
	camera = Camera2D.new()
	add_child(camera)
	#噪声初始化
	noise = FastNoiseLite.new()
	noise.seed = randi()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
func getCamera()->Camera2D:
	return camera
func shake(intensity:float,time:float,frequency:float = 10):
	shakeTime = 0
	shakeIntensity = intensity
	shakeDuration = time
	noise.frequency = frequency
func tweenShake(intensity:float,time:float,frequency:float = 10):
	shakeTime = 0
	shakeIntensity = 0
	shakeDuration = 9999999
	noise.frequency = frequency
	var startTime = time * 0.35
	var endTime = time * 0.65
	var tween = create_tween()
	tween.tween_property(self,"shakeIntensity",intensity,startTime)
	await tween.finished
	tween = create_tween()
	tween.tween_property(self,"shakeIntensity",0,endTime)

@tool
class_name AutoFitHBoxContainer
extends Control

@export var separation: float = 16.0:
	set(value):
		separation = maxf(0.0, value)
		_queue_layout()

var _queued := false

func _ready() -> void:
	resized.connect(_queue_layout)
	child_entered_tree.connect(_on_child_changed)
	child_exiting_tree.connect(_on_child_changed)
	_queue_layout()

func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_queue_layout()

func _on_child_changed(_child: Node) -> void:
	_queue_layout()

func _queue_layout() -> void:
	if _queued:
		return
	_queued = true
	call_deferred("_layout_children")

func _layout_children() -> void:
	_queued = false
	var items: Array[Control] = []
	for child in get_children():
		if child is Control and child.visible:
			items.append(child)
	if items.is_empty() or size.x <= 0.0 or size.y <= 0.0:
		return

	var natural_width := 0.0
	var natural_height := 0.0
	var natural_sizes: Array[Vector2] = []
	for item in items:
		var item_size := item.get_combined_minimum_size()
		natural_sizes.append(item_size)
		natural_width += item_size.x
		natural_height = maxf(natural_height, item_size.y)
	natural_width += separation * float(items.size() - 1)
	if natural_width <= 0.0 or natural_height <= 0.0:
		return

	var fit := minf(1.0, minf(size.x / natural_width, size.y / natural_height))
	var row_size := Vector2(natural_width, natural_height) * fit
	var cursor_x := (size.x - row_size.x) * 0.5
	var top_y := (size.y - row_size.y) * 0.5

	for index in items.size():
		var item := items[index]
		var natural := natural_sizes[index]
		item.position = Vector2(cursor_x, top_y)
		item.size = natural
		item.pivot_offset = Vector2.ZERO
		item.scale = Vector2.ONE * fit
		cursor_x += (natural.x + separation) * fit

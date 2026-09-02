extends Control
@onready var playButton = $"联机"
@onready var playSecondPage = $"联机二级菜单"
@onready var createPage = $"创建房间界面"
@onready var joinPage = $"加入房间界面"
@onready var createNumber = $"创建房间界面/输入框"
@onready var joinNumber = $"加入房间界面/输入框"
@onready var enemySprite = $"创建房间界面/对面框/头像框"
@onready var enemyLabel = $"创建房间界面/对面框/Label"
@onready var startButton = $"创建房间界面/开始游戏"

var page:String
var createOnce:bool = false
func quickPos(node,pos:Vector2,time:float = 0.2):
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(node,"position",pos,time)
func quickScale(node,mscale:Vector2,time:float = 0.2):
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(node,"scale",mscale,time)
func _ready() -> void:
	page = "main"
	playSecondPage.hide()
	createPage.hide()
	joinPage.hide()
	enemySprite.hide()
	startButton.scale = Vector2(0,0)
	WebSocketClient.connect_to_server("ws://127.0.0.1:9090")
	WebSocketClient.connection_succeeded.connect(_on_connected)
	WebSocketClient.room_created.connect(createRoomCode)
	WebSocketClient.receiveData.connect(reciveMsg)
func createRoomCode(data):
	print("data:",data)
	createNumber.text = data.get("code", "")
func reciveMsg(data):
	if data.has("uuid") and data.uuid == WebSocketClient.uuid:
		return
	var hex_msg := str(data.get("msg", ""))
	if hex_msg == "":
		return
	var bytes := hex_msg.hex_decode()
	var original_data = bytes_to_var(bytes)
	match original_data.sendType:
		"intro":
			enemyLabel.text = "玩家已加入"
			enemySprite.show()
			enemySprite.scale = Vector2.ZERO
			quickScale(enemySprite,Vector2(0.35,0.35),0.3)
			quickScale(startButton,Vector2(1,1),0.3)
		"startGame":
			startGame(PVZ.Type.ZOMBIE)
func _on_联机_pressed() -> void:
	if page != "main":return
	playSecondPage.show()
	playSecondPage.position.y = 1280
	quickPos(playSecondPage,Vector2(0,0),0.4)
	page = "secongPage"
func _on_创建房间_pressed() -> void:
	if page != "secongPage":return
	createPage.show()
	createPage.position.y = 1280
	quickPos(createPage,Vector2(0,0),0.4)
	page = "createPage"
	if !createOnce:
		print("🔄 正在发送创建房间请求...")
		WebSocketClient.send_message("create_room", {})
		createOnce = true
func _on_创建房间返回按钮_pressed() -> void:
	if page != "createPage":return
	createPage.position.y = 0
	quickPos(createPage,Vector2(0,1280),0.4)
	page = "secongPage"
func _on_二级菜单返回按钮_pressed() -> void:
	if page != "secongPage":return
	playSecondPage.position.y = 0
	quickPos(playSecondPage,Vector2(0,1280),0.4)
	page = "main"
func _on_connected():
	print("服务器连接成功")
func _on_加入房间_pressed() -> void:
	if page != "secongPage":return
	joinPage.show()
	joinPage.position.y = 1280
	quickPos(joinPage,Vector2(0,0),0.4)
	page = "joinPage"
func _on_加入房间返回按钮_pressed() -> void:
	if page != "joinPage":return
	joinPage.show()
	joinPage.position.y = 0
	quickPos(joinPage,Vector2(0,1280),0.4)
	page = "secongPage"
func _on_进入房间_pressed() -> void:
	var code = joinNumber.text
	print("code:",code)
	if code:
		WebSocketClient.send_message("join_room", {"code":code})
		WebSocketClient.chat({"sendType":"intro"})
		var CQ = CQData.new()
		CQ.type = CQData.Type.CHOOSE_CARD
		#WebSocketClient.chat(CQ.dict())
func _on_开始游戏_pressed() -> void:
	WebSocketClient.chat({"sendType":"startGame"})
	startGame(PVZ.Type.PLANT)
func startGame(type:PVZ.Type):
	print("type:",type)
	if type == PVZ.Type.PLANT:
		TurnManager.team = 1
	elif type == PVZ.Type.ZOMBIE:
		TurnManager.team = 2
	TurnManager.attackMode = TurnManager.AttactMode.PLAYER
	SceneManager.change_scene("res://场景/战斗场景/战斗场景.tscn",{"speed":4,"wait_time":0.2})

extends Node

# --- Sinais para comunicação com o resto do jogo ---
signal connection_succeeded
signal connection_failed
signal connection_closed
signal room_created(data)
signal room_joined(data)
signal server_error(data)
signal start_game 
signal receiveData(data)

# --- Sinais para gameplay ---
signal spawn_local_player(player_data)
signal spawn_new_player(player_data)
signal spawn_network_players(players_data)
signal update_position(position_data)
signal player_disconnected(player_data)

var uuid: String = ""              # Identificador único do jogador
var _peer := WebSocketPeer.new()   # Conexão WebSocket
var _is_connected := false         # Flag de conexão
var _is_connecting := false

func _enter_tree():
	print("WebSocketClient autoload carregado")

func _process(_delta):
	# Só processa se estiver conectado
	if _peer.get_ready_state() == WebSocketPeer.STATE_CONNECTING:
		_peer.poll()
		return

	if _peer.get_ready_state() == WebSocketPeer.STATE_OPEN:
		_peer.poll()
		_is_connected = true
		_is_connecting = false

	# Processa pacotes recebidos do servidor
	while _is_connected and _peer.get_available_packet_count() > 0:
		var packet = _peer.get_packet().get_string_from_utf8()
		var data = JSON.parse_string(packet)
		if data is Dictionary:
			handle_incoming_data(data)

	var state = _peer.get_ready_state()
	if _is_connected and (state == WebSocketPeer.STATE_CLOSED or state == WebSocketPeer.STATE_CLOSING):
		_is_connected = false
		_is_connecting = false
		emit_signal("connection_closed")

func connect_to_server(url: String):
	# Conecta ao servidor via WebSocket
	if _is_connected or _is_connecting or _peer.get_ready_state() != WebSocketPeer.STATE_CLOSED:
		push_warning("WebSocket connection is already open or connecting.")
		return

	var err = _peer.connect_to_url(url)
	if err != OK:
		_is_connected = false
		_is_connecting = false
		emit_signal("connection_failed")
	else:
		_is_connecting = true
		_is_connected = false

func send_message(command: String, content: Dictionary):
	# Envia mensagens para o servidor em formato JSON
	if not _is_connected or _peer.get_ready_state() != WebSocketPeer.STATE_OPEN:
		push_warning("Cannot send message, WebSocket is not open.")
		return

	_peer.send_text(JSON.stringify({"cmd": command, "content": content}))

func handle_incoming_data(data: Dictionary):
	# Lida com mensagens recebidas do servidor
	var cmd = data.get("cmd", "")
	var content = data.get("content", {})
	
	match cmd:
		"joined_server":
			uuid = str(content.get("uuid", ""))
			_is_connected = true
			_is_connecting = false
			emit_signal("connection_succeeded")
		"room_created":
			emit_signal("room_created", content)
		"room_joined":
			emit_signal("room_joined", content)
		"start_game":
			emit_signal("start_game") 
		"spawn_local_player":
			emit_signal("spawn_local_player", content.get("player", {}))
		"spawn_new_player":
			emit_signal("spawn_new_player", content.get("player", {}))
		"spawn_network_players":
			emit_signal("spawn_network_players", content.get("players", []))
		"update_position":
			emit_signal("update_position", content)
		"player_disconnected":
			emit_signal("player_disconnected", content)
		"error":
			emit_signal("server_error", content)
		"new_chat_message":
			emit_signal("receiveData", content)
		_:
			push_error("Comando não reconhecido do servidor: %s" % cmd)

func chat(data):
	if data is CQData:data = data.dict()
	var msg = var_to_bytes(data)
	var msg64 = msg.hex_encode()
	send_message("chat",{"msg":msg64})

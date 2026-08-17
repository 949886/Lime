@tool
extends Node3D
## Helper for displaying a SpineSprite inside a 3D scene via SubViewport + Sprite3D.
## Attach this script to the character root Node3D (the one that owns Sprite3D).

@export var animation_name: String = "Move"
@export var loop: bool = true

@onready var sprite_3d: Sprite3D = $Sprite3D
@onready var sub_viewport: SubViewport = $Sprite3D/SubViewport
@onready var spine_sprite: SpineSprite = $Sprite3D/SubViewport/SpineSprite

func _ready() -> void:
	# 1. 把 SubViewport 渲染结果赋给 Sprite3D，让它在 3D 世界中可见
	if sprite_3d and sub_viewport:
		sprite_3d.texture = sub_viewport.get_texture()
	
	# 2. 播放动画
	if spine_sprite == null or spine_sprite.skeleton_data_res == null:
		return
	
	var anim_state = spine_sprite.get_animation_state()
	if anim_state == null:
		return
	
	# 直接尝试设置动画。如果动画名不存在，Spine 运行时通常会忽略或保持空。
	# 常见可用动画：Move / Default / OnAttack
	anim_state.set_animation(animation_name, loop, 0)

extends Node

signal RequestCall()


func EmitSignal(sender):
	emit_signal("RequestCall", sender)



using Godot;
using System;

public partial class Indicator : Sprite2D
{
	bool _enabled = false;
	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
			Visible = _enabled;
			SetProcess(_enabled);

		}
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetProcess(false);
		Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Rotate((float)Mathf.DegToRad(10 * delta));
	}
}

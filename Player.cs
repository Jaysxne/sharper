using Godot;
using System;

[GlobalClass]
public partial class Player : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	

	[Export]
	public SpringArm3D springArm;

	[Export]
	public MeshInstance3D mesh;

	[Export]
	private float sensX = 1;
	[Export]
	private float sensY = 1;

	[Export]
	float turnSpeed = 5;
	float rotX;
	float rotY;
	float pitchLimit = Mathf.DegToRad(75);
	


    public override void _Ready()
    {
        base._Ready();
		//Input.MouseMode = Input.MouseModeEnum.Captured;
		
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
		
		
		
    }

    public override void _Input(InputEvent @event)

	{
		base._Input(@event);
		if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion mouseEvent = (InputEventMouseMotion) @event;
			Vector2 delta = mouseEvent.ScreenRelative;
			rotX += delta.X * 0.01f;
			rotY += delta.Y * 0.01f;
			rotY = Mathf.Clamp(rotY, -pitchLimit, pitchLimit);
			springArm.Basis = Basis.Identity;
            springArm.RotateObjectLocal(Vector3.Up, -rotX );
            springArm.RotateObjectLocal(Vector3.Right, -rotY);
			
            float pitch = Mathf.Clamp(Mathf.RadToDeg(springArm.Rotation.X), -45, 45);
			//springArm.Rotation = new Vector3(Mathf.DegToRad(pitch), Mathf.DegToRad(springArm.Rotation.Y),0);
			


		}
		
	}
    public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = springArm.Basis * new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

		
		if (direction != Vector3.Zero)
		{
			Vector3 lookDirection = (mesh.Basis * direction with { Y = 0});
			float targetRotation = Mathf.Atan2(-lookDirection.X, -lookDirection.Z) - mesh.Rotation.Y;
            float rotAngle = Mathf.LerpAngle(mesh.Rotation.Y, targetRotation, (float)delta * turnSpeed);
			mesh.Rotation = mesh.Rotation with { Y = rotAngle };

			
			
        }

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

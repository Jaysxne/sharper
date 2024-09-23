using Godot;
using Sharper;
using System;

public partial class Background : ColorRect
{
	[Export]
	public Button serverBtn;

	[Export]
	public Button clientBtn;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		serverBtn.Pressed += CreateServer;
		clientBtn.Pressed += CreateClient;
	}

    

    private void CreateServer()
    {
        Network.isServer = true;
		Network.CreateServer();
		QueueFree();
    }

    private void CreateClient()
    {
		Network.isServer = false;
		Network.CreateClient();
		QueueFree();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}

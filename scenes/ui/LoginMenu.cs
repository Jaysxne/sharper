using Godot;
using Nakama;
using Sharper;
using System;

public partial class LoginMenu : Control
{
    [Export]
    public Button serverBtn;

    [Export]
    public Button clientBtn;

    [Export]
    public LineEdit emailEdit;

    [Export]
    public LineEdit passwordEdit;

    [Export]
    Indicator indicator;

    [Export]
    Label warningLabel;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        serverBtn.Pressed += CreateServer;
        clientBtn.Pressed += LoginClient;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
    }



    private void CreateServer()
    {
        Network.isServer = true;
        Network.CreateServer();
        QueueFree();
    }

    private async void LoginClient()
    {
        Network.isServer = false;
        string email = emailEdit.Text.StripEdges().ToLower();
        string password = passwordEdit.Text;
       
        if(indicator != null)
        {
            indicator.Enabled = true;
        }
        (ISession session, String errorMsg) =  await Network.CreateClient(email,password);

        indicator.Enabled = false;
        if (session != null)
        {
            warningLabel.LabelSettings.FontColor = new Color(0, 1, 0);
            warningLabel.Text = "Login Successful!";
            SceneTreeTimer timer = GetTree().CreateTimer(2);
            timer.Timeout += () =>
            {
                GD.Print("timeout");
                CreateTween().TweenProperty(this, "position:x", -1200, .4).SetTrans(Tween.TransitionType.Sine);
                //QueueFree();
            };
            return;
        }

        warningLabel.Text = errorMsg;



       
    }

    
}

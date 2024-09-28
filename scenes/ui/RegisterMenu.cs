using Godot;
using Nakama;
using Sharper;
using System;

public partial class RegisterMenu : Control
{
	[Export]
	LineEdit emailEdit;

    [Export]
    LineEdit usernameEdit;


    [Export]
    LineEdit passwordEdit;

    [Export]
    Button createBtn;

    [Export]
    Indicator indicator;

    [Export]
    Label warningLabel;



    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        GD.Print(createBtn.Text);
        createBtn.Pressed += CreateAccount;
	}

	
    public async void CreateAccount()
    {
        string email = emailEdit.Text.Trim().ToLower();
        string username = usernameEdit.Text.Trim().ToLower();
        string password = passwordEdit.Text.Trim();
       if (indicator != null)
            indicator.Enabled = true;
        (ISession session, string errMsg) = await Network.CreateClient(email, password, username);
        indicator.Enabled = false;
        
        if (session == null)
        {
           warningLabel.Text = errMsg;
           return;
        }

        warningLabel.Text = "Account Created!";
        warningLabel.LabelSettings.FontColor = new Color(0, 1, 0);
        SceneTreeTimer timer = GetTree().CreateTimer(2);
        timer.Timeout += () =>
        {
            CreateTween().TweenProperty(this, "Position:X", -500,1);
        };
        
        
    }
}

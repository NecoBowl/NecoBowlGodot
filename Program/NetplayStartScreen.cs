using Godot;
using System;
using System.Linq;
using System.Net;
using neco_soft.NecoBowlGodot.Program.Ui;
using neco_soft.NecoBowlGodot.Program.Ui.Playfield;

using NLog;

public partial class NetplayStartScreen : Control
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public CheckBox HostOption => GetNode<CheckBox>("%HostOption");
    public CheckBox ConnectOption => GetNode<CheckBox>("%ConnectOption");
    public TextEdit IpEntry => GetNode<TextEdit>("%IpEntry");
    public Button GoButton => GetNode<Button>("%ShartButton");

    private bool ForceHost = false;
    public bool IsClient => !HostOption.ButtonPressed || !ForceHost;
	
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ConnectOption.ButtonGroup = new ButtonGroup();
        HostOption.ButtonGroup = ConnectOption.ButtonGroup;
        
        ConnectOption.ButtonGroup.Pressed += _ => UpdateEnabledShit();
        GoButton.Pressed += DoNetplayShit;

        this.NecoClient().ConnectionFinished += ClientsDoneConnecting;
		
        UpdateEnabledShit();
        
        if (OS.GetCmdlineUserArgs().Contains("-H"))
        {
            ForceHost = true;
            DoNetplayShit();
        } else if (OS.GetCmdlineUserArgs().Contains("-C"))
        {
            DoNetplayShit();
        }
    }

    private void DoNetplayShit()
    {
        if (IsClient)
        {
            this.NecoClient().SetupAsClient(IpEntry.Text);
        }
        else
        {
            // We hosting!!	
            this.NecoClient().SetupForHosting();
            Logger.Info("Host button pressed!");
        }
        
        UpdateEnabledShit();
    }

    public void UpdateEnabledShit()
    {
        IpEntry.Visible = !HostOption.ButtonPressed;

        GetNode<Control>("%NetplayControls").Visible = !this.NecoClient().NetworkStarted;
        GetNode<Control>("%WaitingAsHost").Visible
            = this.NecoClient().NetworkStarted && this.NecoClient().IsHostingForReal;
        GetNode<Control>("%WaitingAsClient").Visible
            = this.NecoClient().NetworkStarted && !this.NecoClient().IsHostingForReal;
    }

    private void ClientsDoneConnecting()
    {
        this.RemoveAndFreeChildren();
        ReplaceBy(Playfield.New(false));
        
        // TODO Free?
    }
}
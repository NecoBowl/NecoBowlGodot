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
    private bool SkipHolePunch = false;
    public bool IsClient => !HostOption.ButtonPressed || !ForceHost;
	
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ConnectOption.ButtonGroup = new ButtonGroup();
        HostOption.ButtonGroup = ConnectOption.ButtonGroup;
        
        ConnectOption.ButtonGroup.Pressed += _ => UpdateEnabledShit();
        GoButton.Pressed += () => DoNetplayShit(ConnectOption.ButtonPressed);

        this.NecoClient().ConnectionFinished += ClientsDoneConnecting;
		
        UpdateEnabledShit();
        
        if (OS.GetCmdlineUserArgs().Contains("-H"))
        {
            ForceHost = true;
            SkipHolePunch = true;
            DoNetplayShit(false);
        } else if (OS.GetCmdlineUserArgs().Contains("-C"))
        {
            DoNetplayShit(true);
        }
    }

    private void DoNetplayShit(bool isClient)
    {
        if (isClient)
        {
            this.NecoClient().SetupAsClient(IpEntry.Text);
        }
        else
        {
            // We hosting!!	
            this.NecoClient().SetupForHosting(!SkipHolePunch);
            Logger.Info("Host button pressed!");

            if (!this.NecoClient().HolePunchSuccess)
            {
                GetNode<RichTextLabel>("%WaitingAsHost").Text +=
                    $"\nFailed to hole punch (error {this.NecoClient().HolePunchReturnCode}). You probably have to port forward 13237.";
            }
        }
        
        UpdateEnabledShit();
    }

    public void UpdateEnabledShit()
    {
        ForceHost = HostOption.ButtonPressed;
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
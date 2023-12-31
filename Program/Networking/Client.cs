using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Transactions;
using Godot;
using neco_soft.NecoBowlGodot.Program.Loader;
using neco_soft.NecoBowlGodot.Program.Ui;
using neco_soft.NecoBowlGodot.Program.Ui.Playfield;

using NecoBowl.Core;
using NecoBowl.Core.Input;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tactics;
using NecoBowl.Core.Tags;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using JsonException = Newtonsoft.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace neco_soft.NecoBowlGodot.Program.Networking;

/// <summary>
/// Node to access RPC functionality.
/// </summary>
public partial class Client : Node
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const int ENetPort = 13237;
    
    /// <summary>
    /// Sent when all players have consented to end the play.
    /// </summary>
    [Signal]
    public delegate void EndPlayRequestedEventHandler();

    [Signal]
    public delegate void InputSentEventHandler();

    [Signal]
    public delegate void ConnectionFinishedEventHandler();
    
    /// <summary>
    /// If null, this client is treated as being in single-player mode.
    /// </summary>
    public Player? Player;
    public Player? OtherPlayer => Player is null ? null : this.NecoMatch().Context.Players.Enumerate().Single(p => p.Id != Player.Id);

    public NecoPlayerRole PlayerRole =>
        Player is null ? NecoPlayerRole.Offense : this.NecoMatch().Context.Players.RoleOf(Player.Id);

    /// <summary>
    /// Tracks player requests to end the play that gets displayed at the start of every turn (except the first).
    /// </summary>
    private readonly Dictionary<NecoPlayerRole, bool> EndPlayRequests =
        Enum.GetValues<NecoPlayerRole>().ToDictionary(r => r, _ => false);

    public bool NetworkStarted { get; private set; } = false;
    public bool IsHostingForReal { get; private set; } = false;
    public bool HolePunchSuccess { get; private set; } = false;
    public int HolePunchReturnCode = 0;

    public override void _Ready()
    {
        Player = null;

        SetUpMultiplayer();
    }

    private void SetUpMultiplayer()
    {
        Multiplayer.PeerConnected += (p) =>
        {
            Logger.Info($"Peer {p} connected!");
        };
    }

    public bool LocalHasRequestedEndPlay()
    {
        if (Player is null) return false;
        return EndPlayRequests[this.NecoMatch().Context.Players.RoleOf(Player.Id)];
    }

    public void SendNecoInput(NecoInput input)
    {
        var inputSer = JsonConvert.SerializeObject(input,
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented, Converters = { new CardConverter(), new StringEnumConverter() }});

        Logger.Info(inputSer);
        
        Rpc(nameof(Rpc_SendNecoInput), inputSer);
    }

    public void RequestEndPlay()
    {
        if (Player is null)
        {
            // Single player.
            EmitSignal(nameof(EndPlayRequested));
            return;
        }
        
        Rpc(nameof(Rpc_RequestEndPlay), Player.Id.Id.ToString());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = 0)]
    private void Rpc_RequestEndPlay(string playerGuid)
    {
        var playerId = new NecoPlayerId(Guid.Parse(playerGuid));
        EndPlayRequests[this.NecoMatch().Context.Players.RoleOf(playerId)] = true;

        // Send signal and reset the dict if all players have requested to end.
        if (EndPlayRequests.Values.All(b => b))
        {
            EmitSignal(nameof(EndPlayRequested));
            foreach (var role in Enum.GetValues<NecoPlayerRole>())
            {
                EndPlayRequests[role] = false;
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = 0)]
    private void Rpc_SendNecoInput(string necoInputSer)
    {
        Logger.Info(necoInputSer);
        var input = JsonConvert.DeserializeObject<NecoInput>(necoInputSer,
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented, Converters = { new CardConverter() }});

        if (input is not null)
        {
            Logger.Info($"Remote input: {input}");
            this.NecoMatch().Context.SendInput(input);
        }
        else
        {
            Logger.Error("failed to deserialize an input object");
        }

        EmitSignal(nameof(InputSent));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Rpc_TellMatchInfo(string offenseId, string defenseId, int clientRole)
    {
        this.NecoMatch().Context = new NecoBowlContext(new(new(new(Guid.Parse(offenseId))), new(new(Guid.Parse(defenseId)))));
        this.Player = this.NecoMatch().Context.Players.FromRole((NecoPlayerRole)clientRole);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Rpc_StartMatchView()
    {
        EmitSignal(nameof(ConnectionFinished));
    }

    public void SetupAsClient(string ipEntryText)
    {
        if (NetworkStarted)
            throw new InvalidOperationException();
        
        Logger.Info($"Playing as client connecting to {ipEntryText}");
        
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(ipEntryText.ToString(), ENetPort);
        Multiplayer.MultiplayerPeer = peer;
        
        NetworkStarted = true;
    }

    public void SetupForHosting(bool doHolePunch)
    {
        if (NetworkStarted)
            throw new InvalidOperationException();
        
        Logger.Info("Playing as host");
        
        this.NecoMatch().Context = new NecoBowlContext(new(new(), new()));
        Player = this.NecoMatch().Context.Players.FromRole(NecoPlayerRole.Offense);
        
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(ENetPort);
        Multiplayer.MultiplayerPeer = peer;
        IsHostingForReal = true;

        Multiplayer.PeerConnected += Host_OnPlayerConnected;

        if (doHolePunch)
        {
            HolePunch();

            if (!HolePunchSuccess)
            {
                Logger.Debug("Holepunching failed.");
            }
            else
            {
                Logger.Debug("Holepunch success!");
            }
        }

        NetworkStarted = true;
    }

    private void HolePunch()
    {
        Logger.Debug("Trying to holepunch with UPnP...");
        var upnp = new Upnp();
        var err = upnp.Discover();
        if (err > 0)
        {
            Logger.Error($"UPnP error {(Upnp.UpnpResult)err} during discovery.");
            HolePunchReturnCode = err;
            return;
        }

        if (upnp.AddPortMapping(ENetPort) != 0)
        {
            Logger.Error($"UPnP error {(Upnp.UpnpResult)err} while trying to add port mapping.");
            HolePunchReturnCode = err;
            return;
        }

        HolePunchSuccess = true;
    }

    private void Host_OnPlayerConnected(long id)
    {
        RpcId(id,
            nameof(Rpc_TellMatchInfo),
            this.NecoMatch().Context.Players[NecoPlayerRole.Offense].Id.Id.ToString(),
            this.NecoMatch().Context.Players[NecoPlayerRole.Defense].Id.Id.ToString(),
            (int)NecoPlayerRole.Defense);
        
        RpcId(id, nameof(Rpc_StartMatchView));
        Rpc_StartMatchView();
    }
}

public class CardConverter : JsonConverter<Card>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public override void WriteJson(JsonWriter writer, Card? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (value is null) {
            throw new JsonException("card is null");
        }
        
        writer.WriteStartObject();
        writer.WritePropertyName("CardModel");
        writer.WriteValue(value.CardModel.InternalName);
        writer.WritePropertyName("CardId");
        writer.WriteValue(value.CardId.Value.ToString());
        writer.WriteEndObject();
    }

    public override Card? ReadJson(JsonReader reader, Type objectType, Card? existingValue, bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
    {
        CardModel? cardModel = null;
        NecoCardId? cardId = null;
        while (reader.Read())
        {
            Logger.Info(reader.Value);
            if (reader.TokenType == JsonToken.EndObject)
            {
                if (cardModel is null || cardId is null)
                {
                    throw new JsonException("failed to parse card");
                }
                
                return new UnitCard((UnitCardModel)cardModel, cardId.Value);
            }
            
            if (reader.Path.EndsWith(".CardModel"))
            {
                reader.Read();
                
                cardModel = Asset.Card.All.Select(c => c.CardModel)
                    .SingleOrDefault(c => c.InternalName == reader.Value!.ToString());
                
                if (cardModel is null)
                {
                    throw new JsonException($"invalid card model {cardModel}");
                }
            } else if (reader.Path.EndsWith(".CardId"))
            {
                reader.Read();
                cardId = new NecoCardId(Guid.Parse(reader.Value!.ToString()));
            }
        }

        throw new JsonException("invalid card");
    }
}
﻿namespace NeoNetsphere
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading.Tasks;
  using BlubLib.IO;
  using Dapper.FastCrud;
  using ExpressMapper.Extensions;
  using NeoNetsphere.Database.Game;
  using NeoNetsphere.Network;
  using NeoNetsphere.Network.Data.Game;
  using NeoNetsphere.Network.Message.Chat;
  using NeoNetsphere.Network.Message.Club;
  using NeoNetsphere.Network.Message.Game;
  using NeoNetsphere.Network.Message.GameRule;
  using NeoNetsphere.Network.Message.Relay;
  using NeoNetsphere.Network.Services;
  using ProudNetSrc;
  using Serilog;
  using Serilog.Core;

  internal class BattlEyeSession : IDisposable
  {
    private Player _player;

    public bool HB_Enabled { get; set; }

    public byte HB_Info_Received { get; set; }

    public bool HB_Last_Issued { get; set; }

    public int HB_Count { get; set; }

    public TimeSpan HB_Last { get; set; }

    public bool Disposed { get; private set; }

    public BattlEyeSession(Player plr)
    {
      _player = plr;
      HB_Last = TimeSpan.Zero;
    }

    public void Dispose()
    {
      if (Disposed)
        return;

      Disposed = true;
      HB_Enabled = false;
      _player = null;
    }

    public void EnableBE()
    {
      if (Disposed)
        return;

      if (!HB_Enabled && Config.Instance.ACMode == 2)
      {
        HB_Enabled = true;
        HB_Last_Issued = true;
        HB_Last = TimeSpan.FromMinutes(1);
        HB_Count = 0;
        HB_Info_Received = 0;

        using (var writer = new BinaryWriter(new MemoryStream()))
        {
          writer.Write(new byte[] { 0x00 });
          _player.SendAsync(new BattleyeS2CDataMessage(writer.ToArray()));
        }

        using (var writer = new BinaryWriter(new MemoryStream()))
        {
          writer.Write(new byte[]
          {
                        0x02, 0x00, 0x00, 0x31, 0x32, 0x34, 0x63, 0x36, 0x33, 0x62, 0x32, 0x63, 0x62, 0x62, 0x32, 0x64,
                        0x63, 0x38, 0x63, 0x38, 0x63, 0x37, 0x62, 0x64, 0x33, 0x66, 0x62, 0x64, 0x32, 0x62, 0x61, 0x36,
                        0x66, 0x34, 0x63
          });
          _player.SendAsync(new BattleyeS2CDataMessage(writer.ToArray()));
        }

        GeneralService.Logger.ForAccount(_player)
            .Warning("[BE] Enabled");
      }
    }
  }

  internal class Player : IDisposable
  {
    // ReSharper disable once InconsistentNaming
    private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, "GamePlayerMgr");

    private uint _ap;
    private uint _coins1;
    private uint _coins2;
    private byte _level;
    private uint _pen;
    private string _playtime;
    private uint _totalExperience;
    private uint _totallosses;
    private uint _totalwins;
    private byte _tutorialState;

    public bool LoggedIn = false;
    public StatsManager stats;
    public BattlEyeSession BE;

    public Player(GameSession session, Account account, PlayerDto dto)
    {
      Session = session;
      Account = account;

      _playtime = dto.PlayTime;
      _tutorialState = dto.TutorialState;
      _level = dto.Level;
      _totalExperience = dto.TotalExperience;
      _pen = (uint)dto.PEN;
      _ap = (uint)dto.AP;
      _coins1 = (uint)dto.Coins1;
      _coins2 = (uint)dto.Coins2;
      TotalMatches = (uint)dto.TotalMatches;
      _totallosses = (uint)dto.TotalLosses;
      _totalwins = (uint)dto.TotalWins;
      stats = new StatsManager(this, dto);

      Settings = new PlayerSettingManager(this, dto);
      DenyManager = new DenyManager(this, dto);
      FriendManager = new FriendManager(this, dto);
      Mailbox = new Mailbox(this, dto);

      Inventory = new Inventory(this, dto);
      CharacterManager = new CharacterManager(this, dto);
         ItemStats = new ItemStatsManager(this);
      Club = GameServer.Instance.ClubManager.GetClubByAccount(account.Id);

      Room = null;
      RoomInfo = new PlayerRoomInfo();
      PlayerCoinBuff = new PlayerCoinBuff(this);
      LuckyShot = new PlayerLuckyShot(this);

      BE = new BattlEyeSession(this);
    }

    /// <summary>
    ///     Gains experiences and levels up if the player earned enough experience
    /// </summary>
    /// <param name="amount">Amount of experience to earn</param>
    /// <returns>true if the player leveled up</returns>
    public bool GainExp(int amount)
    {
      if (Disposed)
        return false;

      Logger.ForAccount(this)
          .Information("Gained {0} exp", amount);

      var leveledUp = false;
      var expTable = GameServer.Instance.ResourceCache.GetExperience();
      var expInfo = expTable.GetValueOrDefault(Level);
      if (expInfo == null)
      {
        Logger.ForAccount(this)
            .Warning("Level {0} not found", Level);
      }
      else
      {
        // We cant earn exp when we reached max level
        if (expInfo.ExperienceToNextLevel == 0 || Level >= Config.Instance.Game.MaxLevel)
          return false;

        TotalExperience += (uint)amount;

        var oldLevel = Level;

                // Did we level up?
                // using a loop for multiple level ups
                while (expInfo.ExperienceToNextLevel != 0 &&
                       expInfo.ExperienceToNextLevel <= (int)(TotalExperience - expInfo.TotalExperience))
                {
                    var newLevel = Level + 1;
                    expInfo = expTable.GetValueOrDefault(newLevel);

                    if (expInfo == null)
                    {
                        Logger.ForAccount(this)
                            .Warning("Can't level up because level {0} not found", newLevel);
                        break;
                    }

                    Level++;
                    leveledUp = true;

                    switch (Level)
                    {
                        case 1:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 2:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 3:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 4:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 5:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 6:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 7:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 8:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 9:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 10:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 11:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 12:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 13:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 14:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 15:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 16:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 17:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 18:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 19:
                            AP += 0;
                            PEN += 500;
                            break;
                        case 20:
                            AP += 15;
                            PEN += 500;
                            break;
                        case 21:
                            AP += 1;
                            PEN += 1000;
                            break;
                        case 22:
                            AP += 1;
                            PEN += 1000;
                            break;
                        case 23:
                            AP += 1;
                            PEN += 1000;
                            break;
                        case 24:
                            AP += 1;
                            PEN += 1000;
                            break;
                        case 25:
                            AP += 5;
                            PEN += 1000;
                            break;
                        case 26:
                            AP += 2;
                            PEN += 1000;
                            break;
                        case 27:
                            AP += 2;
                            PEN += 1000;
                            break;
                        case 28:
                            AP += 2;
                            PEN += 1000;
                            break;
                        case 29:
                            AP += 2;
                            PEN += 1000;
                            break;
                        case 30:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 31:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 32:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 33:
                            AP += 2;
                            PEN += 1000;
                            break;
                        case 34:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 35:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 36:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 37:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 38:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 39:
                            AP += 2;
                            PEN += 1500;
                            break;
                        case 40:
                            AP += 15;
                            PEN += 5000;
                            break;
                        case 41:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 42:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 43:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 44:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 45:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 46:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 47:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 48:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 49:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 50:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 51:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 52:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 53:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 54:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 55:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 56:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 57:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 58:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 59:
                            AP += 2;
                            PEN += 5000;
                            break;
                        case 60:
                            AP += 20;
                            PEN += 150000;
                            break;
                        case 61:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 62:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 63:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 64:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 65:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 66:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 67:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 68:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 69:
                            AP += 5;
                            PEN += 10000;
                            break;
                        case 70:
                            AP += 10;
                            PEN += 10000;
                            break;
                        case 71:
                            AP += 10;
                            PEN += 10000;
                            break;
                        case 72:
                            AP += 10;
                            PEN += 10000;
                            break;
                        case 73:
                            AP += 10;
                            PEN += 10000;
                            break;
                        case 74:
                            AP += 10;
                            PEN += 10000;
                            break;
                        case 75:
                            AP += 10;
                            PEN += 100000;
                            break;
                        case 76:
                            AP += 10;
                            PEN += 100000;
                            break;
                        case 77:
                            AP += 10;
                            PEN += 100000;
                            break;
                        case 78:
                            AP += 10;
                            PEN += 100000;
                            break;
                        case 79:
                            AP += 10;
                            PEN += 100000;
                            break;
                        case 80:
                            AP += 300;
                            PEN += 500000;
                            break;
                    }

                    SendAsync(new PlayerAccountInfoAckMessage(this.Map<Player, PlayerAccountInfoDto>()));
                    SendAsync(new MoneyRefreshCashInfoAckMessage(PEN, AP));
                }

        if (leveledUp)
        {
          Logger.ForAccount(this).Information("Leveled up from {0} to {1}", oldLevel, Level);
          SendAsync(new ExpRefreshInfoAckMessage(TotalExperience));
        }
      }

      SendAsync(new PlayerAccountInfoAckMessage(this.Map<Player, PlayerAccountInfoDto>()));
      SendAsync(new MoneyRefreshCashInfoAckMessage(PEN, AP));
      return leveledUp;
    }

    public void GainPen(uint amount)
    {
      this.PEN += amount;
      this.SendAsync(new MoneyRefreshCashInfoAckMessage { PEN = this.PEN, AP = this.AP });
    }
    /// <param name="attribute">The attribute to retrieve</param>
    /// <returns></returns>
    /// <summary>
    ///     Sends a message to the game master console
    /// </summary>
    /// <param name="message"
    public void SendConsoleMessage(string message)
    {
      SendAsync(new AdminActionAckMessage { Result = 0, Message = message });
    }

    /// <summary>
    ///     Sends a notice message
    /// </summary>
    /// <param name="message">The message to send</param>
    public void SendNotice(string message)
    {
      SendAsync(new NoticeAdminMessageAckMessage(message));
    }

    /// <summary>
    ///     Saves all pending changes to the database
    /// </summary>
    public void Save()
    {
      if (Disposed)
        return;

      using (var db = GameDatabase.Open())
      {
        if (NeedsToSave)
        {
          var dto = new PlayerDto
          {
            Id = (int)Account.Id,
            PlayTime = PlayTime,
            TutorialState = TutorialState,
            Level = Level,
            TotalExperience = TotalExperience,
            PEN = (int)PEN,
            AP = (int)AP,
            Coins1 = (int)Coins1,
            Coins2 = (int)Coins2,
            CurrentCharacterSlot = CharacterManager.CurrentSlot,
            TotalMatches = (int)(TotalWins + TotalLosses),
            TotalLosses = (int)TotalLosses,
            TotalWins = (int)TotalWins
          };

          DbUtil.Update(db, dto);
          NeedsToSave = false;
        }

        Settings.Save(db);
        Inventory.Save(db);
        CharacterManager.Save(db);
        DenyManager.Save(db);
        Mailbox.Save(db);
        stats.Save(db);
      }
    }

    public Task SendAsync(IGameMessage message)
    {
      if (Disposed)
        return Task.CompletedTask;
      return Session?.SendAsync(message);
    }

    public Task SendAsync(IGameRuleMessage message)
    {
      if (Disposed)
        return Task.CompletedTask;
      return Session?.SendAsync(message);
    }

    public Task SendAsync(IClubMessage message)
    {
      if (Disposed)
        return Task.CompletedTask;
      return Session?.SendAsync(message);
    }

    public Task SendAsync(IChatMessage message)
    {
      if (Disposed)
        return Task.CompletedTask;
      return ChatSession?.SendAsync(message);
    }

    public Task SendAsync(IRelayMessage message)
    {
      if (Disposed)
        return Task.CompletedTask;
      return RelaySession?.SendAsync(message);
    }

    /// <summary>
    ///     Disconnects the player
    /// </summary>
    public void Disconnect()
    {
      Session?.Dispose();
    }

    #region Properties

    internal bool NeedsToSave { get; set; }

    public GameSession Session { get; set; }
    public ChatSession ChatSession { get; set; }
    public RelaySession RelaySession { get; set; }
        public ItemStatsManager ItemStats;
        public PlayerSettingManager Settings { get; private set; }

    public DenyManager DenyManager { get; private set; }
    public FriendManager FriendManager { get; private set; }
    public Mailbox Mailbox { get; private set; }
    public PlayerCoinBuff PlayerCoinBuff { get; set; }
    public PlayerLuckyShot LuckyShot { get; set; }

    public Account Account { get; set; }
    public Club Club { get; set; }
    public CharacterManager CharacterManager { get; private set; }
    public Inventory Inventory { get; private set; }
    public Channel Channel { get; internal set; }

    public Room Room { get; internal set; }
    public PlayerRoomInfo RoomInfo { get; private set; }

    private DateTimeOffset _lastOnTime = DateTimeOffset.Now;

    public bool Disposed { get; private set; }

    public TimeSpan OnTimeDelta
    {
      get
      {
        var ontime = DateTimeOffset.Now - _lastOnTime;
        _lastOnTime = DateTimeOffset.Now;
        return ontime;
      }
    }

    public string PlayTime
    {
      get
      {
        if (_playtime == "")
          _playtime = TimeSpan.FromSeconds(0).ToString();

        _playtime = (TimeSpan.Parse(_playtime) + OnTimeDelta).ToString();
        NeedsToSave = true;
        return _playtime;
      }
    }

    public byte TutorialState
    {
      get => _tutorialState;
      set
      {
        if (_tutorialState == value)
          return;
        _tutorialState = value;
        NeedsToSave = true;
      }
    }

    public byte Level
    {
      get => _level;
      set
      {
        if (_level == value)
          return;
        _level = value;
        NeedsToSave = true;
      }
    }

    public uint TotalExperience
    {
      get => _totalExperience;
      set
      {
        if (_totalExperience == value)
          return;
        _totalExperience = value;
        NeedsToSave = true;
      }
    }

    public uint PEN
    {
      get => _pen;
      set
      {
        if (_pen == value)
          return;
        _pen = value;
        NeedsToSave = true;
      }
    }

    public uint AP
    {
      get => _ap;
      set
      {
        if (_ap == value)
          return;
        _ap = value;
        NeedsToSave = true;
      }
    }

    public uint Coins1
    {
      get => _coins1;
      set
      {
        if (_coins1 == value)
          return;
        _coins1 = value;
        NeedsToSave = true;
      }
    }

    public uint Coins2
    {
      get => _coins2;
      set
      {
        if (_coins2 == value)
          return;
        _coins2 = value;
        NeedsToSave = true;
      }
    }

    public uint TotalWins
    {
      get => _totalwins;
      set
      {
        if (_totalwins == value)
          return;
        _totalwins = value;
        TotalMatches = _totalwins + _totallosses;
        NeedsToSave = true;
      }
    }

    public uint TotalLosses
    {
      get => _totallosses;
      set
      {
        if (_totallosses == value)
          return;
        _totallosses = value;
        TotalMatches = _totalwins + _totallosses;
        NeedsToSave = true;
      }
    }

    public uint TotalMatches { get; private set; }

    #endregion

    public static bool operator !=(Player srcPlayer, Player destPlayer)
    {
      return srcPlayer?.Account?.Id != destPlayer?.Account?.Id;
    }

    public static bool operator ==(Player srcPlayer, Player destPlayer)
    {
      return srcPlayer?.Account?.Id == destPlayer?.Account?.Id;
    }

    public void Dispose()
    {
      if (Disposed || Session == null)
        return;

      GameServer.Instance?.PlayerManager?.Remove(this);
      Session?.Dispose();
      ChatSession?.Dispose();
      RelaySession?.Dispose();
      BE?.Dispose();
      Session = null;
      ChatSession = null;
      RelaySession = null;
      Account = null;
      _playtime = null;
      _tutorialState = 0;
      _level = 0;
      _totalExperience = 0;
      _pen = 0;
      _ap = 0;
      _coins1 = 0;
      _coins2 = 0;
      TotalMatches = 0;
      _totallosses = 0;
      _totalwins = 0;
      stats = null;
      Settings = null;
      DenyManager = null;
      FriendManager = null;
      Mailbox = null;
      Inventory = null;
      CharacterManager = null;
      Club = null;
      Room = null;
      RoomInfo = null;
      BE = null;
      Disposed = true;
    }

    ~Player()
    {
      Dispose();
    }
  }
}
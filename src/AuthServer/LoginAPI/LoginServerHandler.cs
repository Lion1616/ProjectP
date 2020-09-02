namespace NeoNetsphere.LoginAPI
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using BlubLib.Threading.Tasks;
    using Dapper.FastCrud;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;
    using NeoNetsphere.Database.Auth;
    using Serilog;
    using Serilog.Core;

    public class LoginServerHandler : ChannelHandlerAdapter
    {
        private const short Magic = 0x5713;
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, "LoginServer");
        private static readonly object _loginSync = new object();

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            var firstMessage = new CCMessage();
            firstMessage.Write(CCMessage.MessageType.Notify);
            firstMessage.Write($"<region>{Config.Instance.AuthAPI.Region}</region>");
            SendA(context, firstMessage);
        }

        public override void ChannelRead(IChannelHandlerContext context, object messageData)
        {
            var buffer = messageData as IByteBuffer;
            var data = new byte[0];
            if (buffer != null) data = buffer.GetIoBuffer().ToArray();

            var msg = new CCMessage(data, data.Length);
            short magic = 0;
            var message = new ByteArray();

            if (msg.Read(ref magic)
                && magic == Magic
                && msg.Read(ref message))
            {
                var receivedMessage = new CCMessage(message);
                CCMessage.MessageType coreId = 0;
                if (!receivedMessage.Read(ref coreId)) return;

                switch (coreId)
                {
                    case CCMessage.MessageType.Rmi:
                        short rmiId = 0;
                        if (receivedMessage.Read(ref rmiId))
                        {
                            switch (rmiId)
                            {
                                case 15:
                                    {
                                        var username = "";
                                        var password = "";
                                        var hwid = "";
                                        var test = "";

                                        if (receivedMessage.Read(ref username)
                                            && receivedMessage.Read(ref password)
                                            && receivedMessage.Read(ref hwid)
                                            && receivedMessage.Read(ref test))
                                        {
                                            LoginAsync(context, username, password, hwid, test);
                                        }
                                        else
                                        {
                                            Logger.Error("Wrong login for {endpoint}", context.Channel.RemoteAddress.ToString());
                                            var ack = new CCMessage();
                                            ack.Write(false);
                                            ack.Write("Invalid loginpacket");
                                            RmiSend(context, 16, ack);
                                        }

                                        break;
                                    }
                                case 17:
                                    context.CloseAsync();
                                    break;
                                default:
                                    Logger.Error("Received unknown rmiId{rmi} from {endpoint}", rmiId,
                                        context.Channel.RemoteAddress.ToString());
                                    break;
                            }
                        }
                        break;
                    case CCMessage.MessageType.Notify:
                        context.CloseAsync();
                        break;
                    default:
                        Logger.Error("Received unknown coreID{coreid} from {endpoint}", coreId,
                            context.Channel.RemoteAddress.ToString());
                        break;
                }
            }
            else
            {
                Logger.Error("Received invalid packetstruct from {endpoint}", context.Channel.RemoteAddress.ToString());
                context.CloseAsync();
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            context.CloseAsync();
        }

        private static async void LoginAsync(IChannelHandlerContext context, string username, string password, string hwid, string test)
        {
            lock (_loginSync)
            {
                try
                {
                    using (var db = AuthDatabase.Open())
                    {
                        Logger.Information("Authentication login from {endpoint}",
                            context.Channel.RemoteAddress.ToString());

                        if (test != "test")
                            goto loginerror;

                        var hwidban = db.Find<HwidBanDto>(statement => statement
                        .Where(
                            $"{nameof(HwidBanDto.Hwid):C} = @{nameof(hwid)}")
                        .WithParameters(new { hwid }));

                        if (hwidban.Any())
                        {
                            Logger.Error("Hwid ban {0} for {1}", hwid, context.Channel.RemoteAddress);
                            goto HwidBan;
                        }

                        if (username.Length > 5 && password.Length > 5 &&
                            Namecheck.IsNameValid(username))
                        {
                            var result = db.Find<AccountDto>(statement => statement
                                .Where(
                                    $"{nameof(AccountDto.Username):C} = @{nameof(username)}")
                                .WithParameters(new { Username = username }));

                            var account = result.FirstOrDefault();

                            if (account == null &&
                                (Config.Instance.NoobMode || Config.Instance.AutoRegister))
                            {
                                account = new AccountDto { Username = username };

                                var newSalt = new byte[24];
                                using (var csprng = new RNGCryptoServiceProvider())
                                {
                                    csprng.GetBytes(newSalt);
                                }

                                var hash = new byte[24];
                                using (var pbkdf2 = new Rfc2898DeriveBytes(password, newSalt, 24000))
                                {
                                    hash = pbkdf2.GetBytes(24);
                                }

                                account.Password = Convert.ToBase64String(hash);
                                account.Salt = Convert.ToBase64String(newSalt);

                                db.InsertAsync(account);
                            }

                             var ban = db.Find<BanDto>(statement => statement
                                .Where($"{nameof(BanDto.AccountId):C} = @{nameof(account.Id)}")
                                .WithParameters(new { account.Id })).FirstOrDefault();

                            if (ban != null)
                            {
                                var unbanDate = DateTimeOffset.FromUnixTimeSeconds(ban.Date + (ban.Duration ?? 0));
                                Logger.Error("{user} is banned until {until}", account.Username, unbanDate);

                                CCMessage error = new CCMessage();
                                error.Write(false);
                                error.Write("You ban until " + unbanDate);
                                RmiSend(context, 16, error);
                            }

                            var salt = Convert.FromBase64String(account?.Salt ?? "");

                            var passwordGuess =
                                new Rfc2898DeriveBytes(password, salt, 24000).GetBytes(24);
                            var actualPassword =
                                Convert.FromBase64String(account?.Password ?? "");

                            var difference =
                                (uint)passwordGuess.Length ^ (uint)actualPassword.Length;

                            for (var i = 0;
                                i < passwordGuess.Length && i < actualPassword.Length;
                                i++)
                                difference |= (uint)(passwordGuess[i] ^ actualPassword[i]);

                            if ((difference != 0 ||
                                 string.IsNullOrWhiteSpace(account?.Password ?? "")) &&
                                !Config.Instance.NoobMode)
                            {
                                Logger.Error(
                                    "Wrong authentication credentials for {username} / {endpoint}",
                                    username, context.Channel.RemoteAddress.ToString());
                                var ack = new CCMessage();
                                ack.Write(false);
                                ack.Write("Login failed");
                                RmiSend(context, 16, ack);
                            }
                            else
                            {
                                if (account != null)
                                {
                                    account.LoginToken = AuthHash
                                        .GetHash256(
                                            $"{context.Channel.RemoteAddress}-{account.Username}-{account.Password}")
                                        .ToLower();
                                    account.LastLogin =
                                        $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
                                    account.AuthToken = "";
                                    account.newToken = "";
                                    db.UpdateAsync(account);

                                    var ack = new CCMessage();
                                    ack.Write(true);
                                    ack.Write(account.LoginToken);
                                    RmiSend(context, 16, ack);
                                }

                                var authhistory = new AuthHistoryDto()
                                {
                                    AccountId = account.Id,
                                    Date = DateTime.UtcNow.ToString(),
                                    HWID = hwid
                                };
                                db.Insert(authhistory);

                                Logger.Information("Authentication success for {username}",
                                    username);

                                if (account.LoginToken != "" && account.LoginToken.Length == 64)
                                {
                                    account.IsConnect = true;
                                    db.Update(account);
                                }
                            }
                        }
                        else
                        {
                            Logger.Error(
                                "Wrong authentication credentials for {username} / {endpoint}",
                                username, context.Channel.RemoteAddress.ToString());
                            var ack = new CCMessage();
                            ack.Write(false);
                            ack.Write("Invalid length of username/password");
                            RmiSend(context, 16, ack);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    var ack = new CCMessage();
                    ack.Write(false);
                    ack.Write("Login Error");
                    RmiSend(context, 16, ack);
                }

                HwidBan:
                {
                    var error = new CCMessage();
                    error.Write(false);
                    error.Write("You Banned.");
                    RmiSend(context, 16, error);
                    return;
                }
                loginerror:
                {
                    var error = new CCMessage();
                    error.Write(false);
                    error.Write("Failed to login.");
                    RmiSend(context, 16, error);
                    return;
                }
            }
        }

        private static void RmiSend(IChannelHandlerContext ctx, short rmiId, CCMessage message)
        {
            var rmiframe = new CCMessage();
            rmiframe.Write(CCMessage.MessageType.Rmi);
            rmiframe.Write(rmiId);
            rmiframe.Write(message);
            SendA(ctx, rmiframe);
        }

        private static Task SendA(IChannelHandlerContext ctx, CCMessage data)
        {
            var coreframe = new CCMessage();
            coreframe.Write(Magic);
            coreframe.WriteScalar(data.Length);
            coreframe.Write(data);

            var buffer = Unpooled.Buffer(coreframe.Length);
            buffer.WriteBytes(coreframe.Buffer);
            try
            {
                ctx.WriteAndFlushAsync(buffer).WaitEx();
            }
            catch (Exception e)
            {
                ctx.FireExceptionCaught(e);
            }

            return Task.CompletedTask;
        }
    }
}
